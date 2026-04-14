using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CrosswordWord
{
    public string id;
    public bool isAcross;
    public int startRow;
    public int startCol;
    public string answer;
    [TextArea]
    public string clue;
}

public class CrosswordBoardManager : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    [SerializeField] private CrosswordCell cellPrefab;
    [SerializeField] private Transform gridParent;

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

    private CrosswordWord[,] acrossAt;
    private CrosswordWord[,] downAt;
    private char[,] solutionLetters;

    private CrosswordCell _selectedCell;
    private bool _selectedAcross = true;
    private CrosswordCell _highlightedAnchor;

    // ── Hints ─────────────────────────────────────────────────────────────
    private const int MaxHints = 3;
    private int hintsRemaining   = MaxHints;
    private int _wrongPlacements = 0;

    public int  HintsRemaining => hintsRemaining;
    public bool UsedNoHints    => hintsRemaining == MaxHints;

    public event Action<int> OnHintsChanged;

    [Header("Database")]
    [SerializeField] private string resourcesDbName = "trivia_database";
    [SerializeField] private bool fillFromDatabaseOnStart = true;
    [SerializeField] private bool allowDuplicatesInPuzzle = false;

    [Header("Debug / Testing")]
    [Tooltip("Keep FALSE for release.")]
    [SerializeField] private bool autoFillSolutionOnStart = false;

    // ── Mobile keyboard ───────────────────────────────────────────────────
    private TouchScreenKeyboard keyboard;
    private string lastKeyboardText = "";
    private const string KeyboardSentinel = "|";

    private GameDatabase dbCached;

    // Prevents win from firing multiple times
    private bool puzzleSolved = false;

    private void Awake()
    {
        CrosswordSession.currentWords = words;
    }

    private void Start()
    {
        layoutRows = NormalizeLayout(layoutRows);

        if (fillFromDatabaseOnStart)
            FillWordsFromDatabase();

        BuildBoard();

        if (autoFillSolutionOnStart)
            RevealPlacedSolutionLettersOnly();
    }

    // ── Keyboard input ────────────────────────────────────────────────────

    private void Update()
    {
        if (keyboard != null && TouchScreenKeyboard.isSupported && keyboard.active)
        {
            string text = keyboard.text;

            if (text.Length > lastKeyboardText.Length)
            {
                char newChar = char.ToUpper(text[text.Length - 1]);

                if (newChar >= 'A' && newChar <= 'Z' && _selectedCell != null)
                {
                    // If the current cell is already filled, absorb the keystroke
                    // silently and just advance -- the player typed that letter but
                    // it was already there from an intersecting word
                    if (_selectedCell.GetLetter() != '\0')
                    {
                        AdvanceToNextCell();
                    }
                    else
                    {
                        _selectedCell.SetLetter(newChar);
                        if (newChar != solutionLetters[_selectedCell.row, _selectedCell.col])
                            _wrongPlacements++;
                        AdvanceToNextCell();
                        CheckForWin();
                    }
                }

                keyboard.text = KeyboardSentinel;
                lastKeyboardText = KeyboardSentinel;
            }
            else if (text.Length < lastKeyboardText.Length)
            {
                if (_selectedCell != null)
                    _selectedCell.SetLetter('\0');

                keyboard.text = KeyboardSentinel;
                lastKeyboardText = KeyboardSentinel;
            }
            else
            {
                lastKeyboardText = text;
            }
        }

#if UNITY_EDITOR
        if (_selectedCell != null)
        {
            var keyboard2 = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard2 != null)
            {
                if (keyboard2.backspaceKey.wasPressedThisFrame)
                {
                    _selectedCell.SetLetter('\0');
                }
                else
                {
                    foreach (var key in keyboard2.allKeys)
                    {
                        if (!key.wasPressedThisFrame) continue;
                        string keyName = key.name;
                        if (keyName.Length == 1)
                        {
                            char ch = char.ToUpper(keyName[0]);
                            if (ch >= 'A' && ch <= 'Z')
                            {
                                if (_selectedCell.GetLetter() != '\0')
                                {
                                    AdvanceToNextCell();
                                }
                                else
                                {
                                    _selectedCell.SetLetter(ch);
                                    if (ch != solutionLetters[_selectedCell.row, _selectedCell.col])
                                        _wrongPlacements++;
                                    AdvanceToNextCell();
                                    CheckForWin();
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
#endif
    }

    private void OpenKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open(
            KeyboardSentinel,
            TouchScreenKeyboardType.Default,
            false, false, false, false, "", 0
        );

        lastKeyboardText = KeyboardSentinel;
        if (keyboard != null) keyboard.text = KeyboardSentinel;
    }

    private void AdvanceToNextCell()
    {
        if (_selectedCell == null) return;

        int r = _selectedCell.row;
        int c = _selectedCell.col;

        if (_selectedAcross)
        {
            int nextC = c + 1;
            if (nextC < cols && !cells[r, nextC].IsBlocked)
            {
                SelectCell(cells[r, nextC], true);
                return;
            }

            // End of word -- jump to first empty cell in the word if any
            int startC = c;
            while (startC - 1 >= 0 && !cells[r, startC - 1].IsBlocked) startC--;
            for (int sc = startC; sc < cols && !cells[r, sc].IsBlocked; sc++)
            {
                if (cells[r, sc].GetLetter() == '\0')
                {
                    SelectCell(cells[r, sc], true);
                    return;
                }
            }
        }
        else
        {
            int nextR = r + 1;
            if (nextR < rows && !cells[nextR, c].IsBlocked)
            {
                SelectCell(cells[nextR, c], false);
                return;
            }

            // End of word -- jump to first empty cell in the word if any
            int startR = r;
            while (startR - 1 >= 0 && !cells[startR - 1, c].IsBlocked) startR--;
            for (int sr = startR; sr < rows && !cells[sr, c].IsBlocked; sr++)
            {
                if (cells[sr, c].GetLetter() == '\0')
                {
                    SelectCell(cells[sr, c], false);
                    return;
                }
            }
        }
    }

    private void SelectCell(CrosswordCell cell, bool isAcross)
    {
        _selectedCell   = cell;
        _selectedAcross = isAcross;

        ClearHighlights();

        int r = cell.row;
        int c = cell.col;

        CrosswordWord acrossWord   = acrossAt[r, c];
        CrosswordWord downWord     = downAt[r, c];
        CrosswordWord activeWord   = isAcross ? acrossWord : downWord;
        CrosswordWord inactiveWord = isAcross ? downWord   : acrossWord;

        if (CrosswordClueDisplay.Instance != null)
        {
            if (activeWord != null && inactiveWord != null)
                CrosswordClueDisplay.Instance.ShowMultiClue(
                    activeWord.id, activeWord.clue, activeWord.answer?.Length ?? 0,
                    inactiveWord.id, inactiveWord.clue, inactiveWord.answer?.Length ?? 0);
            else if (activeWord != null)
                CrosswordClueDisplay.Instance.ShowClue(activeWord.id, activeWord.clue, activeWord.answer?.Length ?? 0);
        }

        if (isAcross) HighlightAcrossWord(r, c);
        else          HighlightDownWord(r, c);

        if (activeWord != null)
        {
            _highlightedAnchor = cells[activeWord.startRow, activeWord.startCol];
            _highlightedAnchor.SetNumberHighlighted(true);
        }
    }

    // ── DB loading ────────────────────────────────────────────────────────

    private void FillWordsFromDatabase()
    {
        string catId = TriviaSessionData.selectedCategoryId;

        PreGeneratedPuzzle puzzle = CrosswordSession.activePuzzle;

        if (puzzle == null)
        {
            Debug.Log($"[CrosswordBoardManager] Loading puzzle bank for category '{catId}'");

            GameDatabase db = LoadDatabase();
            if (db == null)
            {
                Debug.LogWarning("[CrosswordBoardManager] DB load failed.");
                return;
            }

            var rng = new System.Random();

            bool isMixed = string.IsNullOrWhiteSpace(catId)
                           || catId == "mixed" || catId == "all" || catId == "mix"
                           || catId == "mixed_all";

            if (isMixed)
            {
                var eligibleCats = db.categories?.Where(c => c.puzzles != null && c.puzzles.Count > 0).ToList();
                if (eligibleCats == null || eligibleCats.Count == 0)
                {
                    Debug.LogWarning("[CrosswordBoardManager] No puzzles found across any category.");
                    return;
                }
                var randomCat = eligibleCats[rng.Next(eligibleCats.Count)];
                puzzle = SeenContentTracker.PickUnseenCrossword(randomCat.id, randomCat.puzzles, rng);
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
                    Debug.LogWarning($"[CrosswordBoardManager] No pre-generated puzzles for '{catId}'.");
                    return;
                }

                puzzle = SeenContentTracker.PickUnseenCrossword(catId, cat.puzzles, rng);
            }

            CrosswordSession.activePuzzle = puzzle;
        }
        else
        {
            Debug.Log("[CrosswordBoardManager] Resuming cached puzzle.");
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
                id       = pw.id,
                isAcross = pw.isAcross,
                startRow = pw.startRow,
                startCol = pw.startCol,
                answer   = pw.answer,
                clue     = pw.clue
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

        try { dbCached = JsonUtility.FromJson<GameDatabase>(jsonAsset.text); }
        catch (Exception ex)
        {
            Debug.LogError("[CrosswordBoardManager] Failed to parse GameDatabase JSON: " + ex.Message);
            dbCached = null;
        }

        return dbCached;
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

    // ── Board building ────────────────────────────────────────────────────

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

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        cells           = new CrosswordCell[rows, cols];
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

        hintsRemaining = MaxHints;
        OnHintsChanged?.Invoke(hintsRemaining);

        puzzleSolved = false;
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
            if (w == null || string.IsNullOrWhiteSpace(w.answer)) continue;

            string ans = w.answer.ToUpperInvariant();

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

        // Second pass: guarantee every word's start cell always maps to itself,
        // regardless of iteration order above. Prevents a longer word from
        // overwriting the start cell of a shorter word that begins mid-span.
        foreach (var w in words)
        {
            if (w == null) continue;

            int r = w.startRow;
            int c = w.startCol;

            if (r < 0 || r >= rows || c < 0 || c >= cols) continue;
            if (cells[r, c].IsBlocked) continue;

            if (w.isAcross) acrossAt[r, c] = w;
            else            downAt[r, c]   = w;
        }
    }

    private void AssignCellNumbers()
    {
        if (cells == null) return;

        foreach (var w in words)
        {
            if (w == null || string.IsNullOrWhiteSpace(w.id)) continue;

            string numStr = w.id.TrimEnd('A', 'D', 'a', 'd');

            int r = w.startRow;
            int c = w.startCol;

            if (r < 0 || r >= rows || c < 0 || c >= cols) continue;
            if (cells[r, c] == null || cells[r, c].IsBlocked) continue;

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
                if (!belongs) { cells[r, c].SetLetter('\0'); continue; }

                cells[r, c].SetLetter(solutionLetters[r, c]);
            }
        }
    }

    // ── Cell click handling ───────────────────────────────────────────────

    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        int r = cell.row;
        int c = cell.col;

        CrosswordWord acrossWord = acrossAt[r, c];
        CrosswordWord downWord   = downAt[r, c];

        bool hasAcross = acrossWord != null;
        bool hasDown   = downWord   != null;

        if (!hasAcross && !hasDown)
        {
            CrosswordClueDisplay.Instance?.ClearClue();
            return;
        }

        bool goAcross;
        if (hasAcross && !hasDown)        goAcross = true;
        else if (hasDown && !hasAcross)   goAcross = false;
        else if (_selectedCell == cell)   goAcross = !_selectedAcross;
        else                              goAcross = true;

        _selectedCell   = cell;
        _selectedAcross = goAcross;

        ClearHighlights();

        CrosswordWord activeWord   = goAcross ? acrossWord : downWord;
        CrosswordWord inactiveWord = goAcross ? downWord   : acrossWord;

        if (CrosswordClueDisplay.Instance != null)
        {
            if (inactiveWord != null)
                CrosswordClueDisplay.Instance.ShowMultiClue(
                    activeWord.id,   activeWord.clue,   activeWord.answer?.Length ?? 0,
                    inactiveWord.id, inactiveWord.clue, inactiveWord.answer?.Length ?? 0);
            else
                CrosswordClueDisplay.Instance.ShowClue(activeWord.id, activeWord.clue, activeWord.answer?.Length ?? 0);
        }

        if (goAcross) HighlightAcrossWord(r, c);
        else          HighlightDownWord(r, c);

        _highlightedAnchor = cells[activeWord.startRow, activeWord.startCol];
        _highlightedAnchor.SetNumberHighlighted(true);

        OpenKeyboard();
    }

    // ── Highlight helpers ─────────────────────────────────────────────────

    private void ClearHighlights()
    {
        if (cells == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                cells[r, c]?.SetHighlighted(false);

        _highlightedAnchor?.SetNumberHighlighted(false);
        _highlightedAnchor = null;
    }

    private void HighlightAcrossWord(int row, int col)
    {
        if (cells == null || cells[row, col].IsBlocked) return;

        int startCol = col;
        while (startCol - 1 >= 0 && !cells[row, startCol - 1].IsBlocked) startCol--;

        int endCol = col;
        while (endCol + 1 < cols && !cells[row, endCol + 1].IsBlocked) endCol++;

        for (int c = startCol; c <= endCol; c++)
            cells[row, c].SetHighlighted(true);
    }

    private void HighlightDownWord(int row, int col)
    {
        if (cells == null || cells[row, col].IsBlocked) return;

        int startRow = row;
        while (startRow - 1 >= 0 && !cells[startRow - 1, col].IsBlocked) startRow--;

        int endRow = row;
        while (endRow + 1 < rows && !cells[endRow + 1, col].IsBlocked) endRow++;

        for (int r = startRow; r <= endRow; r++)
            cells[r, col].SetHighlighted(true);
    }

    // ── Hint ─────────────────────────────────────────────────────────────

    public void RequestHint()
    {
        if (hintsRemaining <= 0)
        {
            Debug.Log("[CrosswordBoardManager] No hints remaining.");
            return;
        }

        if (_selectedCell == null)
        {
            Debug.Log("[CrosswordBoardManager] No cell selected for hint.");
            return;
        }

        int r = _selectedCell.row;
        int c = _selectedCell.col;
        char solution = solutionLetters[r, c];

        if (solution == '\0')
        {
            Debug.Log("[CrosswordBoardManager] Selected cell has no solution letter.");
            return;
        }

        if (_selectedCell.GetLetter() == solution)
        {
            Debug.Log("[CrosswordBoardManager] Cell already correct — hint not consumed.");
            return;
        }

        _selectedCell.SetLetter(solution);
        hintsRemaining--;
        OnHintsChanged?.Invoke(hintsRemaining);

        CheckForWin();

        Debug.Log($"[CrosswordBoardManager] Hint used. {hintsRemaining} remaining.");
    }

    // ── Win check ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fires when all playable cells are filled.
    /// Wrong cells get the scarlet letter of shame (red).
    /// Triggers win only if everything is correct.
    /// </summary>
    private void CheckForWin()
    {
        if (puzzleSolved || cells == null) return;

        // First pass — bail out if any playable cell is still empty
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] == null || cells[r, c].IsBlocked) continue;
                if (solutionLetters[r, c] == '\0') continue;
                if (cells[r, c].GetLetter() == '\0') return;
            }
        }

        // All filled — second pass: mark wrong cells red, check for full correctness
        bool allCorrect = true;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] == null || cells[r, c].IsBlocked) continue;
                if (solutionLetters[r, c] == '\0') continue;

                bool correct = cells[r, c].GetLetter() == solutionLetters[r, c];
                cells[r, c].SetWrong(!correct);

                if (!correct) allCorrect = false;
            }
        }

        if (allCorrect)
        {
            puzzleSolved = true;
            Debug.Log("[CrosswordBoardManager] Puzzle solved!");
            TriviaSessionData.crosswordWrongPlacements = _wrongPlacements;
            TriviaSessionData.crosswordPerfectGame     = _wrongPlacements == 0;
            GameWinController.TriggerWin("Crossword");
        }
        else
        {
            Debug.Log("[CrosswordBoardManager] Board full but has errors — wrong cells marked red.");
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────

    public void ResetPuzzle()
    {
        if (cells == null) return;

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (cells[r, c] != null && !cells[r, c].IsBlocked)
                    cells[r, c].SetLetter('\0');

        puzzleSolved     = false;
        _wrongPlacements = 0;
        _selectedCell    = null;
        ClearHighlights();
        CrosswordClueDisplay.Instance?.ClearClue();

        Debug.Log("[CrosswordBoardManager] Puzzle reset.");
    }
}