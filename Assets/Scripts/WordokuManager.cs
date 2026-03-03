using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public enum WordokuDifficulty
{
    Easy,
    Medium,
    Hard,
    Insanity
}

public class WordokuManager : MonoBehaviour
{
    [Header("References")]
    public WordokuBoard board;   // Assigned in Inspector

    [Header("Fallback Word List (used only if DB fails/empty)")]
    public string[] wordokuWords = { "DRAGONFLY", "STARBOUND", "CAMPFIRES", "MISTCLOUD", "WILDFROST" };

    [Header("Letter Choice UI")]
    [SerializeField] private LetterChoiceManager letterChoiceManager;

    [Header("Notes Mode")]
    [SerializeField] private bool notesMode = false;
    public bool NotesMode => notesMode;   // read-only for cells

    public bool enforceSolutionWhileTesting = true;

    [Header("Database")]
    [Tooltip("Resources file name (without .json). Default expects Assets/Resources/trivia_database.json")]
    [SerializeField] private string resourcesDbName = "trivia_database";

    [Tooltip("If DB lookup fails, use fallback word list above.")]
    [SerializeField] private bool fallbackToDefaultWords = true;

    private WordokuDifficulty difficulty = WordokuDifficulty.Medium;

    // PUBLIC READ-ONLY STATE
    public string CurrentWord { get; private set; }
    public char[] CurrentLetters { get; private set; }

    private char[,] solution = new char[9, 9];
    private char[,] startingBoard = new char[9, 9];

    private GameDatabase dbCached;

    private void Start()
    {
        // Wait one frame so the board + layout are fully built
        StartCoroutine(GenerateAfterLayout());
    }

    private IEnumerator GenerateAfterLayout()
    {
        yield return null;
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        SetDifficultyFromSession();

        CurrentWord = GetRandomWord();
        CurrentLetters = CurrentWord.ToCharArray();

        GenerateSolutionGrid(CurrentWord);
        GenerateStartingBoard();
        PopulateBoardUI();

        if (letterChoiceManager != null)
        {
            letterChoiceManager.PopulateFromWord(CurrentLetters);
        }

        UpdateLetterCompletion();
    }

    private void SetDifficultyFromSession()
    {
        string diffStr = TriviaSessionData.selectedDifficulty;

        switch (diffStr)
        {
            case "Easy":
                difficulty = WordokuDifficulty.Easy;
                break;
            case "Hard":
                difficulty = WordokuDifficulty.Hard;
                break;
            case "Insanity":
                difficulty = WordokuDifficulty.Insanity;
                break;
            case "Medium":
            default:
                difficulty = WordokuDifficulty.Medium;
                break;
        }

        Debug.Log($"[WordokuManager] Wordoku difficulty set to: {difficulty}");
    }

    // -------------------------------
    // DATABASE WORD PICKING (Category-only)
    // - Ignores subcategory completely
    // - Ignores word difficulty completely
    // - Difficulty only affects masking (GenerateStartingBoard)
    // -------------------------------

    private string GetRandomWord()
    {
        string catId = TriviaSessionData.selectedCategoryId;

        GameDatabase db = LoadDatabase();
        if (db == null)
            return FallbackWord("DB load failed.");

        CategoryData cat = db.categories?.Find(c => c.id == catId);
        if (cat == null)
            return FallbackWord($"Category '{catId}' not found.");

        if (cat.subcategories == null || cat.subcategories.Count == 0)
            return FallbackWord($"Category '{catId}' has no subcategories.");

        var candidates = new List<string>();

        foreach (var sub in cat.subcategories)
        {
            if (sub == null || sub.wordoku == null) continue;

            foreach (var entry in sub.wordoku)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.word))
                    continue;

                string word = entry.word.Trim().ToUpperInvariant();

                // Must be exactly 9 letters
                if (word.Length != 9)
                    continue;

                // Strongly recommended: 9 unique letters
                if (!HasAllUniqueLetters(word))
                    continue;

                candidates.Add(word);
            }
        }

        if (candidates.Count == 0)
            return FallbackWord($"No valid 9-letter unique-letter words found in category '{catId}'.");

        string pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        Debug.Log($"[WordokuManager] Picked '{pick}' from category '{catId}' (pool={candidates.Count}).");
        return pick;
    }

    private bool HasAllUniqueLetters(string word)
    {
        var set = new HashSet<char>();
        foreach (char ch in word)
        {
            if (ch < 'A' || ch > 'Z') return false;
            if (!set.Add(ch)) return false;
        }
        return set.Count == 9;
    }

    private GameDatabase LoadDatabase()
    {
        if (dbCached != null) return dbCached;

        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcesDbName);
        if (jsonAsset == null)
        {
            Debug.LogError($"[WordokuManager] Could not load Resources/{resourcesDbName}.json");
            return null;
        }

        try
        {
            dbCached = JsonUtility.FromJson<GameDatabase>(jsonAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError("[WordokuManager] Failed to parse GameDatabase JSON: " + ex.Message);
            dbCached = null;
        }

        return dbCached;
    }

    private string FallbackWord(string reason)
    {
        if (!fallbackToDefaultWords || wordokuWords == null || wordokuWords.Length == 0)
        {
            Debug.LogError($"[WordokuManager] No fallback words available. Reason: {reason}");
            return "DRAGONFLY"; // last-ditch
        }

        string pick = wordokuWords[UnityEngine.Random.Range(0, wordokuWords.Length)].ToUpperInvariant();
        Debug.LogWarning($"[WordokuManager] Falling back to default word '{pick}'. Reason: {reason}");
        return pick;
    }

    // -------------------------------
    // YOUR EXISTING PUZZLE LOGIC BELOW
    // -------------------------------

    private void GenerateSolutionGrid(string word)
    {
        char[] letters = word.ToCharArray();
        System.Random rng = new System.Random();

        // Shuffle the letters once (this defines the whole puzzle)
        char[] baseRow = letters.OrderBy(_ => rng.Next()).ToArray();

        // Generate solution using Sudoku shift pattern
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int shift =
                    (row % 3) * 3 +   // shift inside a block
                    (row / 3);        // shift between blocks

                int index = (col + shift) % 9;
                solution[row, col] = baseRow[index];
            }
        }
    }

    private void GenerateStartingBoard()
    {
        // Copy full solution
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                startingBoard[row, col] = solution[row, col];
            }
        }

        int emptiesMin, emptiesMax;
        string diff = TriviaSessionData.selectedDifficulty;

        switch (diff)
        {
            case "Easy":
                emptiesMin = 40;
                emptiesMax = 45;
                break;
            case "Medium":
                emptiesMin = 48;
                emptiesMax = 52;
                break;
            case "Hard":
                emptiesMin = 55;
                emptiesMax = 58;
                break;
            case "Insanity":
            default:
                emptiesMin = 60;
                emptiesMax = 64;
                break;
        }

        int emptiesTarget = UnityEngine.Random.Range(emptiesMin, emptiesMax + 1);
        emptiesTarget = Mathf.Clamp(emptiesTarget, 0, 81);

        var allCells = new List<(int row, int col)>(81);
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                allCells.Add((r, c));

        for (int i = 0; i < allCells.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, allCells.Count);
            (allCells[i], allCells[j]) = (allCells[j], allCells[i]);
        }

        for (int i = 0; i < emptiesTarget; i++)
        {
            var cell = allCells[i];
            startingBoard[cell.row, cell.col] = '\0';
        }

        int cluesLeft = 81 - emptiesTarget;
        Debug.Log($"[WordokuManager] Wordoku difficulty: {diff}, empties: {emptiesTarget}, clues left: {cluesLeft}");
    }

    private void PopulateBoardUI()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];
                char value = startingBoard[row, col];

                if (value != '\0')
                {
                    cell.SetLetter(value.ToString());
                    cell.SetLocked(true);
                }
                else
                {
                    cell.SetLetter("");
                    cell.SetLocked(false);
                }
            }
        }
    }

    public void ResetBoard()
    {
        PopulateBoardUI();
        UpdateLetterCompletion();
    }

    public void GiveHint()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];

                if (!cell.isLocked && string.IsNullOrEmpty(cell.GetLetter()))
                {
                    cell.SetLetter(solution[row, col].ToString());
                    UpdateLetterCompletion();
                    return;
                }
            }
        }
    }

    public void ToggleNotesMode()
    {
        notesMode = !notesMode;
        Debug.Log("Notes mode: " + (notesMode ? "ON" : "OFF"));
    }

    public bool IsValidPlacement(int row, int col, char letter)
    {
        bool inRow = IsInRow(row, letter);
        bool inCol = IsInColumn(col, letter);
        bool inBlock = IsInBlock(row, col, letter);

        return !(inRow || inCol || inBlock);
    }

    private bool IsInRow(int row, char letter)
    {
        for (int c = 0; c < 9; c++)
        {
            if (board.boardCells[row, c].GetLetter() == letter.ToString())
                return true;
        }
        return false;
    }

    private bool IsInColumn(int col, char letter)
    {
        for (int r = 0; r < 9; r++)
        {
            if (board.boardCells[r, col].GetLetter() == letter.ToString())
                return true;
        }
        return false;
    }

    private bool IsInBlock(int row, int col, char letter)
    {
        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;

        for (int r = startRow; r < startRow + 3; r++)
        {
            for (int c = startCol; c < startCol + 3; c++)
            {
                if (board.boardCells[r, c].GetLetter() == letter.ToString())
                    return true;
            }
        }
        return false;
    }

    public char GetSolutionLetter(int row, int col)
    {
        return solution[row, col];
    }

    public void NotifyBoardChanged()
    {
        UpdateLetterCompletion();
        CheckForWin();
    }

    private void UpdateLetterCompletion()
    {
        var totals = new Dictionary<char, int>();
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                char ch = solution[r, c];
                if (ch == '\0') continue;

                if (!totals.ContainsKey(ch))
                    totals[ch] = 0;

                totals[ch]++;
            }
        }

        var used = new Dictionary<char, int>();
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                string s = board.boardCells[r, c].GetLetter();
                if (string.IsNullOrEmpty(s)) continue;

                char ch = s[0];
                if (!used.ContainsKey(ch))
                    used[ch] = 0;

                used[ch]++;
            }
        }

        var allButtons = FindObjectsOfType<LetterChoiceButton>(true);
        foreach (var btn in allButtons)
        {
            char ch = btn.GetLetter();

            totals.TryGetValue(ch, out int total);
            used.TryGetValue(ch, out int count);

            bool completed = total > 0 && count >= total;
            btn.SetCompleted(completed);
        }
    }

    private void CheckForWin()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];

                string letter = cell.GetLetter();
                if (string.IsNullOrEmpty(letter))
                    return;

                char actual = letter[0];
                char expected = solution[row, col];

                if (actual != expected)
                    return;
            }
        }

        Debug.Log($"Wordoku solved! Word = {CurrentWord}, difficulty = {TriviaSessionData.selectedDifficulty}");
        GameWinController.TriggerWin("Wordoku", CurrentWord);
    }
}