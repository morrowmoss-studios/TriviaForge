using UnityEngine;

public class CrosswordBoardManager : MonoBehaviour
{
    public CrosswordCell[,] cells;    // filled when you build the grid

    private enum Direction { Across, Down }

    public void OnCellClicked(CrosswordCell cell)
    {
        if (cell.IsBlocked) return;

        ClearHighlights();
        HighlightWordAt(cell.row, cell.col, Direction.Across);
    }

    private void ClearHighlights()
    {
        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                cells[r, c].SetHighlighted(false);
            }
        }
    }

    private void HighlightWordAt(int row, int col, Direction dir)
    {
        int dr = dir == Direction.Across ? 0 : 1;
        int dc = dir == Direction.Across ? 1 : 0;

        int r = row;
        int c = col;

        // walk backwards to find the start of the word
        while (InBounds(r - dr, c - dc) && !cells[r - dr, c - dc].IsBlocked)
        {
            r -= dr;
            c -= dc;
        }

        // walk forward and highlight the whole word
        while (InBounds(r, c) && !cells[r, c].IsBlocked)
        {
            cells[r, c].SetHighlighted(true);
            r += dr;
            c += dc;
        }
    }

    private bool InBounds(int r, int c)
    {
        return r >= 0 && c >= 0 &&
               r < cells.GetLength(0) &&
               c < cells.GetLength(1);
    }
}
