using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image tileImage;
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Sprite playableSprite;   // light/beige tile
    [SerializeField] private Sprite blockedSprite;    // brown tile

    [Header("Highlight")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan; // tweak in Inspector

    [Header("Grid Coords (read-only at runtime)")]
    public int row;
    public int col;

    private bool isBlocked;
    private bool isHighlighted;

    private CrosswordBoardManager manager;

    // Called right after Instantiate by the board manager
    public void Init(CrosswordBoardManager mgr, int r, int c, bool blocked)
    {
        manager = mgr;
        row = r;
        col = c;
        SetBlocked(blocked);
        SetHighlighted(false);
    }
    
    private void Awake()
    {
        // Auto–find the TMP if we forgot to assign it
        if (letterText == null)
            letterText = GetComponentInChildren<TMP_Text>(true);

        if (letterText != null)
        {
            // Make sure the text is properly inside the tile
            var rt = letterText.rectTransform;
            rt.SetParent(transform, false);          // ensure it’s a child of this cell
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            letterText.alignment = TextAlignmentOptions.Center;
            letterText.text = "";    // clear the default "A"
            letterText.gameObject.SetActive(true);
        }

        // if you already had stuff in Awake, keep it here too
    }


    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;

        if (tileImage == null) return;

        tileImage.sprite = blocked ? blockedSprite : playableSprite;
        tileImage.color = normalColor;
    }

    public bool IsBlocked => isBlocked;

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;

        if (tileImage == null) return;

        // keep the same sprite, just tint for highlight
        tileImage.color = highlighted ? highlightColor : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;

        if (manager != null)
        {
            manager.OnCellClicked(this);
        }
    }
}