using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordokuCell : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image tileBackground;   // <- will auto-grab the Button's Image
    [SerializeField] private Sprite brownTile;       // filled
    [SerializeField] private Sprite whiteTile;       // empty

    [Header("Grid Coords")]
    public int row;
    public int col;

    private bool locked;
    private string currentLetter = "";
    private WordokuManager manager;

    private void Awake()
    {
        // Find the TMP if not wired
        if (letterText == null)
        {
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Use the Button's Image as the tile background if not wired
        if (tileBackground == null)
        {
            tileBackground = GetComponent<Image>();
        }

        manager = FindObjectOfType<WordokuManager>();

        ForceLockText();
        UpdateTileVisual();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
        ForceLockText();
        UpdateTileVisual();
    }

    // ---------- VISUALS ----------

    private void UpdateTileVisual()
    {
        if (tileBackground == null) return;

        // Any letter (locked or player-placed) = brown
        // Empty = white
        if (!string.IsNullOrEmpty(currentLetter))
        {
            tileBackground.sprite = brownTile;
        }
        else
        {
            tileBackground.sprite = whiteTile;
        }
    }

    private void ForceLockText()
    {
        if (letterText == null) return;

        RectTransform textRT = letterText.rectTransform;
        RectTransform cellRT = GetComponent<RectTransform>();

        textRT.SetParent(cellRT, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.localScale = Vector3.one;
        textRT.localRotation = Quaternion.identity;

        letterText.margin = Vector4.zero;
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.enableWordWrapping = false;
        letterText.autoSizeTextContainer = false;

        letterText.ForceMeshUpdate();
    }

    // ---------- API ----------

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        if (letterText != null)
            letterText.text = letter;

        ForceLockText();
        UpdateTileVisual();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        if (letterText != null)
            letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;

        // keep tile brown/white based on letter, not lock state
        UpdateTileVisual();
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText != null)
            letterText.text = "";

        UpdateTileVisual();
    }

    // ---------- CLICK TO PLACE ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (locked) return;

        var selected = LetterSelectionManager.Instance.SelectedLetter;
        if (!selected.HasValue) return;

        PlaceLetter(selected.Value);
    }

    // ---------- CORE PLACEMENT ----------

    public void PlaceLetter(char letter)
    {
        if (manager == null)
        {
            Debug.LogError($"WordokuCell at [{row},{col}] has NO WordokuManager reference!");
            return;
        }

        Debug.Log($"Trying {letter} at [{row},{col}]");

        // optional strict mode
        if (manager.enforceSolutionWhileTesting)
        {
            char expected = manager.GetSolutionLetter(row, col);
            if (letter != expected)
            {
                Debug.Log($"Rejected {letter} at [{row},{col}] – solution expects {expected}");
                return;
            }
        }

        bool valid = manager.IsValidPlacement(row, col, letter);
        Debug.Log($"Validation result for {letter} at [{row},{col}] = {valid}");

        if (!valid)
            return;

        currentLetter = letter.ToString();
        if (letterText != null)
            letterText.text = currentLetter;

        ForceLockText();
        UpdateTileVisual();

        // if you call manager.OnCellFilled(row,col,letter) for button hiding,
        // this is where it goes
    }
}
