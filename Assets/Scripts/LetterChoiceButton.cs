using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class LetterChoiceButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI letterText;

    private char letter;

    // Drag helpers
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startAnchoredPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    // ---------- EXISTING LOGIC (UNCHANGED) ----------

    public void SetLetter(string value)
    {
        letter = value[0];
        letterText.text = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click-to-select still works exactly as before
        LetterSelectionManager.Instance.SelectLetter(letter);
    }

    public char GetLetter() => letter;

    // ---------- DRAG LOGIC (ADDITIVE) ----------

    public void OnPointerDown(PointerEventData eventData)
    {
        startAnchoredPos = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optional: visually bring to front
        rectTransform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move with cursor / finger
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap back to original position after drop
        rectTransform.anchoredPosition = startAnchoredPos;
    }
}