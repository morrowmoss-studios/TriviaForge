using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WordokuManager : MonoBehaviour
{
    [Header("References")]
    public WordokuBoard board;   // Assigned in Inspector

    [Header("Word List")]
    public string[] wordokuWords = { "DRAGONFLY", "STARBOUND", "CAMPFIRES", "MISTCLOUD", "WILDFROST" };

    [Header("Letter Choice UI")]
    [SerializeField] private LetterChoiceManager letterChoiceManager;

    [Header("Notes Mode")]
    [SerializeField] private bool notesMode = false;
    public bool NotesMode => notesMode;   // read-only for cells

    public bool enforceSolutionWhileTesting = true;

    // PUBLIC READ-ONLY STATE
    public string CurrentWord { get; private set; }
    public char[] CurrentLetters { get; private set; }

    private char[,] solution = new char[9, 9];
    private char[,] startingBoard = new char[9, 9];

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

    private string GetRandomWord()
    {
        return wordokuWords[Random.Range(0, wordokuWords.Length)].ToUpper();
    }

    private void GenerateSolutionGrid(string word)
    {
        char[] letters = word.ToCharArray();
        System.Random rng = new System.Random();

        // 1️⃣ Shuffle the letters once (this defines the whole puzzle)
        char[] baseRow = letters.OrderBy(_ => rng.Next()).ToArray();

        // 2️⃣ Generate solution using Sudoku shift pattern
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

        // Remove ~40% of letters
        int removals = Mathf.RoundToInt(81 * 0.4f);
        for (int i = 0; i < removals; i++)
        {
            int row = Random.Range(0, 9);
            int col = Random.Range(0, 9);
            startingBoard[row, col] = '\0';
        }
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

    // Called by the Reset button
    public void ResetBoard()
    {
        PopulateBoardUI();
        UpdateLetterCompletion();
    }

    // Called by the Hint button
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

    // 🔵 NOTES MODE TOGGLE – hook this to the Notes button OnClick
    public void ToggleNotesMode()
    {
        notesMode = !notesMode;
        Debug.Log("Notes mode: " + (notesMode ? "ON" : "OFF"));
        // Later you can update the Notes button visual here if you want
    }

    public bool IsValidPlacement(int row, int col, char letter)
    {
        bool inRow   = IsInRow(row, letter);
        bool inCol   = IsInColumn(col, letter);
        bool inBlock = IsInBlock(row, col, letter);

        Debug.Log(
            $"Check {letter} at [{row},{col}]  " +
            $"RowHas:{inRow} ColHas:{inCol} BlockHas:{inBlock}"
        );

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

    // Called whenever the board state changes (player move, reset, hint, etc.)
    public void NotifyBoardChanged()
    {
        UpdateLetterCompletion();
    }

    // Recalculate which letters are "finished" and hide their buttons
    private void UpdateLetterCompletion()
    {
        // 1) Total copies of each letter in the *solution* grid
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

        // 2) How many of each letter are currently on the *board* (locked + player)
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

        // 3) For each letter button, hide it when fully used
        var allButtons = FindObjectsOfType<LetterChoiceButton>(true); // true = include inactive
        foreach (var btn in allButtons)
        {
            char ch = btn.GetLetter();

            totals.TryGetValue(ch, out int total);
            used.TryGetValue(ch, out int count);

            bool completed = total > 0 && count >= total;
            btn.SetCompleted(completed);
        }
        
    }
    
    public void AutoSolve()
    {
        if (board == null || board.boardCells == null)
        {
            Debug.LogError("WordokuManager.AutoSolve: Board not ready.");
            return;
        }

        // Fill every cell with the solution letter
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];
                char ch = solution[row, col];

                // Make sure we can stomp whatever is there
                cell.SetLocked(false);
                cell.SetLetter(ch.ToString());
                cell.SetLocked(true);   // treat them as "solved" clues
            }
        }

        // Update letter buttons on the left so everything is consistent
        UpdateLetterCompletion();
    }
    
    private void Update()
    {
#if UNITY_EDITOR
        // Backquote (`) key above Tab / left of 1
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Debug.Log("DEV AUTOSOLVE triggered via ` key");
            AutoSolve();
        }
#endif
    }

}
