using TMPro;
using UnityEngine;

public class CrosswordClueDisplay : MonoBehaviour
{
    public static CrosswordClueDisplay Instance { get; private set; }

    [SerializeField] private TMP_Text clueText;
    [SerializeField] private Color activeClueColor = new Color(1f, 0.85f, 0.1f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ClearClue();
    }

    public void ShowClue(string label, string clue, int answerLength = 0)
    {
        if (clueText == null) return;
        string suffix = answerLength > 0 ? $" ({answerLength})" : "";
        string hex = ColorUtility.ToHtmlStringRGB(activeClueColor);
        clueText.text = $"<color=#{hex}><b>{label}</b> – {clue}{suffix}</color>";
    }

    // Active clue is bold and colored, inactive clue is normal weight below it
    public void ShowMultiClue(string activeLabel,   string activeClue,   int activeLength,
        string inactiveLabel, string inactiveClue, int inactiveLength)
    {
        if (clueText == null) return;
        string activeSuffix   = activeLength   > 0 ? $" ({activeLength})"   : "";
        string inactiveSuffix = inactiveLength > 0 ? $" ({inactiveLength})" : "";
        string hex = ColorUtility.ToHtmlStringRGB(activeClueColor);
        clueText.text = $"<color=#{hex}><b>{activeLabel}</b> – {activeClue}{activeSuffix}</color>\n\n{inactiveLabel} – {inactiveClue}{inactiveSuffix}";
    }

    public void ClearClue()
    {
        if (clueText == null) return;
        clueText.text = "";
    }
}