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

}