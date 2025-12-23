using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class LetterChoiceButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite normalSprite;    // brown tile
    [SerializeField] private Sprite selectedSprite;  // white tile

    private char letter;
    private bool isSelected;

    private void Awake()
    {
        // Grab background image if not wired
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // If normal sprite not set, use whatever the Image currently has
        if (backgroundImage != null && normalSprite == null)
            normalSprite = backgroundImage.sprite;

        ApplyVisual();
    }

    // Called by LetterChoiceManager when populating from the word
    public void SetLetter(string value)
    {
        letter = value[0];

        if (letterText != null)
            letterText.text = value;

        // Reset selection state when reused
        isSelected = false;
        ApplyVisual();
    }

    public char GetLetter() => letter;

    // CLICK = select this letter
    public void OnPointerClick(PointerEventData eventData)
    {
        if (LetterSelectionManager.Instance != null)
        {
            LetterSelectionManager.Instance.SelectFromButton(this);
        }
        else
        {
            Debug.LogError("LetterChoiceButton: No LetterSelectionManager.Instance in scene.");
        }
    }

    // Called by LetterSelectionManager
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (!backgroundImage) return;

        if (isSelected && selectedSprite != null)
        {
            backgroundImage.sprite = selectedSprite;
        }
        else if (!isSelected && normalSprite != null)
        {
            backgroundImage.sprite = normalSprite;
        }
    }

    // Called by LetterChoiceManager / WordokuManager when this letter is fully used
    public void SetCompleted(bool completed)
    {
        if (!completed) return;

        if (LetterSelectionManager.Instance != null)
        {
            LetterSelectionManager.Instance.ClearIfLetter(letter);
        }

        gameObject.SetActive(false);
    }
}
