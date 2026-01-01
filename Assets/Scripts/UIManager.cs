using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("If true, this UIManager will persist across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    // Static field to track the last visited scene
    public static string previousSceneName;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // -------------------------------------------------------------
    //  Scene Controls
    // -------------------------------------------------------------

    // Called by buttons to go to another scene
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("UIManager.LoadScene called with empty sceneName.");
            return;
        }

        // Store the current scene before changing
        SetPreviousScene();
        SceneManager.LoadScene(sceneName);
    }

    // Reloads the current active scene
    public void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    // Set the previous scene manually (for special transitions)
    public static void SetPreviousScene()
    {
        previousSceneName = SceneManager.GetActiveScene().name;
    }

    // Go back to the last scene visited
    public void LoadPreviousScene()
    {
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            Debug.LogWarning("No previous scene stored. Loading MainMenu as fallback.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    // -------------------------------------------------------------
    //  App Controls
    // -------------------------------------------------------------

    // Hook this to a Quit/Exit button if you ever add one
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public void OpenSettingsScene()
    {
        SetPreviousScene();
        SceneManager.LoadScene("Settings");
    }

    // For external links (itch page, email, website, etc.)
    public void OpenURL(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("UIManager.OpenURL called with empty url.");
            return;
        }

        Application.OpenURL(url);
    }
}
