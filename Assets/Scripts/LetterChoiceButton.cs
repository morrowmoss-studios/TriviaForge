using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class LetterChoiceButton : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI letterText;

    private char letter;

    // Drag helpers
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    // Visual ghost we drag around
    private RectTransform dragGhost;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Find the top-level canvas so the ghost renders correctly
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            rootCanvas = canvas.rootCanvas;
        }
        else
        {
            Debug.LogError("LetterChoiceButton: No parent Canvas found. Drag ghost may be mis-positioned.");
        }
    }

    // Called by LetterChoiceManager when populating the buttons
    public void SetLetter(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            letter = value[0];
        }
        else
        {
            letter = '\0';
        }

        if (letterText != null)
            letterText.text = value;
    }

    public char GetLetter() => letter;

    // ---------- CLICK TO SELECT ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        // Same behavior as before: click = select this letter
        LetterSelectionManager.Instance.SelectLetter(letter);
    }

    // ---------- DRAG & DROP (GHOST VERSION) ----------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rootCanvas == null || rectTransform == null)
            return;

        // Let raycasts pass THROUGH the real button while dragging
        // so WordokuCell.OnDrop can receive the event.
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        // Clone this button's RectTransform as a ghost under the root canvas
        dragGhost = Instantiate(rectTransform, rootCanvas.transform);
        dragGhost.name = rectTransform.name + "_DragGhost";
        dragGhost.position = eventData.position;

        // Ghost should NOT block raycasts
        foreach (var img in dragGhost.GetComponentsInChildren<Image>())
            img.raycastTarget = false;

        foreach (var tmp in dragGhost.GetComponentsInChildren<TMP_Text>())
            tmp.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            dragGhost.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Kill the ghost
        if (dragGhost != null)
        {
            Destroy(dragGhost.gameObject);
            dragGhost = null;
        }

        // Restore raycast blocking on the real button
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    // ---------- HIDE WHEN COMPLETED ----------

    public void SetCompleted(bool completed)
    {
        gameObject.SetActive(!completed);
    }
}
