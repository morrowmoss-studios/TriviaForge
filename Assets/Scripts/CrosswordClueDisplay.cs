using TMPro;
using UnityEngine;

public class CrosswordClueDisplay : MonoBehaviour
{
    public static CrosswordClueDisplay Instance { get; private set; }

    [SerializeField] private TMP_Text clueText;

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

    public void ShowClue(string label, string clue)
    {
        if (clueText == null) return;
        clueText.text = $"<b>{label}</b> – {clue}";
    }

    // Active clue is bold, inactive clue is normal weight below it
    public void ShowMultiClue(string activeLabel, string activeClue,
        string inactiveLabel, string inactiveClue)
    {
        if (clueText == null) return;
        clueText.text = $"<b>{activeLabel}</b> – {activeClue}\n\n{inactiveLabel} – {inactiveClue}";
    }

    public void ClearClue()
    {
        if (clueText == null) return;
        clueText.text = "";
    }
}