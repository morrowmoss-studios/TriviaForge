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

    // selection state for tap-to-toggle direction
    private CrosswordCell _selectedCell;
    private bool _selectedAcross = true;

    [Header("Database")]
    [Tooltip("Resources file name (without .json). Default expects Assets/Resources/trivia_database.json")]
    [SerializeField] private string resourcesDbName = "trivia_database";

    [Tooltip("If true, fills the crossword from the DB based on ModeSelect session.")]
    [SerializeField] private bool fillFromDatabaseOnStart = true;

    [Tooltip("When pulling answers, allow reuse within the same puzzle if needed (not recommended).")]
    [SerializeField] private bool allowDuplicatesInPuzzle = false;

    [Header("Debug / Testing")]
    [Tooltip("If true, shows the solved letters (BUT only for placed words). Keep FALSE for release.")]
    [SerializeField] private bool autoFillSolutionOnStart = false;

    private GameDatabase dbCached;

    private void Awake()
    {
        // Make our words list available to other scenes (like the clues screen)
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
            RevealPlacedSolutionLettersOnly();
    }

    // -----------------------------
    // DB LOADING + SOLVER FILL
    // -----------------------------

    private void FillWordsFromDatabase()
    {
        string catId = TriviaSessionData.selectedCategoryId;

        Debug.Log($"[CrosswordBoardManager] Loading puzzle bank for category '{catId}'");

        GameDatabase db = LoadDatabase();
        if (db == null)
        {
            Debug.LogWarning("[CrosswordBoardManager] DB load failed.");
            return;
        }

        var rng = new System.Random();
        PreGeneratedPuzzle puzzle = null;

        bool isMixed = string.IsNullOrWhiteSpace(catId)
                       || catId == "mixed" || catId == "all" || catId == "mix"
                       || catId == "mixed_all";

        if (isMixed)
        {
            // Collect all puzzles from all categories and pick one at random
            var allPuzzles = new List<PreGeneratedPuzzle>();
            if (db.categories != null)
                foreach (var c in db.categories)
                    if (c.puzzles != null)
                        allPuzzles.AddRange(c.puzzles);

            if (allPuzzles.Count == 0)
            {
                Debug.LogWarning("[CrosswordBoardManager] No puzzles found across any category.");
                return;
            }

            puzzle = allPuzzles[rng.Next(allPuzzles.Count)];
        }
        else
        {
            CategoryData cat = db.categories?.Find(c => c.id == catId);
            if (cat == null)
            {
                Debug.LogWarning($"[CrosswordBoardManager] Category '{catId}' not found.");
                return;
            }

            if (cat.puzzles == null || cat.puzzles.Count == 0)
            {
                Debug.LogWarning($"[CrosswordBoardManager] No pre-generated puzzles for '{catId}'. " +
                                 "Re-run generate_puzzles.py and reimport trivia_database.json.");
                return;
            }

            puzzle = cat.puzzles[rng.Next(cat.puzzles.Count)];
        }

        if (puzzle.layoutRows == null || puzzle.placedWords == null || puzzle.placedWords.Count == 0)
        {
            Debug.LogWarning("[CrosswordBoardManager] Selected puzzle has no data.");
            return;
        }

        layoutRows = NormalizeLayout(puzzle.layoutRows.ToArray());

        words.Clear();
        foreach (var pw in puzzle.placedWords)
        {
            words.Add(new CrosswordWord
            {
                id        = pw.id,
                isAcross  = pw.isAcross,
                startRow  = pw.startRow,
                startCol  = pw.startCol,
                answer    = pw.answer,
                clue      = pw.clue
            });
        }

        Debug.Log($"[CrosswordBoardManager] Loaded puzzle — {words.Count} words, " +
                  $"{layoutRows.Length}×{layoutRows[0].Length} grid.");
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
        AssignCellNumbers();
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
    private void AssignCellNumbers()
    {
        if (cells == null) return;

        // Extract numeric part from word id (e.g. "3A" -> 3, "12D" -> 12)
        // and write it to the starting cell's NumberLabel
        foreach (var w in words)
        {
            if (w == null || string.IsNullOrWhiteSpace(w.id)) continue;

            // id format is e.g. "4A" or "12D" — strip trailing letter(s)
            string numStr = w.id.TrimEnd('A', 'D', 'a', 'd');

            int r = w.startRow;
            int c = w.startCol;

            if (r < 0 || r >= rows || c < 0 || c >= cols) continue;
            if (cells[r, c] == null || cells[r, c].IsBlocked) continue;

            // Don't overwrite if a higher-priority number is already there
            // (when across and down share the same start cell, same number applies)
            cells[r, c].SetNumber(numStr);
        }
    }
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

        CrosswordWord acrossWord = acrossAt[r, c];
        CrosswordWord downWord   = downAt[r, c];

        bool hasAcross = acrossWord != null;
        bool hasDown   = downWord   != null;

        // Nothing here at all
        if (!hasAcross && !hasDown)
        {
            CrosswordClueDisplay.Instance?.ClearClue();
            return;
        }

        // Decide direction:
        // - If only one direction exists, always use that
        // - If both exist and this is the SAME cell as last tap, toggle
        // - If both exist and this is a NEW cell, prefer across
        bool goAcross;
        if (hasAcross && !hasDown)
            goAcross = true;
        else if (hasDown && !hasAcross)
            goAcross = false;
        else if (_selectedCell == cell)
            goAcross = !_selectedAcross;   // toggle on repeat tap
        else
            goAcross = true;               // new cell — default to across

        _selectedCell   = cell;
        _selectedAcross = goAcross;

        ClearHighlights();

        CrosswordWord activeWord   = goAcross ? acrossWord : downWord;
        CrosswordWord inactiveWord = goAcross ? downWord   : acrossWord;

        // Clue display — active clue first with ▶, inactive below
        if (CrosswordClueDisplay.Instance != null)
        {
            if (inactiveWord != null)
                CrosswordClueDisplay.Instance.ShowMultiClue(
                    activeWord.id,   activeWord.clue,
                    inactiveWord.id, inactiveWord.clue);
            else
                CrosswordClueDisplay.Instance.ShowClue(activeWord.id, activeWord.clue);
        }

        // Highlight
        if (goAcross) HighlightAcrossWord(r, c);
        else          HighlightDownWord(r, c);
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