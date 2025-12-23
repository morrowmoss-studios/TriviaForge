using UnityEngine;

public class LetterChoiceManager : MonoBehaviour
{
    [SerializeField] private LetterChoiceButton[] buttons;

    // CALLED by WordokuManager when the word is ready
    public void PopulateFromWord(char[] letters)
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("LetterChoiceManager: No buttons assigned!");
            return;
        }

        if (letters == null || letters.Length == 0)
        {
            Debug.LogError("LetterChoiceManager: PopulateFromWord called with empty letters array.");
            return;
        }

        Debug.Log("PopulateFromWord called with: " + new string(letters));

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < letters.Length)
            {
                var btn = buttons[i];

                if (btn == null)
                {
                    Debug.LogWarning($"LetterChoiceManager: Button index {i} is null.");
                    continue;
                }

                btn.gameObject.SetActive(true);
                btn.SetLetter(letters[i].ToString());
                btn.SetSelected(false); // reset highlight
            }
            else
            {
                // Extra buttons beyond the word length get hidden
                if (buttons[i] != null)
                    buttons[i].gameObject.SetActive(false);
            }
        }
    }

    // CALLED by WordokuManager when a letter is fully used on the board
    public void MarkLetterCompleted(char letter)
    {
        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null || !btn.gameObject.activeSelf)
                continue;

            if (btn.GetLetter() == letter)
            {
                btn.SetCompleted(true);  // handles hiding + clearing selection
                return;
            }
        }
    }
}