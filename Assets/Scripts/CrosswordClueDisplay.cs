using UnityEngine;
using TMPro;

public class CrosswordClueDisplay : MonoBehaviour
{
    public static CrosswordClueDisplay Instance { get; private set; }

    [Header("UI")]
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

        // Example: "1A – Capital of France"
        clueText.text = $"{label} – {clue}";
    }

    public void ShowMultiClue(string acrossLabel, string acrossClue,
        string downLabel, string downClue)
    {
        if (clueText == null) return;

        // If a cell belongs to both an across and a down word
        clueText.text = $"{acrossLabel} – {acrossClue}\n{downLabel} – {downClue}";
    }

    public void ClearClue()
    {
        if (clueText != null)
            clueText.text = "";
    }
}