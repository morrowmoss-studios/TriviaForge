using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image tileImage;
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