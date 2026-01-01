using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;   // <-- NEW

public class CrosswordCluesUI : MonoBehaviour
{
    [Header("Clue Text")]
    [SerializeField] private TMP_Text acrossText;
    [SerializeField] private TMP_Text downText;

    [Header("Scene Names")]          // <-- NEW
    [SerializeField] private string crosswordSceneName = "CrosswordMode";
    [SerializeField] private string quitPopupSceneName = "Quit_PopUp";
    [SerializeField] private string settingsSceneName  = "SettingsScene";

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

    // ---------- BUTTON HOOKS (NEW) ----------

    public void OnBackPressed()
    {
        SceneManager.LoadScene(crosswordSceneName);
    }

    public void OnQuitPressed()
    {
        SceneManager.LoadScene(quitPopupSceneName);
    }

    public void OnSettingsPressed()
    {
        SceneManager.LoadScene(settingsSceneName);
    }
}