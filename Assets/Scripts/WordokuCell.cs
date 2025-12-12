using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WordokuCell : MonoBehaviour
{
    public TMP_Text letterText;
    public int row, col;

    private bool locked = false;
    private string currentLetter = "";

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
    }

    public void SetLetter(string letter)
    {
        currentLetter = letter.ToUpper();
        letterText.text = currentLetter;
        //letterText.rectTransform.anchoredPosition = Vector2.zero;
    }

    public string GetLetter()
    {
        return currentLetter;
    }

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;

        // Optional visual change: dim text or tint background if locked
        Color textColor = isLocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        letterText.color = textColor;
    }

    public bool isLocked
    {
        get { return locked; }
    }

    public void ClearCell()
    {
        currentLetter = "";
        letterText.text = "";
    }
}