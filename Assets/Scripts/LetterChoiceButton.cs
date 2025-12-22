using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class LetterChoiceButton : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Main UI")]
    [SerializeField] private TextMeshProUGUI letterText;

    [Header("Drag Ghost")]
    // Assign a child RectTransform that has an Image + TMP for the ghost
    [SerializeField] private RectTransform dragGhost;

    private TextMeshProUGUI dragGhostText;

    private char letter;

    private Canvas parentCanvas;
    private bool isDragging = false;
    private Vector2 dragGhostStartPos;

    // --------------------------------------------------------------------
    //  Awake
    // --------------------------------------------------------------------
    private void Awake()
    {
        if (!letterText)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);

        parentCanvas = GetComponentInParent<Canvas>();
        if (!parentCanvas)
        {
            Debug.LogError("LetterChoiceButton: No parent Canvas found – drag math may be wrong.");
        }

        if (dragGhost != null)
        {
            dragGhostText = dragGhost.GetComponentInChildren<TextMeshProUGUI>(true);

            // Remember its "home" position and start hidden
            dragGhostStartPos = dragGhost.anchoredPosition;
            dragGhost.gameObject.SetActive(false);

            // Make sure the ghost does NOT block raycasts,
            // so the board cells can still receive OnDrop.
            var cg = dragGhost.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = dragGhost.gameObject.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;
        }
    }

    // --------------------------------------------------------------------
    //  Letter setup
    // --------------------------------------------------------------------
    public void SetLetter(string value)
    {
        letter = value[0];

        if (letterText != null)
            letterText.text = value;

        if (dragGhostText != null)
            dragGhostText.text = value;
    }

    public char GetLetter() => letter;

    // --------------------------------------------------------------------
    //  Click-to-select
    // --------------------------------------------------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        LetterSelectionManager.Instance.SelectLetter(letter);
    }

    // --------------------------------------------------------------------
    //  Drag & Drop (ghost only)
    // --------------------------------------------------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragGhost == null || parentCanvas == null) return;

        isDragging = true;

        // Show the ghost and start it from its home position
        dragGhost.gameObject.SetActive(true);
        dragGhost.anchoredPosition = dragGhostStartPos;
        dragGhost.SetAsLastSibling(); // render on top
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragGhost == null || parentCanvas == null) return;

        dragGhost.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        ResetGhost();
    }

    // --------------------------------------------------------------------
    //  Completed toggle (called from manager)
    // --------------------------------------------------------------------
    public void SetCompleted(bool completed)
    {
        gameObject.SetActive(!completed);

        if (completed)
        {
            // Hard safety: kill any leftover ghost visual
            ResetGhost();
        }
    }

    // --------------------------------------------------------------------
    //  Helpers
    // --------------------------------------------------------------------
    private void ResetGhost()
    {
        if (dragGhost == null) return;

        dragGhost.gameObject.SetActive(false);
        dragGhost.anchoredPosition = dragGhostStartPos;
    }
}
