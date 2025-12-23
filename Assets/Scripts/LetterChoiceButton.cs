using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class LetterChoiceButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI letterText;

    // This is the image we will actually resprite
    [SerializeField] private Image backgroundImage;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;    // brown tile
    [SerializeField] private Sprite selectedSprite;  // white tile

    private char letter;
    private bool isSelected;

    private void Awake()
    {
        // 1) If backgroundImage not wired, try Button.targetGraphic
        if (backgroundImage == null)
        {
            var btn = GetComponent<Button>();
            if (btn != null && btn.targetGraphic is Image targetImg)
            {
                backgroundImage = targetImg;
            }
        }

        // 2) If still null, fall back to Image on this object
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // 3) If normalSprite not set, use current sprite
        if (backgroundImage != null && normalSprite == null)
        {
            normalSprite = backgroundImage.sprite;
        }

        ApplyVisual();
    }

    // Called by LetterChoiceManager when populating from the word
    public void SetLetter(string value)
    {
        letter = value[0];

        if (letterText != null)
            letterText.text = value;

        // Reset selection whenever this button is reused
        isSelected = false;
        ApplyVisual();
    }

    public char GetLetter() => letter;

    // CLICK = select / toggle this letter
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

    // Called when this letter is fully used
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
