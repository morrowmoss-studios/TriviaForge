using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the leaderboard scroll view with players ranked by total score.
///
/// Scene setup required:
/// -- Attach this script to any GameObject in the Leaderboard scene
/// -- entryPrefab: a prefab with 3 TMP_Text children in order: Rank, Username, Score
/// -- entryContainer: the Content transform inside Leaderboard_ScrollView
/// -- loadingText (optional): shown while fetching from Firestore
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("Scroll View")]
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform  entryContainer;

    [Header("State")]
    [SerializeField] private TMP_Text loadingText;

    private void Start()
    {
        _ = LoadLeaderboardAsync();
    }

    private async System.Threading.Tasks.Task LoadLeaderboardAsync()
    {
        SetLoading(true);

        var entries = await PlayerDatabaseAPI.GetLeaderboardAsync("highestScore", 50);

        SetLoading(false);
        PopulateEntries(entries);
    }

    private void PopulateEntries(List<(string username, long value)> entries)
    {
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        if (entries == null || entries.Count == 0)
        {
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "No scores yet. Be the first!";
            }
            return;
        }

        if (loadingText != null) loadingText.gameObject.SetActive(false);

        string currentUser = PlayerPrefs.GetString("TF_CurrentUser", "");

        for (int i = 0; i < entries.Count; i++)
        {
            var (username, score) = entries[i];

            var go    = Instantiate(entryPrefab, entryContainer);
            var texts = go.GetComponentsInChildren<TMP_Text>(true);

            if (texts.Length >= 3)
            {
                texts[0].text = $"#{i + 1}";
                texts[1].text = username;
                texts[2].text = score.ToString("N0");
            }

            // Highlight the current player's own row
            if (username == currentUser)
            {
                var img = go.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.2f, 0.85f, 0.45f, 0.25f);
            }
        }
    }

    private void SetLoading(bool on)
    {
        if (loadingText == null) return;
        loadingText.gameObject.SetActive(on);
        if (on) loadingText.text = "Loading leaderboard...";
    }
}