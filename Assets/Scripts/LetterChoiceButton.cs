using UnityEngine;
using TMPro;

public class LetterChoiceButton : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;

    private string letter;

    public void SetLetter(string newLetter)
    {
        letter = newLetter;
        letterText.text = newLetter;
    }

    public string GetLetter()
    {
        return letter;
    }
}