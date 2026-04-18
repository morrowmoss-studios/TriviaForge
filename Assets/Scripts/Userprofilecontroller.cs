using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserProfileController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Image    avatarImage;
    [SerializeField] private Sprite[] avatarSprites;
    [SerializeField] private Sprite   defaultSprite;

    [Header("Trivia Stats")]
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text highStreakText;
    [SerializeField] private TMP_Text leaderboardRankText;

    [Header("Crossword Stats")]
    [SerializeField] private TMP_Text perfectGameText;
    [SerializeField] private TMP_Text cwScarletLetterText;

    [Header("Wordoku Stats")]
    [SerializeField] private TMP_Text fastestWordokuText;
    [SerializeField] private TMP_Text wdScarletLetterText;

    [Header("General")]
    [SerializeField] private TMP_Text accuracyRateText;

    [Header("Navigation")]
    [SerializeField] private string avatarSceneName = "Avatars";

    private void Start()
    {
        PopulateProfile();
        _ = FetchLeaderboardRankAsync();
    }

    private void PopulateProfile()
    {
        var p = PlayerDatabaseAPI.GetCurrentPlayer();
        bool isGuest = PlayerDatabaseAPI.IsGuest;

        // Username
        if (usernameText != null)
        {
            string name = PlayerDatabaseAPI.CurrentUsername;
            usernameText.text = isGuest || string.IsNullOrEmpty(name) ? "Guest" : name;
        }

        // Avatar
        RefreshAvatar();

        if (p == null || isGuest)
        {
            SetAllStatsToDefault();
            return;
        }

        // Trivia
        if (highScoreText    != null) highScoreText.text    = p.highestScore.ToString();
        if (highStreakText    != null) highStreakText.text   = p.highestStreak.ToString();

        // Crossword
        if (perfectGameText  != null) perfectGameText.text  = p.perfectSolves.ToString();
        if (cwScarletLetterText != null)
            cwScarletLetterText.text = p.crosswordBestScarletLetters < 0
                ? "N/A" : p.crosswordBestScarletLetters.ToString();

        // Wordoku
        if (wdScarletLetterText != null)
            wdScarletLetterText.text = p.wordokuBestScarletLetters < 0
                ? "N/A" : p.wordokuBestScarletLetters.ToString();

        if (fastestWordokuText != null)
            fastestWordokuText.text = p.wordokuFastestSeconds > 0
                ? FormatTime(p.wordokuFastestSeconds) : "N/A";

        // Accuracy
        if (accuracyRateText != null)
            accuracyRateText.text = p.totalAnswers > 0
                ? $"{Mathf.RoundToInt(p.AccuracyRate * 100)}%" : "N/A";

        // Leaderboard rank placeholder until async fetch returns
        if (leaderboardRankText != null) leaderboardRankText.text = "...";
    }

    private async System.Threading.Tasks.Task FetchLeaderboardRankAsync()
    {
        if (leaderboardRankText == null) return;
        if (PlayerDatabaseAPI.IsGuest) { leaderboardRankText.text = "N/A"; return; }

        var board = await PlayerDatabaseAPI.GetLeaderboardAsync("highestScore", 500);
        string myName = PlayerDatabaseAPI.CurrentUsername;

        int rank = -1;
        for (int i = 0; i < board.Count; i++)
        {
            if (board[i].username == myName) { rank = i + 1; break; }
        }

        leaderboardRankText.text = rank > 0 ? $"#{rank}" : "Unranked";
    }

    private void RefreshAvatar()
    {
        if (avatarImage == null) return;
        int idx = PlayerDatabaseAPI.GetAvatarIndex();
        if (idx >= 0 && avatarSprites != null && idx < avatarSprites.Length)
            avatarImage.sprite = avatarSprites[idx];
        else if (defaultSprite != null)
            avatarImage.sprite = defaultSprite;
    }

    private void SetAllStatsToDefault()
    {
        TMP_Text[] all = {
            highScoreText, highStreakText, leaderboardRankText,
            perfectGameText, cwScarletLetterText,
            fastestWordokuText, wdScarletLetterText, accuracyRateText
        };
        foreach (var t in all)
            if (t != null) t.text = "N/A";
    }

    private string FormatTime(int totalSeconds)
    {
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m}:{s:00}";
    }

    // Call from the avatar image/button OnClick
    public void OnAvatarButtonPressed()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene(avatarSceneName);
    }

    // Call from Back button
    public void OnBackPressed()
    {
        FindObjectOfType<UIManager>()?.LoadPreviousScene();
    }

    // Called by AvatarSelectionController after returning to this scene
    public void OnReturnFromAvatarSelection()
    {
        RefreshAvatar();
    }
}