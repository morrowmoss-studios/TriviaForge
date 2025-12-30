using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite playableSprite; // light tile
    [SerializeField] private Sprite blockedSprite;  // dark tile

    [Header("Highlight")]
    [SerializeField] private Color normalColor   = Color.white;
    [SerializeField] private Color highlightColor = new Color(0f, 1f, 1f, 0.8f); // teal glow

    [Header("Letter")]
    [SerializeField] private TMP_Text letterText;

    public int row;
    public int col;

    public bool IsBlocked { get; private set; }
    private bool isHighlighted;

    private CrosswordBoardManager board;   // set in Awake

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        board = FindObjectOfType<CrosswordBoardManager>();
        UpdateVisual();
    }

    public void SetBlocked(bool blocked)
    {
        IsBlocked = blocked;
        if (letterText != null && blocked)
            letterText.text = ""; // no letter in black/wood cell

        UpdateVisual();
    }

    public void SetLetter(char c)
    {
        if (letterText != null)
            letterText.text = c == '\0' ? "" : c.ToString();
    }

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (backgroundImage == null) return;

        // sprite: light if playable, dark if blocked
        backgroundImage.sprite = IsBlocked ? blockedSprite : playableSprite;

        // tint: teal when part of selected word
        backgroundImage.color = isHighlighted ? highlightColor : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (board != null)
            board.OnCellClicked(this);
    }
}
