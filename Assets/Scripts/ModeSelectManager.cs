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
        public string displayName;   // e.g. "Biology"
        public string id;            // e.g. "biology"
    }

    [System.Serializable]
    public class CategoryConfig
    {
        public string displayName;                 // e.g. "Science"
        public string id;                          // e.g. "science" or "mixed_all"
        public List<SubcategoryConfig> subcategories =
            new List<SubcategoryConfig>();        // 👈 keep this name
    }

    [Header("Categories & Subcategories")]
    public List<CategoryConfig> categories = new List<CategoryConfig>();   // 👈 keep this name & public

    private CategoryConfig _currentCategory;

    private const string MixedAllCategoryId = "mixed_all";

    private void Start()
    {
        SetupGameModeDropdown();
        SetupDifficultyDropdown();
        SetupCategoryDropdown();

        // Listen for mode changes (so Wordoku can disable subcategory like Mixed All)
        gameModeDropdown.onValueChanged.RemoveListener(OnGameModeChanged);
        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);

        // Ensure UI matches current selection on boot
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
            "Mixed" // special difficulty = all difficulties together
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

        // Prevent stacking listeners
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

    // -----------------------------
    // NEW: single source of truth
    // -----------------------------

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

        // Wordoku, Crossword, OR MixedAll => disable subcategory and show "All"
        if (isWordoku || isCrossword || isMixedAll)
        {
            subcategoryDropdown.AddOptions(new List<string> { "All" });
            subcategoryDropdown.value = 0;
            subcategoryDropdown.interactable = false;
            subcategoryDropdown.RefreshShownValue();
            return;
        }

        // Normal category => enable and populate subs
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

    // -----------------------------
    // START BUTTON
    // -----------------------------

    public void OnStartPressed()
    {
        string selectedGameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;

        string categoryDisplay = _currentCategory != null ? _currentCategory.displayName : "Unknown";
        string categoryId = _currentCategory != null ? _currentCategory.id : "";

        bool isMixedAll = string.Equals(
            categoryId,
            MixedAllCategoryId,
            System.StringComparison.OrdinalIgnoreCase);

        bool isWordoku = string.Equals(
            selectedGameMode,
            "Wordoku",
            System.StringComparison.OrdinalIgnoreCase);

        bool isCrossword = string.Equals(
            selectedGameMode,
            "Crossword",
            System.StringComparison.OrdinalIgnoreCase);

        string subDisplay;
        string subId;

        // Wordoku and Crossword always use "All" (subcategory not used)
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

            var sub = _currentCategory.subcategories[subIndex];
            subDisplay = sub != null ? sub.displayName : "None";
            subId = sub != null ? sub.id : "";
        }
        else
        {
            // Mixed-all (or no subs) => conceptually use "All"
            subDisplay = "All";
            subId = "";
        }

        // Store in session data
        TriviaSessionData.selectedGameMode = selectedGameMode;
        TriviaSessionData.selectedCategory = categoryDisplay;
        TriviaSessionData.selectedCategoryId = categoryId;
        TriviaSessionData.selectedSubcategory = subDisplay;
        TriviaSessionData.selectedSubcategoryId = subId;
        TriviaSessionData.selectedDifficulty = selectedDifficulty;

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