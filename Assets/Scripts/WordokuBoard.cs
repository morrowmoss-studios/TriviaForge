using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WordokuBoard : MonoBehaviour
{
    [Header("Board Settings")]
    public GameObject cellPrefab;           // Assign WordokuCellPrefab in Inspector
    public Transform gridParent;            // Assign GridParent (with GridLayoutGroup)
    public Sprite darkTileSprite;           // Assign in Inspector
    public Sprite lightTileSprite;          // Assign in Inspector

    [HideInInspector]
    public WordokuCell[,] boardCells = new WordokuCell[9, 9];
    
    [SerializeField] private RectTransform boardWrapper;  // Assign your outer AspectRatioFitter wrapper here
    [SerializeField] private GridLayoutGroup gridLayout;

    private void Start()
    {
        AdjustCellSize();
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        // Clear any old tiles
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                cellGO.transform.localScale = Vector3.one;

                WordokuCell cell = cellGO.GetComponent<WordokuCell>();
                cell.Setup(row, col);
                cell.SetLetter(GetRandomLetter());

                Image img = cellGO.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = (row + col) % 2 == 0 ? darkTileSprite : lightTileSprite;
                    img.color = Color.white;
                }

                boardCells[row, col] = cell;
            }
        }
    }
    
    private string GetRandomLetter()
    {
        string letters = "ABCDEFGHI"; // 9 unique letters for Wordoku
        int index = Random.Range(0, letters.Length);
        return letters[index].ToString();
    }
    private void AdjustCellSize()
    {
        StartCoroutine(ResizeNextFrame());
    }

    private IEnumerator ResizeNextFrame()
    {
        yield return null; // Wait one frame to ensure layout is settled

        float gridWidth = boardWrapper.rect.width;

        float spacing = gridLayout.spacing.x;
        float totalSpacing = spacing * 8f; // 9 columns = 8 gaps

        float cellSize = (gridWidth - totalSpacing) / 9f;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }
    private void OnRectTransformDimensionsChange()
    {
        AdjustCellSize();
    }
}