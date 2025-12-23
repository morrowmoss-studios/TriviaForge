using UnityEngine;

public class LetterSelectionManager : MonoBehaviour
{
    public static LetterSelectionManager Instance { get; private set; }

    // The currently selected letter (if any)
    public char? SelectedLetter { get; private set; }

    // The currently selected button (for highlight)
    private LetterChoiceButton currentButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Called by LetterChoiceButton when clicked
    public void SelectFromButton(LetterChoiceButton button)
    {
        // 🔁 If clicking the same button again, toggle OFF
        if (currentButton == button)
        {
            ClearSelection();
            return;
        }

        // Turn off old highlight
        if (currentButton != null && currentButton != button)
        {
            currentButton.SetSelected(false);
        }

        currentButton = button;
        SelectedLetter = button.GetLetter();
        currentButton.SetSelected(true);
    }

    // Fallback if anything still calls this directly
    public void SelectLetter(char letter)
    {
        SelectedLetter = letter;
    }

    public void ClearSelection()
    {
        if (currentButton != null)
        {
            currentButton.SetSelected(false);
            currentButton = null;
        }

        SelectedLetter = null;
    }

    // Called when a button is completed/hidden
    public void ClearIfLetter(char letter)
    {
        if (SelectedLetter.HasValue && SelectedLetter.Value == letter)
        {
            ClearSelection();
        }
    }
}