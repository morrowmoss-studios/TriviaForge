using UnityEngine;
using TMPro;

public class WordokuCell : MonoBehaviour
{
    public TMP_Text letterText;
    public int row, col;

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
    }

    public void SetLetter(char letter)
    {
        letterText.text = letter.ToString().ToUpper();
    }

    public void ClearCell()
    {
        letterText.text = "";
    }
}