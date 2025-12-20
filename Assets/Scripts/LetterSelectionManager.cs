using UnityEngine;

public class LetterSelectionManager : MonoBehaviour
{
    public static LetterSelectionManager Instance;

    public char? SelectedLetter { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SelectLetter(char letter)
    {
        SelectedLetter = letter;
    }

    public void ClearSelection()
    {
        SelectedLetter = null;
    }
}