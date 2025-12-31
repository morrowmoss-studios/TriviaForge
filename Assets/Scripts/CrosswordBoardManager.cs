using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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
        "..####....",
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

    [Header("Words & Clues")]
    public List<CrosswordWord> words = new List<CrosswordWord>();

    // quick lookup: which word(s) live on each cell
    private CrosswordWord[,] acrossAt;
    private CrosswordWord[,] downAt;

    private void Start()
    {
        BuildBoard();
    }

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

        // derive size from layout so you don't have to keep numbers in sync
        rows = layoutRows.Length;
        cols = layoutRows[0].Length;

        // nuke any old children under the grid (just in case)
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(gridParent.GetChild(i).gameObject);
        }

        cells = new CrosswordCell[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            string rowString = layoutRows[r];

            // safety if one line is shorter
            if (rowString.Length < cols)
            {
                rowString = rowString.PadRight(cols, '.');
            }

            for (int c = 0; c < cols; c++)
            {
                var cellInstance = Instantiate(cellPrefab, gridParent);
                cellInstance.name = $"Cell_{r}_{c}";

                bool blocked = rowString[c] == '#';

                // init with our existing API
                cellInstance.Init(this, r, c, blocked);

                cells[r, c] = cellInstance;
            }
        }

        IndexWords();
    }

    private void IndexWords()
    {
        acrossAt = new CrosswordWord[rows, cols];
        downAt   = new CrosswordWord[rows, cols];

        foreach (var w in words)
        {
            if (string.IsNullOrEmpty(w.answer)) continue;

            int r = w.startRow;
            int c = w.startCol;

            for (int i = 0; i < w.answer.Length; i++)
            {
                int rr = r + (w.isAcross ? 0 : i);
                int cc = c + (w.isAcross ? i : 0);

                if (rr < 0 || rr >= rows || cc < 0 || cc >= cols)
                {
                    Debug.LogWarning($"Word {w.id} runs off the board.");
                    break;
                }

                if (cells[rr, cc].IsBlocked)
                {
                    Debug.LogWarning($"Word {w.id} hits a blocked cell at {rr},{cc}.");
                    break;
                }

                if (w.isAcross) acrossAt[rr, cc] = w;
                else            downAt[rr, cc]   = w;

                // later we’ll also store the solution letter here
            }
        }
    }

    // Called by CrosswordCell.OnPointerClick(this)
    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        int r = cell.row;
        int c = cell.col;

        ClearHighlights();

        // prefer across if there is one, otherwise down
        CrosswordWord word = acrossAt[r, c] ?? downAt[r, c];
        if (word == null)
        {
            Debug.Log($"Cell {r},{c} is playable but not assigned to any word.");
            return;
        }

        Debug.Log($"Clicked word {word.id}: {word.answer}");

        if (word.isAcross)
            HighlightAcrossWord(r, c);
        else
            HighlightDownWord(r, c);
    }

    private void ClearHighlights()
    {
        if (cells == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] != null)
                    cells[r, c].SetHighlighted(false);
            }
        }
    }

    private void HighlightAcrossWord(int row, int col)
    {
        if (cells == null) return;

        if (row < 0 || row >= rows || col < 0 || col >= cols) return;
        if (cells[row, col].IsBlocked) return;

        // walk left
        int startCol = col;
        while (startCol - 1 >= 0 && !cells[row, startCol - 1].IsBlocked)
        {
            startCol--;
        }

        // walk right
        int endCol = col;
        while (endCol + 1 < cols && !cells[row, endCol + 1].IsBlocked)
        {
            endCol++;
        }

        // highlight all cells from startCol..endCol
        for (int c = startCol; c <= endCol; c++)
        {
            cells[row, c].SetHighlighted(true);
        }
    }

    private void HighlightDownWord(int row, int col)
    {
        if (cells == null) return;

        if (row < 0 || row >= rows || col < 0 || col >= cols) return;
        if (cells[row, col].IsBlocked) return;

        // walk up
        int startRow = row;
        while (startRow - 1 >= 0 && !cells[startRow - 1, col].IsBlocked)
        {
            startRow--;
        }

        // walk down
        int endRow = row;
        while (endRow + 1 < rows && !cells[endRow + 1, col].IsBlocked)
        {
            endRow++;
        }

        // highlight all cells from startRow..endRow
        for (int r = startRow; r <= endRow; r++)
        {
            cells[r, col].SetHighlighted(true);
        }
    }
}
