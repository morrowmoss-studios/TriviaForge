using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ModeSelectManager : MonoBehaviour
{
    // --------- CONFIG TYPES (editable in Inspector) ---------

    [System.Serializable]
    public class SubcategoryConfig
    {
        [Tooltip("What the player sees, e.g. 'Physics'")]
        public string displayName;

        [Tooltip("ID used in JSON, e.g. 'physics_quantum'")]
        public string id;
    }

    [System.Serializable]
    public class CategoryConfig
    {
        [Tooltip("What the player sees, e.g. 'Science'")]
        public string displayName;

        [Tooltip("ID used in JSON, e.g. 'science'")]
        public string id;

        [Tooltip("Subcategories that belong to this category")]
        public List<SubcategoryConfig> subcategories = new List<SubcategoryConfig>();
    }

    // --------- UI REFERENCES ---------

    [Header("Dropdowns")]
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown subcategoryDropdown;
    public TMP_Dropdown difficultyDropdown;

    [Header("Categories & Subcategories")]
    [Tooltip("Define all your categories + subcategories + IDs here")]
    public List<CategoryConfig> categories = new List<CategoryConfig>();

    // quick lookup by display name
    private Dictionary<string, CategoryConfig> categoryByDisplayName =
        new Dictionary<string, CategoryConfig>();

    // --------- UNITY LIFECYCLE ---------

    private void Start()
    {
        BuildCategoryLookup();
        SetupCategoryDropdown();
        SetupGameModeDropdown();
        SetupDifficultyDropdown();
    }

    // --------- INITIALIZATION ---------

    private void BuildCategoryLookup()
    {
        categoryByDisplayName.Clear();

        foreach (var cat in categories)
        {
            if (cat == null || string.IsNullOrEmpty(cat.displayName))
                continue;

            if (!categoryByDisplayName.ContainsKey(cat.displayName))
            {
                categoryByDisplayName.Add(cat.displayName, cat);
            }
            else
            {
                Debug.LogWarning($"[ModeSelectManager] Duplicate category displayName '{cat.displayName}'");
            }
        }
    }

    private void SetupCategoryDropdown()
    {
        categoryDropdown.ClearOptions();

        List<string> labels = new List<string>();
        foreach (var cat in categories)
        {
            if (cat != null && !string.IsNullOrEmpty(cat.displayName))
            {
                labels.Add(cat.displayName);
            }
        }

        if (labels.Count == 0)
        {
            labels.Add("NO CATEGORIES");
            Debug.LogError("[ModeSelectManager] No categories configured in the inspector.");
        }

        categoryDropdown.AddOptions(labels);
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        // initialize subcategory dropdown with first category
        OnCategoryChanged(categoryDropdown.value);
    }

    private void SetupGameModeDropdown()
    {
        List<string> gameModes = new List<string> { "Trivia", "Crossword", "Wordoku" };
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(gameModes);
    }

    private void SetupDifficultyDropdown()
    {
        List<string> difficultyOptions = new List<string>
        {
            "Easy",
            "Medium",
            "Hard",
            "Insanity"
        };

        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(difficultyOptions);

        // default to Medium
        difficultyDropdown.value = 1;
        difficultyDropdown.RefreshShownValue();
    }

    // --------- CATEGORY / SUBCATEGORY HANDLING ---------

    private void OnCategoryChanged(int index)
    {
        if (categoryDropdown.options.Count == 0)
            return;

        string selectedCategoryLabel = categoryDropdown.options[index].text;

        if (!categoryByDisplayName.TryGetValue(selectedCategoryLabel, out var catConfig) ||
            catConfig == null)
        {
            Debug.LogWarning($"[ModeSelectManager] No CategoryConfig found for '{selectedCategoryLabel}'.");
            subcategoryDropdown.ClearOptions();
            subcategoryDropdown.AddOptions(new List<string> { "N/A" });
            subcategoryDropdown.value = 0;
            subcategoryDropdown.RefreshShownValue();
            return;
        }

        // fill subcategory dropdown
        subcategoryDropdown.ClearOptions();

        List<string> subLabels = new List<string>();
        foreach (var sub in catConfig.subcategories)
        {
            if (sub != null && !string.IsNullOrEmpty(sub.displayName))
            {
                subLabels.Add(sub.displayName);
            }
        }

        if (subLabels.Count == 0)
        {
            subLabels.Add("N/A");
            Debug.LogWarning($"[ModeSelectManager] Category '{selectedCategoryLabel}' has no subcategories configured.");
        }

        subcategoryDropdown.AddOptions(subLabels);
        subcategoryDropdown.value = 0;
        subcategoryDropdown.RefreshShownValue();
    }

    // --------- BUTTON HANDLERS ---------

    public void OnStartPressed()
    {
        // Pretty labels for UI
        string selectedGameMode  = gameModeDropdown.options[gameModeDropdown.value].text;
        string selectedCategory  = categoryDropdown.options[categoryDropdown.value].text;
        string selectedSubcat    = subcategoryDropdown.options[subcategoryDropdown.value].text;
        string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;

        // Save pretty values
        TriviaSessionData.selectedGameMode    = selectedGameMode;
        TriviaSessionData.selectedCategory    = selectedCategory;
        TriviaSessionData.selectedSubcategory = selectedSubcat;
        TriviaSessionData.selectedDifficulty  = selectedDifficulty;

        // Look up IDs for JSON
        string categoryId    = selectedCategory;   // fallback
        string subcategoryId = selectedSubcat;     // fallback

        if (categoryByDisplayName.TryGetValue(selectedCategory, out var catConfig) && catConfig != null)
        {
            if (!string.IsNullOrEmpty(catConfig.id))
                categoryId = catConfig.id;

            // find subcategory config
            foreach (var sub in catConfig.subcategories)
            {
                if (sub != null && sub.displayName == selectedSubcat)
                {
                    if (!string.IsNullOrEmpty(sub.id))
                        subcategoryId = sub.id;
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ModeSelectManager] No category config for label '{selectedCategory}', using label as id.");
        }

        // Save IDs for database access
        TriviaSessionData.selectedCategoryId    = categoryId;
        TriviaSessionData.selectedSubcategoryId = subcategoryId;

        Debug.Log($"Game Mode: {selectedGameMode}, " +
                  $"Category: {selectedCategory} ({categoryId}), " +
                  $"Sub: {selectedSubcat} ({subcategoryId}), " +
                  $"Diff: {selectedDifficulty}");

        switch (selectedGameMode)
        {
            case "Trivia":
                SceneManager.LoadScene("TriviaMode");
                break;
            case "Crossword":
                SceneManager.LoadScene("CrosswordMode");
                break;
            case "Wordoku":
                SceneManager.LoadScene("WordokuMode");
                break;
            default:
                Debug.LogWarning("[ModeSelectManager] Invalid game mode selected!");
                break;
        }
    }
}
