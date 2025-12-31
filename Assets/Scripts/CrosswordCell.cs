using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image tileImage;      // we'll auto-grab this
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
        // 👉 NEW: auto-grab the Image on this object if none assigned
        if (tileImage == null)
            tileImage = GetComponent<Image>();

        // Auto–find the TMP if we forgot to assign it
        if (letterText == null)
            letterText = GetComponentInChildren<TMP_Text>(true);

        if (letterText != null)
        {
            var rt = letterText.rectTransform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            letterText.alignment = TextAlignmentOptions.Center;
            letterText.text = "";
            letterText.gameObject.SetActive(true);
        }
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
