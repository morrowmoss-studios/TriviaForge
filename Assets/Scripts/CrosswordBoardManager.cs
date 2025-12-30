using UnityEngine;

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
    }

    // Called by CrosswordCell.OnPointerClick(this)
    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        int row = cell.row;
        int col = cell.col;

        ClearHighlights();
        HighlightAcrossWord(row, col);
        // later: we can also add HighlightDownWord(row, col) and let you toggle
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
}
