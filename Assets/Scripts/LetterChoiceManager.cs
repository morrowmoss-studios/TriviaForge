using UnityEngine;

public class LetterChoiceManager : MonoBehaviour
{
    [SerializeField] private LetterChoiceButton[] buttons;

    // CALLED by WordokuManager when the word is ready
    public void PopulateFromWord(char[] letters)
    {
        Debug.Log("PopulateFromWord called with: " + new string(letters));

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetLetter(letters[i].ToString());
        }
    }

}