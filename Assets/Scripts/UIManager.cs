using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("If true, this UIManager will persist across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            // If you ever choose to have only one global UIManager
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

        SceneManager.LoadScene(sceneName);
    }

    // Reloads the current active scene
    public void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
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