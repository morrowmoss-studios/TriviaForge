using UnityEngine;
using System.Linq;

public class WordokuManager : MonoBehaviour
{
    [Header("References")]
    public WordokuBoard board; // Link this in the Inspector
    public string[] wordokuWords = { "DRAGONFLY", "MOONLIGHT", "CAMPFIRES" }; // sample 9-letter words

    private char[,] solution = new char[9, 9];   // final solution
    private char[,] startingBoard = new char[9, 9]; // what the player sees initially
    

    private void Start()
    {
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        string word = GetRandomWord();
        GenerateSolutionGrid(word);         // fill 'solution'
        GenerateStartingBoard();            // hide some letters
        board.GenerateBoard();              // generate UI grid
        PopulateBoardUI();                  // set cell texts
    }

    private string GetRandomWord()
    {
        return wordokuWords[Random.Range(0, wordokuWords.Length)].ToUpper();
    }

    private void GenerateSolutionGrid(string word)
    {
        // TODO: For now, fill solution with randomized 9-letter word — later swap in real solving algo
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

        // Hide ~40% of the tiles to create the puzzle
        for (int i = 0; i < 81 * 0.4f; i++)
        {
            int row = Random.Range(0, 9);
            int col = Random.Range(0, 9);
            startingBoard[row, col] = '\0'; // empty cell
        }
    }

    private void PopulateBoardUI()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = board.boardCells[row, col];
                char val = startingBoard[row, col];

                if (val != '\0')
                {
                    cell.SetLetter(val.ToString());
                    cell.SetLocked(true);  // pre-filled
                }
                else
                {
                    cell.SetLetter("");    // blank
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
    
    public void ShowHint()
    {
        // Simple: find first empty and fill it
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
