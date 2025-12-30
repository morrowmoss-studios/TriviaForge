using UnityEngine;
using UnityEngine.UI;

public class CrosswordBoardManager : MonoBehaviour
{
    [Header("Board Setup")]
    [SerializeField] private int rows = 10;
    [SerializeField] private int columns = 10;
    [SerializeField] private Transform gridParent;          // Cross_GridParent
    [SerializeField] private CrosswordCell cellPrefab;

    private CrosswordCell[,] cells;

    // track current highlighted word so we can clear it
    private CrosswordCell[] currentWordCells = new CrosswordCell[0];

    private void Start()
    {
        BuildBoard();
        ApplyTestPattern();
    }

    private void BuildBoard()
    {
        // safety
        if (gridParent == null || cellPrefab == null)
        {
            Debug.LogError("CrosswordBoardManager: missing gridParent or cellPrefab.");
            return;
        }

        cells = new CrosswordCell[rows, columns];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                CrosswordCell cell = Instantiate(cellPrefab, gridParent);
                bool blocked = false; // default; we’ll override with pattern later
                cell.Init(this, r, c, blocked);
                cells[r, c] = cell;
            }
        }
    }

    /// <summary>
    /// TEMP: hard-coded blocked pattern so you can see the behavior.
    /// Replace this later when you hook in real crossword data.
    /// </summary>
    private void ApplyTestPattern()
    {
        if (cells == null) return;

        // simple cross shape of blocked cells
        int midRow = rows / 2;
        int midCol = columns / 2;

        for (int r = 0; r < rows; r++)
        {
            cells[r, midCol].SetBlocked(true);
        }

        for (int c = 0; c < columns; c++)
        {
            cells[midRow, c].SetBlocked(true);
        }

        // you can comment this out later, it’s just visual
        Debug.Log("Crossword test pattern applied.");
    }

    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell == null || cell.IsBlocked) return;

        // For now we default to HORIZONTAL words like the Guardian.
        HighlightWordFrom(cell, horizontal: true);
    }

    private void ClearCurrentHighlight()
    {
        foreach (var c in currentWordCells)
        {
            if (c != null)
                c.SetHighlighted(false);
        }

        currentWordCells = new CrosswordCell[0];
    }

    private void HighlightWordFrom(CrosswordCell startCell, bool horizontal)
    {
        ClearCurrentHighlight();

        int r = startCell.row;
        int c = startCell.col;

        // walk left/up
        int rStep = horizontal ? 0 : -1;
        int cStep = horizontal ? -1 : 0;

        int startR = r;
        int startC = c;

        while (IsInBounds(startR + rStep, startC + cStep) &&
               !cells[startR + rStep, startC + cStep].IsBlocked)
        {
            startR += rStep;
            startC += cStep;
        }

        // walk right/down from that start
        rStep = horizontal ? 0 : 1;
        cStep = horizontal ? 1 : 0;

        System.Collections.Generic.List<CrosswordCell> wordCells =
            new System.Collections.Generic.List<CrosswordCell>();

        int curR = startR;
        int curC = startC;

        while (IsInBounds(curR, curC) && !cells[curR, curC].IsBlocked)
        {
            wordCells.Add(cells[curR, curC]);
            curR += rStep;
            curC += cStep;
        }

        // apply highlight
        foreach (var cell in wordCells)
        {
            cell.SetHighlighted(true);
        }

        currentWordCells = wordCells.ToArray();
    }

    private bool IsInBounds(int r, int c)
    {
        return r >= 0 && r < rows && c >= 0 && c < columns;
    }
}
