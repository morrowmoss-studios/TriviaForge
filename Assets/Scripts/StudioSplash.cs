using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StudioSplash : MonoBehaviour
{
    public float fadeDuration  = 1f;
    public float waitDuration  = 1.5f;
    public string nextSceneName = "MainMenu";

    [SerializeField] private Image img; // assign in inspector, or auto-found below

    void Start()
    {
        // Try to find Image if not assigned in inspector
        if (img == null)
            img = GetComponent<Image>();

        // Also check children
        if (img == null)
            img = GetComponentInChildren<Image>();

        if (img == null)
        {
            Debug.LogError("StudioSplash: No Image component found. Assign it in the inspector.");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        img.color = new Color(1, 1, 1, 0);
        StartCoroutine(FadeSequence());
    }

    void Update()
    {
        var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        bool tapped = (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
                      || (mouse != null && mouse.leftButton.wasPressedThisFrame);

        if (tapped)
            SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeSequence()
    {
        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            img.color = new Color(1, 1, 1, t / fadeDuration);
            yield return null;
        }
        img.color = Color.white;

        // Wait
        yield return new WaitForSeconds(waitDuration);

        // Fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            img.color = new Color(1, 1, 1, 1f - (t / fadeDuration));
            yield return null;
        }
        img.color = new Color(1, 1, 1, 0);

        SceneManager.LoadScene(nextSceneName);
    }
}