using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ModeSelectManager : MonoBehaviour
{
    [Header("Dropdowns")]
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown subcategoryDropdown;

    private Dictionary<string, List<string>> categoryMap = new Dictionary<string, List<string>>();

    void Start()
    {
        // Example dummy categories & subcategories
        categoryMap.Add("Science", new List<string> { "Biology", "Physics", "Space" });
        categoryMap.Add("Pop Culture", new List<string> { "Movies", "Music", "Celebrities" });
        categoryMap.Add("History", new List<string> { "Ancient", "Modern", "Military" });

        // Setup category dropdown
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(new List<string>(categoryMap.Keys));

        // Add listener for when category changes
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);

        // Initialize subcategory list
        OnCategoryChanged(0);
        
        // Set up Game Mode options
        List<string> gameModes = new List<string> { "Trivia", "Crossword", "Wordoku" };
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(gameModes);

    }

    void OnCategoryChanged(int index)
    {
        string selectedCategory = categoryDropdown.options[index].text;

        if (categoryMap.ContainsKey(selectedCategory))
        {
            subcategoryDropdown.ClearOptions();
            subcategoryDropdown.AddOptions(categoryMap[selectedCategory]);
            subcategoryDropdown.value = 0;
        }
    }

    public void OnStartPressed()
    {
        string selectedGameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        string selectedCategory = categoryDropdown.options[categoryDropdown.value].text;
        string selectedSubcategory = subcategoryDropdown.options[subcategoryDropdown.value].text;

        TriviaSessionData.selectedGameMode = selectedGameMode;
        TriviaSessionData.selectedCategory = selectedCategory;
        TriviaSessionData.selectedSubcategory = selectedSubcategory;

        Debug.Log($"Game Mode: {selectedGameMode}, Category: {selectedCategory}, Sub: {selectedSubcategory}");

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
                Debug.LogWarning("Invalid game mode selected!");
                break;
        }
    }
}
