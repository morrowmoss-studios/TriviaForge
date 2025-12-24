using UnityEngine;

public class WordokuSelectionHighlighter : MonoBehaviour
{
    [SerializeField] private WordokuManager wordokuManager;

    private void Update()
    {
        if (wordokuManager == null || wordokuManager.board == null || wordokuManager.board.boardCells == null)
            return;

        char? selected = null;

        if (LetterSelectionManager.Instance != null)
        {
            selected = LetterSelectionManager.Instance.SelectedLetter;
        }

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                WordokuCell cell = wordokuManager.board.boardCells[row, col];
                if (cell == null) continue;

                string letterInCell = cell.GetLetter();
                bool shouldHighlight =
                    selected.HasValue &&
                    !string.IsNullOrEmpty(letterInCell) &&
                    letterInCell[0] == selected.Value;

                cell.SetLetterHighlight(shouldHighlight);
            }
        }
    }
}