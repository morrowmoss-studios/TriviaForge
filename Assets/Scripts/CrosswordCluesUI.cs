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
        // Audio
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnMenuScene();

        // Fix scrollviews before populating
        FixScrollView(acrossScroll);
        FixScrollView(downScroll);

        List<CrosswordWord> words = CrosswordSession.currentWords;

        if (words == null || words.Count == 0)
        {
            if (acrossText != null) acrossText.text = "No crossword data.";
            if (downText   != null) downText.text   = "";
            return;
        }

        // Split across and down
        List<CrosswordWord> across = new List<CrosswordWord>();
        List<CrosswordWord> down   = new List<CrosswordWord>();

        foreach (var w in words)
        {
            if (w == null) continue;
            if (w.isAcross) across.Add(w);
            else            down.Add(w);
        }

        // Sort by clue number
        across.Sort((a, b) => ExtractNumber(a.id).CompareTo(ExtractNumber(b.id)));
        down.Sort((a,   b) => ExtractNumber(a.id).CompareTo(ExtractNumber(b.id)));

        var acrossBuilder = new StringBuilder();
        var downBuilder   = new StringBuilder();

        foreach (var w in across)
            acrossBuilder.AppendLine($"{w.id}  {w.clue}");

        foreach (var w in down)
            downBuilder.AppendLine($"{w.id}  {w.clue}");

        if (acrossText != null)
        {
            acrossText.text = acrossBuilder.ToString();
            acrossText.ForceMeshUpdate();
        }

        if (downText != null)
        {
            downText.text = downBuilder.ToString();
            downText.ForceMeshUpdate();
        }

        // Rebuild layouts so ContentSizeFitter recalculates heights
        if (acrossScroll != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                acrossScroll.content ?? acrossScroll.GetComponent<RectTransform>());

        if (downScroll != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                downScroll.content ?? downScroll.GetComponent<RectTransform>());

        // Reset scroll to top
        if (acrossScroll != null) acrossScroll.verticalNormalizedPosition = 1f;
        if (downScroll   != null) downScroll.verticalNormalizedPosition   = 1f;
    }

    // ── Scroll fix ────────────────────────────────────────────────────────

    void FixScrollView(ScrollRect scrollRect)
    {
        if (scrollRect == null) return;

        // Configure ScrollRect
        scrollRect.vertical          = true;
        scrollRect.horizontal        = false;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.inertia           = true;
        scrollRect.decelerationRate  = 0.135f;

        // Viewport needs an Image + Mask for clipping to work
        RectTransform viewport = scrollRect.viewport;
        if (viewport != null)
        {
            Image vpImage = viewport.GetComponent<Image>();
            if (vpImage == null) vpImage = viewport.gameObject.AddComponent<Image>();
            vpImage.color = new Color(0, 0, 0, 0);

            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
        }

        // Content anchors: top-stretch, pivot top
        RectTransform content = scrollRect.content;
        if (content != null)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot     = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(content.offsetMin.x, 0f);
            content.offsetMax = new Vector2(content.offsetMax.x, 0f);

            // Ensure ContentSizeFitter exists
            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Turn off raycast on child graphics so they don't eat scroll input
            foreach (var graphic in content.GetComponentsInChildren<MaskableGraphic>())
                graphic.raycastTarget = false;
        }

        // ScrollRect itself needs a transparent Image with raycastTarget ON
        // so touch input reaches the ScrollRect component
        Image srImage = scrollRect.GetComponent<Image>();
        if (srImage == null) srImage = scrollRect.gameObject.AddComponent<Image>();
        srImage.color         = new Color(0, 0, 0, 0);
        srImage.raycastTarget = true;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private int ExtractNumber(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;

        int i = 0;
        while (i < id.Length && char.IsDigit(id[i])) i++;

        if (int.TryParse(id.Substring(0, i), out int result))
            return result;

        return 0;
    }

    // ── Button hooks ──────────────────────────────────────────────────────

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