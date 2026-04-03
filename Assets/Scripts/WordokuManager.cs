using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    public WordokuBoard board;

    [Header("Fallback Word List (used only if DB fails/empty)")]
    public string[] wordokuWords = { "DRAGONFLY", "STARBOUND", "CAMPFIRES", "MISTCLOUD", "WILDFROST" };

    [Header("Letter Choice UI")]
    [SerializeField] private LetterChoiceManager letterChoiceManager;

    [Header("Notes Mode")]
    [SerializeField] private bool notesMode = false;
    [SerializeField] private Button notesButton;
    [SerializeField] private Color notesActiveColor   = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color notesInactiveColor = Color.white;
    public bool NotesMode => notesMode;

    public bool enforceSolutionWhileTesting = true;

    [Header("Database")]
    [SerializeField] private string resourcesDbName = "trivia_database";
    [SerializeField] private bool fallbackToDefaultWords = true;

    private WordokuDifficulty difficulty = WordokuDifficulty.Medium;

    public string CurrentWord    { get; private set; }
    public char[] CurrentLetters { get; private set; }

    private char[,] solution      = new char[9, 9];
    private char[,] startingBoard = new char[9, 9];

    private GameDatabase dbCached;

    private void Start()
    {
        RefreshNotesButtonVisual();
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

        CurrentWord    = GetRandomWord();
        CurrentLetters = CurrentWord.ToCharArray();

        GenerateSolutionGrid(CurrentWord);
        GenerateStartingBoard();
        PopulateBoardUI();

        if (letterChoiceManager != null)
            letterChoiceManager.PopulateFromWord(CurrentLetters);

        UpdateLetterCompletion();
    }

    private void SetDifficultyFromSession()
    {
        string diffStr = TriviaSessionData.selectedDifficulty;

        switch (diffStr)
        {
            case "Easy":     difficulty = WordokuDifficulty.Easy;     break;
            case "Hard":     difficulty = WordokuDifficulty.Hard;     break;
            case "Insanity": difficulty = WordokuDifficulty.Insanity; break;
            case "Medium":
            default:         difficulty = WordokuDifficulty.Medium;   break;
        }

        Debug.Log($"[WordokuManager] Wordoku difficulty set to: {difficulty}");
    }

    // ── Database word picking ─────────────────────────────────────────────

    private string GetRandomWord()
    {
        string catId = TriviaSessionData.selectedCategoryId;

        GameDatabase db = LoadDatabase();
        if (db == null) return FallbackWord("DB load failed.");

        CategoryData cat = db.categories?.Find(c => c.id == catId);
        if (cat == null) return FallbackWord($"Category '{catId}' not found.");

        if (cat.subcategories == null || cat.subcategories.Count == 0)
            return FallbackWord($"Category '{catId}' has no subcategories.");

        var candidates = new List<string>();

        foreach (var sub in cat.subcategories)
        {
            if (sub == null || sub.wordoku == null) continue;

            foreach (var entry in sub.wordoku)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.word)) continue;

                string word = entry.word.Trim().ToUpperInvariant();
                if (word.Length != 9) continue;
                if (!HasAllUniqueLetters(word)) continue;

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

        try { dbCached = JsonUtility.FromJson<GameDatabase>(jsonAsset.text); }
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
            return "DRAGONFLY";
        }

        string pick = wordokuWords[UnityEngine.Random.Range(0, wordokuWords.Length)].ToUpperInvariant();
        Debug.LogWarning($"[WordokuManager] Falling back to default word '{pick}'. Reason: {reason}");
        return pick;
    }

    // ── Puzzle generation ─────────────────────────────────────────────────

    private void GenerateSolutionGrid(string word)
    {
        char[] letters = word.ToCharArray();
        System.Random rng = new System.Random();
        char[] baseRow = letters.OrderBy(_ => rng.Next()).ToArray();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int shift = (row % 3) * 3 + (row / 3);
                int index = (col + shift) % 9;
                solution[row, col] = baseRow[index];
            }
        }
    }

    private void GenerateStartingBoard()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                startingBoard[row, col] = solution[row, col];

        int emptiesMin, emptiesMax;
        string diff = TriviaSessionData.selectedDifficulty;

        switch (diff)
        {
            case "Easy":     emptiesMin = 40; emptiesMax = 45; break;
            case "Medium":   emptiesMin = 48; emptiesMax = 52; break;
            case "Hard":     emptiesMin = 55; emptiesMax = 58; break;
            case "Insanity":
            default:         emptiesMin = 60; emptiesMax = 64; break;
        }

        int emptiesTarget = Mathf.Clamp(UnityEngine.Random.Range(emptiesMin, emptiesMax + 1), 0, 81);

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

        Debug.Log($"[WordokuManager] Wordoku difficulty: {diff}, empties: {emptiesTarget}, clues left: {81 - emptiesTarget}");
    }

    private void PopulateBoardUI()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];
                char value = startingBoard[row, col];

                if (value != '\0') { cell.SetLetter(value.ToString()); cell.SetLocked(true); }
                else               { cell.SetLetter("");               cell.SetLocked(false); }
            }
        }
    }

    // ── Public actions ────────────────────────────────────────────────────

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
        RefreshNotesButtonVisual();
        Debug.Log("Notes mode: " + (notesMode ? "ON" : "OFF"));
    }

    // ── Notes button visual ───────────────────────────────────────────────

    private void RefreshNotesButtonVisual()
    {
        if (notesButton == null) return;

        var img = notesButton.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.color = notesMode ? notesActiveColor : notesInactiveColor;

        var tmp = notesButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = notesMode ? new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0.5f)
                                  : new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
    }

    // ── Validation ────────────────────────────────────────────────────────

    public bool IsValidPlacement(int row, int col, char letter)
    {
        return !(IsInRow(row, letter) || IsInColumn(col, letter) || IsInBlock(row, col, letter));
    }

    private bool IsInRow(int row, char letter)
    {
        for (int c = 0; c < 9; c++)
            if (board.boardCells[row, c].GetLetter() == letter.ToString()) return true;
        return false;
    }

    private bool IsInColumn(int col, char letter)
    {
        for (int r = 0; r < 9; r++)
            if (board.boardCells[r, col].GetLetter() == letter.ToString()) return true;
        return false;
    }

    private bool IsInBlock(int row, int col, char letter)
    {
        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;

        for (int r = startRow; r < startRow + 3; r++)
            for (int c = startCol; c < startCol + 3; c++)
                if (board.boardCells[r, c].GetLetter() == letter.ToString()) return true;

        return false;
    }

    public char GetSolutionLetter(int row, int col) => solution[row, col];

    // ── Board change notification ─────────────────────────────────────────

    public void NotifyBoardChanged()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayTilePlaced();

        UpdateLetterCompletion();
        CheckForWin();
    }

    // ── Letter completion tracking ────────────────────────────────────────

    private void UpdateLetterCompletion()
    {
        // Count how many times each letter appears in the solution
        var totals = new Dictionary<char, int>();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                char ch = solution[r, c];
                if (ch == '\0') continue;
                if (!totals.ContainsKey(ch)) totals[ch] = 0;
                totals[ch]++;
            }

        // Count how many times each letter has been placed on the board
        var used = new Dictionary<char, int>();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                string s = board.boardCells[r, c].GetLetter();
                if (string.IsNullOrEmpty(s)) continue;
                char ch = s[0];
                if (!used.ContainsKey(ch)) used[ch] = 0;
                used[ch]++;
            }

        // Find which letters are now fully placed
        var completedLetters = new HashSet<char>();
        foreach (var kvp in totals)
        {
            used.TryGetValue(kvp.Key, out int count);
            if (count >= kvp.Value)
                completedLetters.Add(kvp.Key);
        }

        // Auto-remove completed letters from all cell notes
        if (completedLetters.Count > 0)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    board.boardCells[r, c].RemoveNotesForLetters(completedLetters);
        }

        // Update letter choice buttons
        var allButtons = FindObjectsOfType<LetterChoiceButton>(true);
        foreach (var btn in allButtons)
        {
            char ch = btn.GetLetter();
            totals.TryGetValue(ch, out int total);
            used.TryGetValue(ch, out int count);
            btn.SetCompleted(total > 0 && count >= total);
        }
    }

    // ── Win check ─────────────────────────────────────────────────────────

    private void CheckForWin()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                string letter = board.boardCells[row, col].GetLetter();
                if (string.IsNullOrEmpty(letter)) return;
                if (letter[0] != solution[row, col]) return;
            }
        }

        Debug.Log($"Wordoku solved! Word = {CurrentWord}, difficulty = {TriviaSessionData.selectedDifficulty}");
        GameWinController.TriggerWin("Wordoku", CurrentWord);
    }
}