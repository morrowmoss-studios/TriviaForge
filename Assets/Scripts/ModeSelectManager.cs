using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ModeSelectManager : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_Dropdown subcategoryDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [System.Serializable]
    public class SubcategoryConfig
    {
        public string displayName;
        public string id;
    }

    [System.Serializable]
    public class CategoryConfig
    {
        public string displayName;
        public string id;
        public List<SubcategoryConfig> subcategories =
            new List<SubcategoryConfig>();
    }

    [Header("Categories & Subcategories")]
    public List<CategoryConfig> categories = new List<CategoryConfig>();

    private CategoryConfig _currentCategory;

    private const string MixedAllCategoryId = "mixed_all";

    private void Start()
    {
        SetupGameModeDropdown();
        SetupDifficultyDropdown();
        SetupCategoryDropdown();

        gameModeDropdown.onValueChanged.RemoveListener(OnGameModeChanged);
        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);

        RefreshSubcategoryState();
    }

    private void SetupGameModeDropdown()
    {
        var gameModes = new List<string> { "Trivia", "Crossword", "Wordoku" };
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(gameModes);
        gameModeDropdown.value = 0;
        gameModeDropdown.RefreshShownValue();
    }

    private void SetupDifficultyDropdown()
    {
        var difficultyOptions = new List<string>
        {
            "Easy",
            "Medium",
            "Hard",
            "Insanity",
            "Mixed"
        };

        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(difficultyOptions);
        difficultyDropdown.value = 0;
        difficultyDropdown.RefreshShownValue();
    }

    private void SetupCategoryDropdown()
    {
        categoryDropdown.ClearOptions();

        List<string> names = new List<string>();
        foreach (var cat in categories)
        {
            if (cat != null)
                names.Add(cat.displayName);
        }

        categoryDropdown.AddOptions(names);

        categoryDropdown.onValueChanged.RemoveListener(OnCategoryChanged);
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        if (categories.Count > 0)
        {
            categoryDropdown.value = 0;
            categoryDropdown.RefreshShownValue();

            _currentCategory = categories[0];
            RefreshSubcategoryState();
        }
    }

    private void OnGameModeChanged(int index)
    {
        RefreshSubcategoryState();
    }

    private void OnCategoryChanged(int index)
    {
        if (index < 0 || index >= categories.Count) return;

        _currentCategory = categories[index];
        if (_currentCategory == null) return;

        RefreshSubcategoryState();
    }

    private void RefreshSubcategoryState()
    {
        if (_currentCategory == null) return;

        string selectedGameMode = gameModeDropdown.options[gameModeDropdown.value].text;

        bool isWordoku = string.Equals(
            selectedGameMode,
            "Wordoku",
            System.StringComparison.OrdinalIgnoreCase);

        bool isCrossword = string.Equals(
            selectedGameMode,
            "Crossword",
            System.StringComparison.OrdinalIgnoreCase);

        bool isMixedAll = string.Equals(
            _currentCategory.id,
            MixedAllCategoryId,
            System.StringComparison.OrdinalIgnoreCase);

        subcategoryDropdown.ClearOptions();

        if (isWordoku || isCrossword || isMixedAll)
        {
            subcategoryDropdown.AddOptions(new List<string> { "All" });
            subcategoryDropdown.value = 0;
            subcategoryDropdown.interactable = false;
            subcategoryDropdown.RefreshShownValue();
            return;
        }

        subcategoryDropdown.interactable = true;

        List<string> subNames = new List<string>();
        if (_currentCategory.subcategories != null)
        {
            foreach (var sub in _currentCategory.subcategories)
            {
                if (sub != null && !string.IsNullOrEmpty(sub.displayName))
                    subNames.Add(sub.displayName);
            }
        }

        if (subNames.Count == 0)
            subNames.Add("None");

        subcategoryDropdown.AddOptions(subNames);
        subcategoryDropdown.value = 0;
        subcategoryDropdown.RefreshShownValue();
    }

    public void OnLogoutPressed()
    {
        PlayerPrefs.SetInt("TF_RememberMe", 0);
        PlayerPrefs.Save();
        PlayerDatabaseAPI.SignOut();
        SceneManager.LoadScene("Login_PopUp");
    }

    public void OnSettingsPressed()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene("Settings");
    }

    public void OnStartPressed()
    {
        string selectedGameMode   = gameModeDropdown.options[gameModeDropdown.value].text;
        string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;

        string categoryDisplay = _currentCategory != null ? _currentCategory.displayName : "Unknown";
        string categoryId      = _currentCategory != null ? _currentCategory.id          : "";

        bool isMixedAll = string.Equals(categoryId, MixedAllCategoryId, System.StringComparison.OrdinalIgnoreCase);
        bool isWordoku  = string.Equals(selectedGameMode, "Wordoku",    System.StringComparison.OrdinalIgnoreCase);
        bool isCrossword = string.Equals(selectedGameMode, "Crossword", System.StringComparison.OrdinalIgnoreCase);

        string subDisplay;
        string subId;

        if (isWordoku || isCrossword)
        {
            subDisplay = "All";
            subId = "";
        }
        else if (!isMixedAll &&
                 _currentCategory != null &&
                 _currentCategory.subcategories != null &&
                 _currentCategory.subcategories.Count > 0 &&
                 subcategoryDropdown.interactable)
        {
            int subIndex = Mathf.Clamp(
                subcategoryDropdown.value,
                0,
                _currentCategory.subcategories.Count - 1);

            var sub    = _currentCategory.subcategories[subIndex];
            subDisplay = sub != null ? sub.displayName : "None";
            subId      = sub != null ? sub.id          : "";
        }
        else
        {
            subDisplay = "All";
            subId      = "";
        }

        // Always clear the trivia session when starting any new game
        TriviaSessionData.ClearSession();

        TriviaSessionData.selectedGameMode      = selectedGameMode;
        TriviaSessionData.selectedCategory      = categoryDisplay;
        TriviaSessionData.selectedCategoryId    = categoryId;
        TriviaSessionData.selectedSubcategory   = subDisplay;
        TriviaSessionData.selectedSubcategoryId = subId;
        TriviaSessionData.selectedDifficulty    = selectedDifficulty;

        if (selectedGameMode == "Trivia")
        {
            TriviaSessionData.strikes = 0;
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.ResetScore();
        }

        Debug.Log($"[ModeSelect] Game Mode: {selectedGameMode}, " +
                  $"Category: {categoryDisplay} ({categoryId}), " +
                  $"Sub: {subDisplay} ({subId}), " +
                  $"Diff: {selectedDifficulty}");

        switch (selectedGameMode)
        {
            case "Trivia":
                TriviaSessionData.ClearSession();
                TriviaSessionData.strikes = 0;

                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.ResetScore();

                SceneManager.LoadScene("TriviaMode");
                break;
            case "Crossword":
                CrosswordSession.ClearPuzzle();
                SceneManager.LoadScene("CrosswordMode");
                break;
            case "Wordoku":
                SceneManager.LoadScene("WordokuMode");
                break;
            default:
                Debug.LogWarning("[ModeSelect] Invalid game mode selected!");
                break;
        }
    }
}