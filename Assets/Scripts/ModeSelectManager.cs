using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ModeSelectManager : MonoBehaviour
{
    [Header("Dropdowns")]
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown subcategoryDropdown;

    public void OnStartPressed()
    {
        string selectedGameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        string selectedCategory = categoryDropdown.options[categoryDropdown.value].text;
        string selectedSubcategory = subcategoryDropdown.options[subcategoryDropdown.value].text;

        // Save to session data (you can expand this later)
        TriviaSessionData.selectedGameMode = selectedGameMode;
        TriviaSessionData.selectedCategory = selectedCategory;
        TriviaSessionData.selectedSubcategory = selectedSubcategory;

        Debug.Log($"Game Mode: {selectedGameMode}, Category: {selectedCategory}, Sub: {selectedSubcategory}");

        // Load scene based on game mode
        switch (selectedGameMode)
        {
            case "Trivia":
                SceneManager.LoadScene("TriviaMode");
                break;
            case "Crossword":
                SceneManager.LoadScene("CrosswordMode");  // you'll build this later
                break;
            case "Wordoku":
                SceneManager.LoadScene("WordokuMode");    // you'll build this later
                break;
            default:
                Debug.LogWarning("Invalid game mode selected!");
                break;
        }
    }
}
