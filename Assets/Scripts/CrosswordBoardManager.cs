using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CrosswordWord
{
    public string id;          // e.g. "1A", "2D"
    public bool isAcross;      // true = across, false = down
    public int startRow;
    public int startCol;
    public string answer;      // "DRAGONFLY"
    [TextArea]
    public string clue;        // "Winged insect often seen near ponds."
}

public class CrosswordBoardManager : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    [SerializeField] private CrosswordCell cellPrefab;
    [SerializeField] private Transform gridParent;   // Cross_GridParent

    [Header("Layout (# = blocked, . = playable)")]
    [TextArea(1, 20)]
    public string[] layoutRows = new string[]
    {
        "....##....",
        "...##.....",
        "..........",
        "..........",
        "..........",
        "....####..",
        "..........",
        ".....##...",
        "....##....",
        ".........."
    };

    private CrosswordCell[,] cells;
    private int rows;
    private int cols;

    [Header("Words & Clues (runtime-filled)")]
    public List<CrosswordWord> words = new List<CrosswordWord>();

    // quick lookup: which word(s) live on each cell
    private CrosswordWord[,] acrossAt;
    private CrosswordWord[,] downAt;

    // solution letter for each cell (empty = '\0')
    private char[,] solutionLetters;

    [Header("Database")]
    [Tooltip("Resources file name (without .json). Default expects Assets/Resources/trivia_database.json")]
    [SerializeField] private string resourcesDbName = "trivia_database";

    [Tooltip("If true, fills the crossword from the DB based on ModeSelect session.")]
    [SerializeField] private bool fillFromDatabaseOnStart = true;

    [Tooltip("When pulling answers, allow reuse within the same puzzle if needed (not recommended).")]
    [SerializeField] private bool allowDuplicatesInPuzzle = false;

    [Header("Debug / Testing")]
    [SerializeField] private bool autoFillSolutionOnStart = true;

    private GameDatabase dbCached;

    private void Awake()
    {
        CrosswordSession.currentWords = words;
    }

    private void Start()
    {
        // Normalize layout so EVERYTHING agrees on dimensions
        layoutRows = NormalizeLayout(layoutRows);

        if (fillFromDatabaseOnStart)
            FillWordsFromDatabase();

        BuildBoard();

        if (autoFillSolutionOnStart)
            DebugFillSolution();
    }

    // -----------------------------
    // DB LOADING + SLOT FILL (SAFE)
    // -----------------------------

    private void FillWordsFromDatabase()
{
    // Always start clean so we never show inspector defaults by accident
    words.Clear();

    string catId   = TriviaSessionData.selectedCategoryId;
    string subId   = TriviaSessionData.selectedSubcategoryId;
    string diffKey = NormalizeDifficulty(TriviaSessionData.selectedDifficulty);

    bool isMixed = (diffKey == "mixed" || diffKey == "all" || diffKey == "any");

    layoutRows = NormalizeLayout(layoutRows);

    Debug.Log($"[CrosswordBoardManager] FillWordsFromDatabase START | DB='{resourcesDbName}' | cat='{catId}' sub='{subId}' diff='{diffKey}' mixed={isMixed}");

    GameDatabase db = LoadDatabase();
    if (db == null)
    {
        Debug.LogError("[CrosswordBoardManager] DB load failed (db is null).");
        CrosswordSession.currentWords = words;
        return;
    }

    CategoryData cat = db.categories?.Find(c => c.id == catId);
    if (cat == null)
    {
        Debug.LogError($"[CrosswordBoardManager] Category not found: '{catId}'");
        CrosswordSession.currentWords = words;
        return;
    }

    SubcategoryData sub = cat.subcategories?.Find(s => s.id == subId);
    if (sub == null)
    {
        Debug.LogError($"[CrosswordBoardManager] Subcategory not found: '{subId}' under '{catId}'");
        CrosswordSession.currentWords = words;
        return;
    }

    int rawCount = sub.crosswords?.Count ?? 0;
    Debug.Log($"[CrosswordBoardManager] Found sub.crosswords count={rawCount}");
    if (rawCount == 0)
    {
        Debug.LogError($"[CrosswordBoardManager] sub.crosswords is empty for '{catId}/{subId}'");
        CrosswordSession.currentWords = words;
        return;
    }

    // Build pool
    var pool = new List<CrosswordEntry>();
    foreach (var e in sub.crosswords)
    {
        if (e == null) continue;
        if (string.IsNullOrWhiteSpace(e.answer) || string.IsNullOrWhiteSpace(e.clue)) continue;

        string d = NormalizeDifficulty(e.difficulty);
        if (string.IsNullOrWhiteSpace(d)) d = "medium";

        if (!isMixed && d != diffKey) continue;

        string ans = CleanAnswer(e.answer);
        if (ans.Length < 3 || ans.Length > 10) continue;

        pool.Add(new CrosswordEntry
        {
            id = e.id,
            answer = ans,
            clue = e.clue.Trim(),
            difficulty = d
        });
    }

    Debug.Log($"[CrosswordBoardManager] Pool after difficulty filter: {pool.Count}");
    if (pool.Count == 0)
    {
        Debug.LogError($"[CrosswordBoardManager] Pool is 0 for diff='{diffKey}' mixed={isMixed}. Nothing to place.");
        CrosswordSession.currentWords = words;
        return;
    }

    // ---- Attempt 1: solver (smart feed-off-each-other fill) ----
    bool ok = false;
    List<CrosswordWord> solvedWords = null;

    try
    {
        Debug.Log("[CrosswordBoardManager] Calling CrosswordFiller.TryFillAllSlots...");
        ok = CrosswordFiller.TryFillAllSlots(
            layoutRows,
            pool,
            out solvedWords,
            seed: 0,
            maxSolveAttempts: 60,
            maxBacktrackNodes: 350000,
            allowDuplicates: allowDuplicatesInPuzzle,
            minLen: 3,
            maxLen: 10
        );
        Debug.Log($"[CrosswordBoardManager] Solver returned ok={ok} solvedCount={(solvedWords != null ? solvedWords.Count : -1)}");
    }
    catch (Exception ex)
    {
        Debug.LogError("[CrosswordBoardManager] Solver threw exception:\n" + ex);
        ok = false;
    }

    if (ok && solvedWords != null && solvedWords.Count > 0)
    {
        words.Clear();
        words.AddRange(solvedWords);
        Debug.Log($"[CrosswordBoardManager] Solver SUCCESS. Words placed={words.Count}");
        CrosswordSession.currentWords = words;
        return;
    }

    Debug.LogWarning("[CrosswordBoardManager] Solver FAILED (or returned 0 words). Falling back to DB greedy fill so you see letters.");
    
    // ---- Attempt 2 (fallback): greedy fill that avoids conflicts (still DB-backed) ----
    // We place ACROSS first (longest -> shortest), then DOWN (longest -> shortest).
    // This prevents the "only down" outcome and produces more crossword-y intersections.
var allSlots = ComputeSlots(layoutRows);

var acrossSlots = new List<Slot>();
var downSlots   = new List<Slot>();

foreach (var s in allSlots)
{
    if (s.isAcross) acrossSlots.Add(s);
    else            downSlots.Add(s);
}

// Longer first within each direction
acrossSlots.Sort((a, b) => b.length.CompareTo(a.length));
downSlots.Sort((a, b) => b.length.CompareTo(a.length));

int rCount = layoutRows.Length;
int cCount = layoutRows[0].Length;

bool[,] blocked = new bool[rCount, cCount];
for (int r = 0; r < rCount; r++)
    for (int c = 0; c < cCount; c++)
        blocked[r, c] = (layoutRows[r][c] == '#');

// Reserved letters = committed intersections
char[,] reserved = new char[rCount, cCount];

// Group pool by length once
var byLen = new Dictionary<int, List<CrosswordEntry>>();
foreach (var e in pool)
{
    int len = e.answer.Length;
    if (!byLen.TryGetValue(len, out var list))
    {
        list = new List<CrosswordEntry>();
        byLen[len] = list;
    }
    list.Add(e);
}

var used = new HashSet<string>();
int slotIndex = 1;

// Helper local function to attempt fill a slot list
void FillSlotList(List<Slot> slotList)
{
    foreach (var slot in slotList)
    {
        if (!byLen.TryGetValue(slot.length, out var candidates) || candidates.Count == 0)
            continue;

        // Try a handful of random candidates so we don't always pick the same
        CrosswordEntry pick = null;
        int tries = Mathf.Min(40, candidates.Count);

        for (int t = 0; t < tries; t++)
        {
            var cand = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (!allowDuplicatesInPuzzle && used.Contains(cand.answer)) continue;

            if (CanFitSlot(slot, cand.answer, reserved, blocked, rCount, cCount))
            {
                pick = cand;
                break;
            }
        }

        if (pick == null) continue;

        ApplyToReserved(slot, pick.answer, reserved);

        if (!allowDuplicatesInPuzzle)
            used.Add(pick.answer);

        words.Add(new CrosswordWord
        {
            id = slotIndex.ToString() + (slot.isAcross ? "A" : "D"),
            isAcross = slot.isAcross,
            startRow = slot.startRow,
            startCol = slot.startCol,
            answer = pick.answer,
            clue = pick.clue
        });

        slotIndex++;
    }
}

// Place across first, then down
FillSlotList(acrossSlots);
FillSlotList(downSlots);

Debug.Log($"[CrosswordBoardManager] Greedy fallback placed {words.Count} words (Across={acrossSlots.Count}, Down={downSlots.Count}).");
CrosswordSession.currentWords = words;
}

    private static bool CanFitSlot(
        Slot slot,
        string answer,
        char[,] reserved,
        bool[,] blocked,
        int rows,
        int cols)
    {
        if (string.IsNullOrEmpty(answer)) return false;
        if (answer.Length != slot.length) return false;

        for (int i = 0; i < answer.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);

            if (r < 0 || r >= rows || c < 0 || c >= cols)
                return false;

            if (blocked[r, c])
                return false;

            char existing = reserved[r, c];
            char incoming = answer[i];

            // intersection must match existing reserved letter
            if (existing != '\0' && existing != incoming)
                return false;
        }

        return true;
    }

    private static void ApplyToReserved(Slot slot, string answer, char[,] reserved)
    {
        for (int i = 0; i < answer.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);
            reserved[r, c] = answer[i];
        }
    }

    private GameDatabase LoadDatabase()
    {
        if (dbCached != null) return dbCached;

        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcesDbName);
        if (jsonAsset == null)
        {
            Debug.LogError($"[CrosswordBoardManager] Could not load Resources/{resourcesDbName}.json");
            return null;
        }

        try
        {
            dbCached = JsonUtility.FromJson<GameDatabase>(jsonAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError("[CrosswordBoardManager] Failed to parse GameDatabase JSON: " + ex.Message);
            dbCached = null;
        }

        return dbCached;
    }

    private static string NormalizeDifficulty(string diff)
    {
        if (string.IsNullOrWhiteSpace(diff)) return "medium";

        diff = diff.Trim().ToLowerInvariant();
        if (diff == "insane") return "insanity";
        if (diff == "insanity") return "insanity";
        if (diff == "easy") return "easy";
        if (diff == "medium") return "medium";
        if (diff == "hard") return "hard";

        return diff;
    }

    private static string CleanAnswer(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        raw = raw.Trim().ToUpperInvariant();

        var chars = new List<char>(raw.Length);
        foreach (char ch in raw)
        {
            if (ch >= 'A' && ch <= 'Z')
                chars.Add(ch);
        }
        return new string(chars.ToArray());
    }

    private static string[] NormalizeLayout(string[] input)
    {
        if (input == null || input.Length == 0)
            return new[] { ".........." };

        int rCount = input.Length;
        int cCount = 0;
        for (int r = 0; r < rCount; r++)
            cCount = Mathf.Max(cCount, input[r]?.Length ?? 0);

        if (cCount <= 0) cCount = 10;

        var output = new string[rCount];
        for (int r = 0; r < rCount; r++)
        {
            string line = input[r] ?? "";
            if (line.Length < cCount) line = line.PadRight(cCount, '.');
            if (line.Length > cCount) line = line.Substring(0, cCount);
            output[r] = line;
        }

        return output;
    }

    private struct Slot
    {
        public bool isAcross;
        public int startRow;
        public int startCol;
        public int length;
    }

    private static List<Slot> ComputeSlots(string[] layout)
    {
        var slots = new List<Slot>();
        if (layout == null || layout.Length == 0) return slots;

        int rCount = layout.Length;
        int cCount = layout[0].Length;

        bool IsBlocked(int r, int c)
        {
            if (r < 0 || r >= rCount) return true;
            if (c < 0 || c >= cCount) return true;
            return layout[r][c] == '#';
        }

        // Across slots
        for (int r = 0; r < rCount; r++)
        {
            int c = 0;
            while (c < cCount)
            {
                while (c < cCount && IsBlocked(r, c)) c++;
                int start = c;
                while (c < cCount && !IsBlocked(r, c)) c++;
                int len = c - start;

                if (len >= 2)
                    slots.Add(new Slot { isAcross = true, startRow = r, startCol = start, length = len });
            }
        }

        // Down slots
        for (int c = 0; c < cCount; c++)
        {
            int r = 0;
            while (r < rCount)
            {
                while (r < rCount && IsBlocked(r, c)) r++;
                int start = r;
                while (r < rCount && !IsBlocked(r, c)) r++;
                int len = r - start;

                if (len >= 2)
                    slots.Add(new Slot { isAcross = false, startRow = start, startCol = c, length = len });
            }
        }

        return slots;
    }

    // -----------------------------
    // BUILD + INDEXING (STRICT)
    // -----------------------------

    private void BuildBoard()
    {
        if (cellPrefab == null || gridParent == null)
        {
            Debug.LogError("CrosswordBoardManager: Missing cellPrefab or gridParent.");
            return;
        }

        if (layoutRows == null || layoutRows.Length == 0)
        {
            Debug.LogError("CrosswordBoardManager: layoutRows is empty.");
            return;
        }

        rows = layoutRows.Length;
        cols = layoutRows[0].Length;

        // nuke any old children under the grid
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        cells = new CrosswordCell[rows, cols];
        solutionLetters = new char[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            string rowString = layoutRows[r];
            for (int c = 0; c < cols; c++)
            {
                var cellInstance = Instantiate(cellPrefab, gridParent);
                cellInstance.name = $"Cell_{r}_{c}";

                bool blocked = rowString[c] == '#';
                cellInstance.Init(this, r, c, blocked);

                cells[r, c] = cellInstance;
            }
        }

        IndexWords();

        if (autoFillSolutionOnStart)
            ApplySolutionToCellsForTesting();
    }

    private void IndexWords()
    {
        acrossAt = new CrosswordWord[rows, cols];
        downAt   = new CrosswordWord[rows, cols];

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            solutionLetters[r, c] = '\0';

        foreach (var w in words)
        {
            if (w == null) continue;
            if (string.IsNullOrWhiteSpace(w.answer)) continue;

            string ans = w.answer.ToUpperInvariant();

            // Validate full fit (bounds + not blocked)
            if (!WordFullyFits(w, ans))
            {
                Debug.LogWarning($"[CrosswordBoardManager] Word {w.id} does not fully fit (bounds/blocked). Skipping.");
                continue;
            }

            // Validate conflicts BEFORE writing anything
            if (WordConflictsExisting(w, ans))
            {
                Debug.LogWarning($"[CrosswordBoardManager] Word {w.id} conflicts with existing letters. Skipping.");
                continue;
            }

            // Safe write
            for (int i = 0; i < ans.Length; i++)
            {
                int rr = w.startRow + (w.isAcross ? 0 : i);
                int cc = w.startCol + (w.isAcross ? i : 0);

                if (w.isAcross) acrossAt[rr, cc] = w;
                else            downAt[rr, cc]   = w;

                solutionLetters[rr, cc] = ans[i];
            }
        }
    }

    private bool WordFullyFits(CrosswordWord w, string ans)
    {
        for (int i = 0; i < ans.Length; i++)
        {
            int rr = w.startRow + (w.isAcross ? 0 : i);
            int cc = w.startCol + (w.isAcross ? i : 0);

            if (rr < 0 || rr >= rows || cc < 0 || cc >= cols)
                return false;

            if (cells[rr, cc].IsBlocked)
                return false;
        }
        return true;
    }

    private bool WordConflictsExisting(CrosswordWord w, string ans)
    {
        for (int i = 0; i < ans.Length; i++)
        {
            int rr = w.startRow + (w.isAcross ? 0 : i);
            int cc = w.startCol + (w.isAcross ? i : 0);

            char existing = solutionLetters[rr, cc];
            char incoming = ans[i];

            if (existing != '\0' && existing != incoming)
                return true;
        }
        return false;
    }

    private void ApplySolutionToCellsForTesting()
    {
        if (cells == null || solutionLetters == null) return;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (cells[r, c] == null || cells[r, c].IsBlocked) continue;
            cells[r, c].SetLetter(solutionLetters[r, c]);
        }
    }

    // -----------------------------
    // UI / INPUT
    // -----------------------------

    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        int r = cell.row;
        int c = cell.col;

        ClearHighlights();

        CrosswordWord word = acrossAt[r, c] ?? downAt[r, c];
        if (word == null)
        {
            CrosswordClueDisplay.Instance?.ClearClue();
            return;
        }

        if (CrosswordClueDisplay.Instance != null)
        {
            CrosswordWord acrossWord = acrossAt[r, c];
            CrosswordWord downWord   = downAt[r, c];

            bool hasAcross = acrossWord != null;
            bool hasDown   = downWord   != null;

            if (hasAcross && hasDown)
            {
                CrosswordClueDisplay.Instance.ShowMultiClue(
                    acrossWord.id, acrossWord.clue,
                    downWord.id,   downWord.clue
                );
            }
            else if (hasAcross)
            {
                CrosswordClueDisplay.Instance.ShowClue(acrossWord.id, acrossWord.clue);
            }
            else if (hasDown)
            {
                CrosswordClueDisplay.Instance.ShowClue(downWord.id, downWord.clue);
            }
            else
            {
                CrosswordClueDisplay.Instance.ClearClue();
            }
        }

        if (word.isAcross)
            HighlightAcrossWord(r, c);
        else
            HighlightDownWord(r, c);
    }

    private void ClearHighlights()
    {
        if (cells == null) return;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            cells[r, c]?.SetHighlighted(false);
    }

    private void HighlightAcrossWord(int row, int col)
    {
        if (cells == null) return;
        if (row < 0 || row >= rows || col < 0 || col >= cols) return;
        if (cells[row, col].IsBlocked) return;

        int startCol = col;
        while (startCol - 1 >= 0 && !cells[row, startCol - 1].IsBlocked)
            startCol--;

        int endCol = col;
        while (endCol + 1 < cols && !cells[row, endCol + 1].IsBlocked)
            endCol++;

        for (int c = startCol; c <= endCol; c++)
            cells[row, c].SetHighlighted(true);
    }

    private void HighlightDownWord(int row, int col)
    {
        if (cells == null) return;
        if (row < 0 || row >= rows || col < 0 || col >= cols) return;
        if (cells[row, col].IsBlocked) return;

        int startRow = row;
        while (startRow - 1 >= 0 && !cells[startRow - 1, col].IsBlocked)
            startRow--;

        int endRow = row;
        while (endRow + 1 < rows && !cells[endRow + 1, col].IsBlocked)
            endRow++;

        for (int r = startRow; r <= endRow; r++)
            cells[r, col].SetHighlighted(true);
    }

    private void DebugFillSolution()
    {
        if (solutionLetters == null || cells == null) return;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (cells[r, c] == null) continue;
            if (cells[r, c].IsBlocked) continue;

            char ch = solutionLetters[r, c];
            if (ch != '\0')
                cells[r, c].SetLetter(ch);
        }
    }

    public void RequestHint()
    {
        Debug.Log("CrosswordBoardManager: Hint requested (not implemented yet).");
    }
}