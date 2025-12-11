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

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                WordokuCell cell = cellGO.GetComponent<WordokuCell>();
                cell.Setup(row, col); // Optional if you need references to coords

                // Assign tile sprite based on checkerboard pattern
                Image img = cellGO.GetComponent<Image>();

                if (img != null)
                {
                    img.sprite = (row + col) % 2 == 0 ? darkTileSprite : lightTileSprite;
                    img.color = Color.white; // Just in case prefab is tinted
                }
                else
                {
                    Debug.LogWarning($"No Image component found on prefab at {row},{col}");
                }

                boardCells[row, col] = cell;
            }
        } 
    }
}