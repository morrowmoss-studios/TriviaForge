using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WordokuBoard : MonoBehaviour
{
    [Header("Board Settings")]
    public GameObject cellPrefab;
    public RectTransform gridParent;
    public RectTransform frameInner;   // ← THIS is the key
    public GridLayoutGroup gridLayout;

    public Sprite darkTileSprite;
    public Sprite lightTileSprite;

    [HideInInspector]
    public WordokuCell[,] boardCells = new WordokuCell[9, 9];

    private void Start()
    {
        GenerateBoard();
        StartCoroutine(ResizeToFrame());
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

    private IEnumerator ResizeToFrame()
    {
        yield return null; // wait for layout + canvas

        float width = frameInner.rect.width;
        float spacing = gridLayout.spacing.x;

        float cellSize = (width - spacing * 8f) / 9f;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }
}