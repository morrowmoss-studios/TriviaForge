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
    public string clue;        // clue text
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
    [Tooltip("If true, shows the solved letters (BUT only for placed words).")]
    [SerializeField] private bool autoFillSolutionOnStart = true;

    private GameDatabase dbCached;

    private void Awake()
    {
        // NOTE: Do NOT set CrosswordSession.currentWords here.
        // Awake runs before Start, so writing here would wipe any valid session
        // data we need to read in Start() for the Clues back-button restore.
        // CrosswordSession is updated after generation in FillWordsFromDatabase instead.
    }

    private void Start()
    {
        // If we're returning from the Clues screen, CrosswordSession already holds
        // the current puzzle — reuse it instead of generating a new one.
        if (CrosswordSession.currentWords != null && CrosswordSession.currentWords.Count > 0)
        {
            words.Clear();
            words.AddRange(CrosswordSession.currentWords);
            Debug.Log($"[CrosswordBoardManager] Restored {words.Count} words from session.");
        }
        else
        {
            // Normalize layout so EVERYTHING agrees on dimensions
            layoutRows = NormalizeLayout(layoutRows);

            if (fillFromDatabaseOnStart)
                FillWordsFromDatabase();
        }

        // Always rebuild the visual board from whatever words are now loaded
        if (words.Count > 0)
            RebuildLayoutFromWords();

        BuildBoard();

        if (autoFillSolutionOnStart)
            RevealPlacedSolutionLettersOnly();
    }

    /// <summary>
    /// Reconstructs layoutRows from the placed words so BuildBoard draws the
    /// correct blocked/open cells when restoring a session.
    /// </summary>
    private void RebuildLayoutFromWords()
    {
        // Find the bounding box of all placed word cells
        int maxR = 0, maxC = 0;
        foreach (var w in words)
        {
            if (w == null) continue;
            int endR = w.startRow + (w.isAcross ? 0 : w.answer.Length - 1);
            int endC = w.startCol + (w.isAcross ? w.answer.Length - 1 : 0);
            if (endR > maxR) maxR = endR;
            if (endC > maxC) maxC = endC;
        }

        int rows2 = Mathf.Max(maxR + 1, 10);
        int cols2 = Mathf.Max(maxC + 1, 10);

        // Mark every cell that belongs to a word as open
        bool[,] open = new bool[rows2, cols2];
        foreach (var w in words)
        {
            if (w == null) continue;
            for (int i = 0; i < w.answer.Length; i++)
            {
                int r = w.startRow + (w.isAcross ? 0 : i);
                int c = w.startCol + (w.isAcross ? i : 0);
                if (r < rows2 && c < cols2)
                    open[r, c] = true;
            }
        }

        var rebuilt = new string[rows2];
        for (int r = 0; r < rows2; r++)
        {
            var sb = new System.Text.StringBuilder(cols2);
            for (int c = 0; c < cols2; c++)
                sb.Append(open[r, c] ? '.' : '#');
            rebuilt[r] = sb.ToString();
        }

        layoutRows = rebuilt;
    }

    // -----------------------------
    // DB LOADING + SOLVER FILL
    // -----------------------------

    private void FillWordsFromDatabase()
    {
        string catId = TriviaSessionData.selectedCategoryId;
        string subId = TriviaSessionData.selectedSubcategoryId;

        string selectedDiff = TriviaSessionData.selectedDifficulty;
        string diffKey      = NormalizeDifficulty(selectedDiff);
        bool   mixed        = IsMixedDifficulty(selectedDiff);

        Debug.Log($"[CrosswordBoardManager] FillWordsFromDatabase START | " +
                  $"cat='{catId}' sub='{subId}' diff='{diffKey}' mixed={mixed}");

        GameDatabase db = LoadDatabase();
        if (db == null)
        {
            Debug.LogWarning("[CrosswordBoardManager] DB load failed.");
            return;
        }

        // ── collect crossword entries ─────────────────────────────────────────
        // Use ALL difficulty levels for the generator pool.
        // Difficulty only controls which CLUE is shown to the player, not which
        // words are available — filtering by difficulty was the root cause of
        // the old generator failing on "easy" mode with no long words.
        var pool = new List<CrosswordEntry>();

        CategoryData cat = db.categories?.Find(c => c.id == catId);
        if (cat == null)
        {
            Debug.LogWarning($"[CrosswordBoardManager] Category '{catId}' not found.");
            return;
        }

        // Collect from ALL subcategories in the category.
        // Crossword selection is category-level (same as Wordoku) so subId is ignored.
        if (cat.subcategories != null)
        {
            foreach (var sub in cat.subcategories)
            {
                if (sub?.crosswords == null) continue;
                foreach (var e in sub.crosswords)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.answer) ||
                        string.IsNullOrWhiteSpace(e.clue)) continue;

                    string ans = CleanAnswer(e.answer);
                    if (ans.Length < 3 || ans.Length > 9) continue;

                    pool.Add(new CrosswordEntry
                    {
                        id         = e.id,
                        answer     = ans,
                        clue       = e.clue.Trim(),
                        difficulty = NormalizeDifficulty(e.difficulty)
                    });
                }
            }
        }

        Debug.Log($"[CrosswordBoardManager] Total pool size: {pool.Count}");

        if (pool.Count < 20)
        {
            Debug.LogWarning("[CrosswordBoardManager] Pool too small (< 20). Cannot generate puzzle.");
            return;
        }

        // ── generate puzzle ───────────────────────────────────────────────────
        var generated = CrosswordGenerator.Generate(
            pool,
            rows:         10,
            cols:         10,
            targetWords:  18,
            seed:         0,          // 0 = random seed each time
            maxAttempts:  40,
            maxIterPerAttempt: 6000
        );

        if (generated == null || generated.placedWords == null ||
            generated.placedWords.Count == 0)
        {
            Debug.LogError("[CrosswordBoardManager] CrosswordGenerator failed to place any words. " +
                           "Add more words to the pool (aim for 150+ per category, especially 3-6 letter words).");
            words.Clear();
            CrosswordSession.currentWords = words;
            return;
        }

        // ── apply result to board ─────────────────────────────────────────────
        // Update the layout so BuildBoard() draws the right blocked cells
        layoutRows = NormalizeLayout(generated.layoutRows);

        words.Clear();
        words.AddRange(generated.placedWords);

        Debug.Log($"[CrosswordBoardManager] Generator SUCCESS — {words.Count} words placed.");
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

    private static bool IsMixedDifficulty(string diff)
    {
        if (string.IsNullOrWhiteSpace(diff)) return false;
        diff = diff.Trim().ToLowerInvariant();
        return diff == "mixed" || diff == "mix" || diff == "all";
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

        if (diff == "mixed" || diff == "mix" || diff == "all") return "mixed";

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

    // -----------------------------
    // BUILD + PLAY LOGIC
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

        // clear old
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

        IndexWordsAndBuildSolution();
    }

    private void IndexWordsAndBuildSolution()
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

            // Place letters; solver guarantees no conflicts, but still guard bounds/blocks
            for (int i = 0; i < ans.Length; i++)
            {
                int rr = w.startRow + (w.isAcross ? 0 : i);
                int cc = w.startCol + (w.isAcross ? i : 0);

                if (rr < 0 || rr >= rows || cc < 0 || cc >= cols) break;
                if (cells[rr, cc].IsBlocked) break;

                if (w.isAcross) acrossAt[rr, cc] = w;
                else            downAt[rr, cc]   = w;

                solutionLetters[rr, cc] = ans[i];
            }
        }
    }

    // IMPORTANT: reveal letters ONLY if that cell belongs to a placed word.
    // This prevents "nonsense down words" from appearing just because across letters line up.
    private void RevealPlacedSolutionLettersOnly()
    {
        if (cells == null || solutionLetters == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] == null || cells[r, c].IsBlocked) continue;

                bool belongs = (acrossAt[r, c] != null) || (downAt[r, c] != null);
                if (!belongs)
                {
                    cells[r, c].SetLetter('\0');
                    continue;
                }

                char ch = solutionLetters[r, c];
                cells[r, c].SetLetter(ch);
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

        if (word.isAcross) HighlightAcrossWord(r, c);
        else HighlightDownWord(r, c);
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

    public void RequestHint()
    {
        Debug.Log("CrosswordBoardManager: Hint requested (not implemented yet).");
    }
}