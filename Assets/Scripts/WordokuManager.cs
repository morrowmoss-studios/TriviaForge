using System.Collections;
using UnityEngine;
using System.Linq;

public class WordokuManager : MonoBehaviour
{
    [Header("References")]
    public WordokuBoard board;   // Assigned in Inspector

    [Header("Word List")]
    public string[] wordokuWords = { "DRAGONFLY", "MOONLIGHT", "CAMPFIRES" };

    // PUBLIC READ-ONLY STATE (important)
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

        // Board already exists – just populate
        PopulateBoardUI();
    }

    private string GetRandomWord()
    {
        return wordokuWords[Random.Range(0, wordokuWords.Length)].ToUpper();
    }

    private void GenerateSolutionGrid(string word)
    {
        char[] letters = word.ToCharArray();
        System.Random rng = new System.Random();

        for (int row = 0; row < 9; row++)
        {
            var shuffled = letters.OrderBy(_ => rng.Next()).ToArray();
            for (int col = 0; col < 9; col++)
            {
                solution[row, col] = shuffled[col];
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

    public void ResetBoard()
    {
        PopulateBoardUI();
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
                    return;
                }
            }
        }
    }
}
