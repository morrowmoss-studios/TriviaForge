using UnityEngine;
using TMPro;

public class WordokuCell : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;

    public int row, col;
    private bool locked;
    private string currentLetter = "";

    private void Awake()
    {
        // Absolute last-resort safety: find TMP only inside THIS cell
        if (letterText == null)
        {
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        ForceLockText();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
        ForceLockText();
    }

    private void ForceLockText()
    {
        if (letterText == null) return;

        RectTransform textRT = letterText.rectTransform;
        RectTransform cellRT = GetComponent<RectTransform>();

        // Make the text a direct child of THIS cell
        textRT.SetParent(cellRT, false);

        // Hard-lock layout
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.localScale = Vector3.one;
        textRT.localRotation = Quaternion.identity;

        // TMP sanity reset
        letterText.margin = Vector4.zero;
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.enableWordWrapping = false;
        letterText.autoSizeTextContainer = false;

        letterText.ForceMeshUpdate();
    }

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        letterText.text = letter;
        ForceLockText();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText != null)
            letterText.text = "";
    }
}
