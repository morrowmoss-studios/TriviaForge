using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HowToWordoku : MonoBehaviour
{
    private const string HasSeenKey = "HasSeenWordokuHowTo";

    // Set to true when launched from HowToPlay scene so we go back instead of forward
    public static bool LaunchedFromSettings = false;

    [Header("Scene Names")]
    [SerializeField] private string wordokuSceneName = "WordokuMode";

    [Header("Panels in order")]
    [SerializeField] private GameObject[] panels;

    [Header("Don't Show Again")]
    [SerializeField] private Toggle dontShowAgainToggle;

    int _index  = -1;
    bool _active = false;

    void Start()
    {
        // Mark seen immediately to prevent WordokuMode redirect loop
        PlayerPrefs.SetInt(HasSeenKey, 1);
        PlayerPrefs.Save();

        if (panels != null)
            foreach (var p in panels)
                if (p) p.SetActive(false);

        ShowPanel(0);
        _active = true;
    }

    void Update()
    {
        if (!_active) return;

        bool tapped = false;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                tapped = true;
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                tapped = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!tapped)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                tapped = true;
            else if (Input.GetMouseButtonDown(0))
                tapped = true;
        }
#endif

        if (!tapped) return;

        Advance();
    }

    void ShowPanel(int index)
    {
        _index = Mathf.Clamp(index, 0, (panels?.Length ?? 1) - 1);
        if (panels == null) return;
        for (int i = 0; i < panels.Length; i++)
            if (panels[i]) panels[i].SetActive(i == _index);
    }

    void Advance()
    {
        if (panels == null || panels.Length == 0) return;

        int next = _index + 1;
        if (next >= panels.Length)
        {
            FinishTutorial();
        }
        else
        {
            ShowPanel(next);
        }
    }

    // Wire this to the Done button on the last panel
    public void OnDonePressed()
    {
        FinishTutorial();
    }

    void FinishTutorial()
    {
        _active = false;

        if (panels != null)
            foreach (var p in panels)
                if (p) p.SetActive(false);

        bool dontShow = dontShowAgainToggle != null && dontShowAgainToggle.isOn;
        if (!dontShow)
        {
            // Reset so instructions show again next Wordoku load
            PlayerPrefs.SetInt(HasSeenKey, 0);
            PlayerPrefs.Save();
        }

        if (LaunchedFromSettings)
        {
            LaunchedFromSettings = false;
            SceneManager.LoadScene("HowToPlay");
            return;
        }

        SceneManager.LoadScene(wordokuSceneName);
    }

    // Call this on WordokuMode Start to redirect to how-to if first time
    public static bool ShouldShowHowTo()
    {
        return PlayerPrefs.GetInt(HasSeenKey, 0) == 0;
    }

    // Call this from the HowToPlay scene Wordoku button
    public static void LaunchFromSettingsMenu()
    {
        LaunchedFromSettings = true;
        SceneManager.LoadScene("HowTo_Wordoku");
    }
}