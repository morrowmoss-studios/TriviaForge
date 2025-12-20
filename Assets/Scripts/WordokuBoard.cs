using UnityEngine;
using UnityEngine.UI;

public class WordokuBoard : MonoBehaviour
{
    [Header("Board Settings")]
    public GameObject cellPrefab;
    public RectTransform gridParent;
    public GridLayoutGroup gridLayout;

    public Sprite darkTileSprite;
    public Sprite lightTileSprite;

    [HideInInspector]
    public WordokuCell[,] boardCells = new WordokuCell[9, 9];

    private void Start()
    {
        GenerateBoard();
        ResizeGridToParent();
    }

    public void GenerateBoard()
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                cellGO.transform.localScale = Vector3.one;

                WordokuCell cell = cellGO.GetComponent<WordokuCell>();
                cell.Setup(row, col);

                Image img = cellGO.GetComponent<Image>();
                img.sprite = (row + col) % 2 == 0 ? darkTileSprite : lightTileSprite;

                boardCells[row, col] = cell;
            }
        }
    }

    private void ResizeGridToParent()
    {
        if (gridParent == null || gridLayout == null)
            return;

        float width = gridParent.rect.width;
        float spacing = gridLayout.spacing.x;

        float cellSize = (width - spacing * 8f) / 9f * 0.90f;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }
}