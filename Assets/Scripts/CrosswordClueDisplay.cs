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
        clueText.text = $"{label} – {clue}";
    }

    public void ShowMultiClue(string acrossLabel, string acrossClue,
        string downLabel, string downClue)
    {
        if (clueText == null) return;
        clueText.text = $"{acrossLabel} – {acrossClue}\n{downLabel} – {downClue}";
    }

    public void ClearClue()
    {
        if (clueText == null) return;
        clueText.text = "";
    }
}