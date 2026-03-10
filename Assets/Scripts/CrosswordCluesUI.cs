using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CrosswordCluesUI : MonoBehaviour
{
    [Header("ScrollView Text (Content Objects)")]
    [SerializeField] private TMP_Text acrossText;
    [SerializeField] private TMP_Text downText;

    [Header("ScrollViews (optional but recommended)")]
    [SerializeField] private ScrollRect acrossScroll;
    [SerializeField] private ScrollRect downScroll;

    [Header("Scene Names")]
    [SerializeField] private string crosswordSceneName = "CrosswordMode";
    [SerializeField] private string quitPopupSceneName = "Quit_PopUp";
    [SerializeField] private string settingsSceneName  = "Settings";

    private void Start()
    {
        List<CrosswordWord> words = CrosswordSession.currentWords;

        if (words == null || words.Count == 0)
        {
            if (acrossText != null) acrossText.text = "No crossword data.";
            if (downText != null)   downText.text   = "";
            return;
        }

        // Split across and down
        List<CrosswordWord> across = new List<CrosswordWord>();
        List<CrosswordWord> down   = new List<CrosswordWord>();

        foreach (var w in words)
        {
            if (w == null) continue;

            if (w.isAcross)
                across.Add(w);
            else
                down.Add(w);
        }

        // Sort by clue number
        across.Sort((a,b) => ExtractNumber(a.id).CompareTo(ExtractNumber(b.id)));
        down.Sort((a,b)   => ExtractNumber(a.id).CompareTo(ExtractNumber(b.id)));

        var acrossBuilder = new StringBuilder();
        var downBuilder   = new StringBuilder();

        foreach (var w in across)
            acrossBuilder.AppendLine($"{w.id}  {w.clue}");

        foreach (var w in down)
            downBuilder.AppendLine($"{w.id}  {w.clue}");

        if (acrossText != null)
            acrossText.text = acrossBuilder.ToString();

        if (downText != null)
            downText.text = downBuilder.ToString();

        // Reset scroll to top
        if (acrossScroll != null)
            acrossScroll.verticalNormalizedPosition = 1f;

        if (downScroll != null)
            downScroll.verticalNormalizedPosition = 1f;
    }

    private int ExtractNumber(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;

        int i = 0;
        while (i < id.Length && char.IsDigit(id[i])) i++;

        if (int.TryParse(id.Substring(0, i), out int result))
            return result;

        return 0;
    }

    // ---------- BUTTON HOOKS ----------

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
        UIManager.SetPreviousScene();
        SceneManager.LoadScene(settingsSceneName);
    }
}