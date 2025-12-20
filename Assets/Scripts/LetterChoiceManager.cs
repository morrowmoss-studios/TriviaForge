using UnityEngine;

public class LetterChoiceManager : MonoBehaviour
{
    [SerializeField] private WordokuManager wordokuManager;
    [SerializeField] private LetterChoiceButton[] buttons;

    private void Start()
    {
        PopulateButtons();
    }

    private void PopulateButtons()
    {
        char[] letters = wordokuManager.CurrentLetters;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetLetter(letters[i].ToString());
        }
    }
}