using UnityEngine;
using TMPro;

public class WordokuCell : MonoBehaviour
{
    public TMP_Text letterText;
    public int row, col;

    private bool locked = false;
    private string currentLetter = "";

    private void Awake()
    {
        if (letterText == null)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);

        ForceTMPReset();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
        ForceTMPReset();
    }

    private void ForceTMPReset()
    {
        if (letterText == null) return;

        // Hard reset TMP layout + geometry
        letterText.margin = Vector4.zero;
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.enableWordWrapping = false;
        letterText.overflowMode = TextOverflowModes.Overflow;

        var rt = letterText.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        letterText.ForceMeshUpdate();
    }

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        if (letterText != null)
        {
            letterText.text = letter;
            letterText.ForceMeshUpdate();
        }
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        if (letterText != null)
            letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText != null) letterText.text = "";
    }
}