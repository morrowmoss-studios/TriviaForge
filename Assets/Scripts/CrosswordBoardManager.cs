using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using System;
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
        // Make our words list available to other scenes (like the clues screen)
        CrosswordSession.currentWords = words;
    }

    private void Start()
    {
        if (fillFromDatabaseOnStart)
        {
            FillWordsFromDatabase();
        }

        BuildBoard();

        if (autoFillSolutionOnStart)
        {
            DebugFillSolution();
        }
    }

    // -----------------------------
    // DB LOADING + TEMPLATE FILL
    // -----------------------------

    private void FillWordsFromDatabase()
    {
        // Pull selection from session (same pattern as Wordoku)
        string catId = TriviaSessionData.selectedCategoryId;
        string subId = TriviaSessionData.selectedSubcategoryId;
        string diffKey = NormalizeDifficulty(TriviaSessionData.selectedDifficulty);

        GameDatabase db = LoadDatabase();
        if (db == null)
        {
            Debug.LogWarning("[CrosswordBoardManager] DB load failed. Leaving existing words list as-is.");
            return;
        }

        CategoryData cat = db.categories?.Find(c => c.id == catId);
        if (cat == null)
        {
            Debug.LogWarning($"[CrosswordBoardManager] Category '{catId}' not found. Leaving existing words list as-is.");
            return;
        }

        SubcategoryData sub = cat.subcategories?.Find(s => s.id == subId);
        if (sub == null)
        {
            Debug.LogWarning($"[CrosswordBoardManager] Subcategory '{subId}' not found under '{catId}'. Leaving existing words list as-is.");
            return;
        }

        if (sub.crosswords == null || sub.crosswords.Count == 0)
        {
            Debug.LogWarning($"[CrosswordBoardManager] No crossword entries in '{catId}/{subId}'. Leaving existing words list as-is.");
            return;
        }

        // 1) Compute all slots in this layout (across + down)
        var slots = ComputeSlots(layoutRows);

        if (slots.Count == 0)
        {
            Debug.LogWarning("[CrosswordBoardManager] No slots found in layout. Leaving existing words list as-is.");
            return;
        }

        // 2) Build candidate list from clue bank, filtered by difficulty + cleaned
        var pool = new List<CrosswordEntry>();
        foreach (var e in sub.crosswords)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.answer) || string.IsNullOrWhiteSpace(e.clue)) continue;

            string d = NormalizeDifficulty(e.difficulty);
            if (string.IsNullOrWhiteSpace(d)) d = "medium";

            if (d != diffKey) continue;

            string ans = CleanAnswer(e.answer);
            if (ans.Length < 2) continue; // ignore 1-letter junk
            pool.Add(e);
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[CrosswordBoardManager] No crossword entries matched diff='{diffKey}' in '{catId}/{subId}'. Leaving existing words list as-is.");
            return;
        }

        // 3) Assign entries to slots by length
        words.Clear();
        var usedAnswers = new HashSet<string>();

        int assigned = 0;
        int slotIndex = 1;

        foreach (var slot in slots)
        {
            // Find entries that fit this slot length
            var matches = new List<CrosswordEntry>();
            foreach (var e in pool)
            {
                string ans = CleanAnswer(e.answer);
                if (ans.Length != slot.length) continue;

                if (!allowDuplicatesInPuzzle && usedAnswers.Contains(ans))
                    continue;

                matches.Add(e);
            }

            if (matches.Count == 0)
                continue;

            var pick = matches[UnityEngine.Random.Range(0, matches.Count)];
            string pickAnswer = CleanAnswer(pick.answer);

            if (!allowDuplicatesInPuzzle)
                usedAnswers.Add(pickAnswer);

            // Create a placed word record for your existing system
            var w = new CrosswordWord
            {
                id = slotIndex.ToString() + (slot.isAcross ? "A" : "D"),
                isAcross = slot.isAcross,
                startRow = slot.startRow,
                startCol = slot.startCol,
                answer = pickAnswer,
                clue = pick.clue.Trim()
            };

            words.Add(w);
            assigned++;
            slotIndex++;
        }

        Debug.Log($"[CrosswordBoardManager] Filled from DB: cat='{catId}' sub='{subId}' diff='{diffKey}'. Slots={slots.Count}, assigned={assigned}, pool={pool.Count}.");

        // Update session reference after rebuild
        CrosswordSession.currentWords = words;
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
        // ModeSelect uses "Easy/Medium/Hard/Insanity" sometimes
        if (diff == "easy") return "easy";
        if (diff == "medium") return "medium";
        if (diff == "hard") return "hard";
        if (diff == "insanity") return "insanity";

        // also accept title case inputs
        if (diff == "easy") return "easy";
        if (diff == "hard") return "hard";
        if (diff == "insane") return "insanity";

        return diff;
    }

    private static string CleanAnswer(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        raw = raw.Trim().ToUpperInvariant();

        // Remove spaces, hyphens, apostrophes, etc.
        var chars = new List<char>(raw.Length);
        foreach (char ch in raw)
        {
            if (ch >= 'A' && ch <= 'Z')
                chars.Add(ch);
        }
        return new string(chars.ToArray());
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
            char ch = layout[r][c];
            return ch == '#';
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
                {
                    slots.Add(new Slot { isAcross = true, startRow = r, startCol = start, length = len });
                }
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
                {
                    slots.Add(new Slot { isAcross = false, startRow = start, startCol = c, length = len });
                }
            }
        }

        return slots;
    }

    // -----------------------------
    // EXISTING BUILD + PLAY LOGIC
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

        // nuke any old children under the grid (just in case)
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(gridParent.GetChild(i).gameObject);
        }

        cells = new CrosswordCell[rows, cols];
        solutionLetters = new char[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            string rowString = layoutRows[r];

            if (rowString.Length < cols)
                rowString = rowString.PadRight(cols, '.');

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
        {
            ApplySolutionToCellsForTesting();
        }
    }

    private void IndexWords()
    {
        acrossAt = new CrosswordWord[rows, cols];
        downAt   = new CrosswordWord[rows, cols];

        // reset solution letters
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                solutionLetters[r, c] = '\0';

        foreach (var w in words)
        {
            if (string.IsNullOrEmpty(w.answer)) continue;

            string upperAnswer = w.answer.ToUpperInvariant();

            int r = w.startRow;
            int c = w.startCol;

            for (int i = 0; i < upperAnswer.Length; i++)
            {
                int rr = r + (w.isAcross ? 0 : i);
                int cc = c + (w.isAcross ? i : 0);

                if (rr < 0 || rr >= rows || cc < 0 || cc >= cols)
                {
                    Debug.LogWarning($"Word {w.id} runs off the board at {rr},{cc}.");
                    break;
                }

                if (cells[rr, cc].IsBlocked)
                {
                    Debug.LogWarning($"Word {w.id} hits a blocked cell at {rr},{cc}.");
                    break;
                }

                char letter = upperAnswer[i];

                if (w.isAcross) acrossAt[rr, cc] = w;
                else            downAt[rr, cc]   = w;

                // conflict check (optional): if already set and different, warn
                char existing = solutionLetters[rr, cc];
                if (existing != '\0' && existing != letter)
                {
                    Debug.LogWarning($"Conflict at {rr},{cc}: existing='{existing}' new='{letter}' (word {w.id}).");
                }

                solutionLetters[rr, cc] = letter;
            }
        }
    }

    private void ApplySolutionToCellsForTesting()
    {
        if (cells == null || solutionLetters == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] == null || cells[r, c].IsBlocked) continue;

                char sol = solutionLetters[r, c];

                if (sol != '\0')
                    cells[r, c].SetLetter(sol);
                else
                    cells[r, c].SetLetter('\0');
            }
        }
    }

    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        int r = cell.row;
        int c = cell.col;

        ClearHighlights();

        CrosswordWord word = acrossAt[r, c] ?? downAt[r, c];
        if (word == null)
        {
            if (CrosswordClueDisplay.Instance != null)
                CrosswordClueDisplay.Instance.ClearClue();
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
                if (cells[r, c] != null)
                    cells[r, c].SetHighlighted(false);
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
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] == null) continue;
                if (cells[r, c].IsBlocked) continue;

                char ch = solutionLetters[r, c];
                if (ch != '\0')
                    cells[r, c].SetLetter(ch);
            }
        }
    }

    public void RequestHint()
    {
        Debug.Log("CrosswordBoardManager: Hint requested (not implemented yet).");
    }
}