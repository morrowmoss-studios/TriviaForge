using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the Leaderboard scene.
///
/// Scene setup required:
/// -- 4 tab buttons wired to tabHighScore, tabStreak, tabGames, tabPerfect
/// -- An entryPrefab with 3 TMP_Text children in order: Rank, Username, Value
/// -- A ScrollRect with entryContainer as its Content transform
/// -- Optional: personal stats TMP_Text labels for the current player panel
/// -- Optional: loadingPanel GameObject shown during fetch
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button tabHighScore;
    [SerializeField] private Button tabStreak;
    [SerializeField] private Button tabGames;
    [SerializeField] private Button tabPerfect;

    [Header("Entry List")]
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform  entryContainer;

    [Header("Personal Stats Panel (optional)")]
    [SerializeField] private TMP_Text statsTotalScore;
    [SerializeField] private TMP_Text statsHighScore;
    [SerializeField] private TMP_Text statsStreak;
    [SerializeField] private TMP_Text statsGames;
    [SerializeField] private TMP_Text statsPerfect;
    [SerializeField] private TMP_Text statsAccuracy;
    [SerializeField] private TMP_Text statsUsername;

    [Header("State")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text   emptyText;

    // ── Tab config ─────────────────────────────────────────────────────────

    private readonly struct TabConfig
    {
        public readonly string Field;
        public readonly string Label;
        public TabConfig(string field, string label) { Field = field; Label = label; }
    }

    private readonly TabConfig[] Tabs = new[]
    {
        new TabConfig("totalScore",    "High Score"),
        new TabConfig("highestStreak", "Best Streak"),
        new TabConfig("gamesCompleted","Games Completed"),
        new TabConfig("perfectSolves", "Perfect Solves")
    };

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        tabHighScore?.onClick.AddListener(() => LoadBoard(0));
        tabStreak   ?.onClick.AddListener(() => LoadBoard(1));
        tabGames    ?.onClick.AddListener(() => LoadBoard(2));
        tabPerfect  ?.onClick.AddListener(() => LoadBoard(3));

        LoadPersonalStats();
        LoadBoard(0);           // default to High Score tab
    }

    // ── Board loading ──────────────────────────────────────────────────────

    private async void LoadBoard(int tabIndex)
    {
        var tab = Tabs[tabIndex];
        SetLoading(true);

        List<(string username, long value)> entries =
            await PlayerDatabaseAPI.GetLeaderboardAsync(tab.Field, 25);

        SetLoading(false);
        PopulateEntries(entries, tab.Label);
    }

    private void PopulateEntries(List<(string username, long value)> entries, string label)
    {
        // Clear existing rows
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        if (entries == null || entries.Count == 0)
        {
            if (emptyText != null) emptyText.text = "No scores yet. Be the first!";
            if (emptyText != null) emptyText.gameObject.SetActive(true);
            return;
        }

        if (emptyText != null) emptyText.gameObject.SetActive(false);

        string currentUser = PlayerPrefs.GetString("TF_CurrentUser", "");

        for (int i = 0; i < entries.Count; i++)
        {
            var (username, value) = entries[i];

            var go    = Instantiate(entryPrefab, entryContainer);
            var texts = go.GetComponentsInChildren<TMP_Text>(true);

            // Expects prefab to have at least 3 TMP_Text children: Rank, Username, Value
            if (texts.Length >= 3)
            {
                texts[0].text = $"#{i + 1}";
                texts[1].text = username;
                texts[2].text = FormatValue(label, value);
            }

            // Highlight the current player's row
            if (username == currentUser)
            {
                var img = go.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.2f, 0.85f, 0.45f, 0.25f);
            }
        }
    }

    // ── Personal stats ─────────────────────────────────────────────────────

    private void LoadPersonalStats()
    {
        var player = PlayerDatabaseAPI.GetCurrentPlayer();

        if (statsUsername != null)
            statsUsername.text = PlayerDatabaseAPI.IsGuest ? "Guest" : PlayerDatabaseAPI.CurrentUsername;

        if (player == null) return;

        if (statsTotalScore != null) statsTotalScore.text = player.totalScore.ToString("N0");
        if (statsHighScore  != null) statsHighScore.text  = player.highestScore.ToString("N0");
        if (statsStreak     != null) statsStreak.text     = player.highestStreak.ToString();
        if (statsGames      != null) statsGames.text      = player.gamesCompleted.ToString();
        if (statsPerfect    != null) statsPerfect.text    = player.perfectSolves.ToString();

        if (statsAccuracy != null)
        {
            statsAccuracy.text = player.totalAnswers > 0
                ? $"{player.AccuracyRate * 100f:F1}%"
                : "--";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private string FormatValue(string label, long value)
    {
        return label switch
        {
            "Best Streak"      => $"x{value}",
            "Games Completed"  => value.ToString(),
            "Perfect Solves"   => value.ToString(),
            _                  => value.ToString("N0")
        };
    }

    private void SetLoading(bool on)
    {
        if (loadingPanel != null) loadingPanel.SetActive(on);
    }
}