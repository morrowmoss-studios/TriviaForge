using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordokuCell : MonoBehaviour,
    IPointerClickHandler,
    IDropHandler
{
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image tileBackground;
    [SerializeField] private Sprite brownTile;
    [SerializeField] private Sprite whiteTile;

    public int row, col;
    private bool locked;
    private string currentLetter = "";
    private WordokuManager manager;

    private void Awake()
    {
        if (letterText == null)
        {
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
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

    // ---------- VISUAL STATE (NEW, SAFE) ----------

    private void UpdateTileVisual()
    {
        if (tileBackground == null) return;

        // FINAL RULE:
        // Any letter (locked OR player placed) = brown
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

    // ---------- EXISTING LOGIC (UNCHANGED BEHAVIOR) ----------

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        letterText.text = letter;
        ForceLockText();
        UpdateTileVisual();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
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

    // ---------- CLICK-TO-PLACE ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked) return;

        var selected = LetterSelectionManager.Instance.SelectedLetter;
        if (selected.HasValue)
        {
            PlaceLetter(selected.Value);
            LetterSelectionManager.Instance.ClearSelection();
        }
    }

    // ---------- DRAG-AND-DROP ----------

    public void OnDrop(PointerEventData eventData)
    {
        if (isLocked) return;

        var letterButton = eventData.pointerDrag?.GetComponent<LetterChoiceButton>();
        if (letterButton == null) return;

        PlaceLetter(letterButton.GetLetter());
    }

    // ---------- SINGLE SOURCE OF TRUTH ----------

    public void PlaceLetter(char letter)
    {
        if (manager != null && !manager.IsValidPlacement(row, col, letter))
        {
            return;
        }

        currentLetter = letter.ToString();
        letterText.text = currentLetter;
        ForceLockText();
        UpdateTileVisual();
    }
}
