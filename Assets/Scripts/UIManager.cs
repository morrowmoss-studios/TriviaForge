using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("If true, this UIManager will persist across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    // Navigation stack — push before leaving, pop when going back
    private static readonly Stack<string> sceneHistory = new Stack<string>();

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

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("UIManager.LoadScene called with empty sceneName.");
            return;
        }

        SetPreviousScene();
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    /// <summary>
    /// Push the current scene onto the history stack before navigating away.
    /// Call this anywhere you navigate TO Settings, About, HowToPlay, etc.
    /// </summary>
    public static void SetPreviousScene()
    {
        sceneHistory.Push(SceneManager.GetActiveScene().name);
        Debug.Log($"[UIManager] Pushed '{sceneHistory.Peek()}' onto history stack. Depth: {sceneHistory.Count}");
    }

    /// <summary>
    /// Pop the stack and return to wherever the user actually came from.
    /// </summary>
    public void LoadPreviousScene()
    {
        if (sceneHistory.Count > 0)
        {
            string target = sceneHistory.Pop();
            Debug.Log($"[UIManager] Popping back to '{target}'. Remaining depth: {sceneHistory.Count}");
            SceneManager.LoadScene(target);
        }
        else
        {
            Debug.LogWarning("[UIManager] History stack is empty. Loading MainMenu as fallback.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Clears the navigation history — call this when returning to MainMenu
    /// so stale history doesn't bleed into a new session.
    /// </summary>
    public static void ClearHistory()
    {
        sceneHistory.Clear();
        Debug.Log("[UIManager] Navigation history cleared.");
    }

    // -------------------------------------------------------------
    //  App Controls
    // -------------------------------------------------------------

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