using System.Text;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class CrosswordCluesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text acrossText;
    [SerializeField] private TMP_Text downText;

    private void Start()
    {
        List<CrosswordWord> words = CrosswordSession.currentWords;

        if (words == null || words.Count == 0)
        {
            if (acrossText != null) acrossText.text = "No crossword data.";
            if (downText != null)   downText.text   = "";
            return;
        }

        var acrossBuilder = new StringBuilder();
        var downBuilder   = new StringBuilder();

        foreach (var w in words)
        {
            if (w == null) continue;
            if (string.IsNullOrEmpty(w.id)) continue;

            string line = $"{w.id}  {w.clue}";

            if (w.isAcross)
                acrossBuilder.AppendLine(line);
            else
                downBuilder.AppendLine(line);
        }

        if (acrossText != null) acrossText.text = acrossBuilder.ToString();
        if (downText != null)   downText.text   = downBuilder.ToString();
    }
}