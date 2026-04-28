using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HowToWordoku : MonoBehaviour
{
    public static bool LaunchedFromSettings = false;
    public static bool JustCompletedHowTo  = false;

    private const string HasSeenKey = "HasSeenWordokuHowTo";

    [Header("Panels in order")]
    public GameObject[] panels;

    [Header("Don't Show Again (on last panel)")]
    public Toggle dontShowAgainToggle;

    [Header("Scene Names")]
    [SerializeField] private string wordokuSceneName = "WordokuMode";

    int _index   = -1;
    bool _active = false;

    void Start()
    {
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
        if (_index >= (panels != null ? panels.Length - 1 : 0)) return;

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
        if (next >= panels.Length) FinishTutorial();
        else ShowPanel(next);
    }

    public void OnDonePressed()
    {
        _active = false;
        StartCoroutine(FinishAfterFrame());
    }

    System.Collections.IEnumerator FinishAfterFrame()
    {
        yield return null;
        FinishTutorial();
    }

    void FinishTutorial()
    {
        if (panels != null)
            foreach (var p in panels)
                if (p) p.SetActive(false);

        bool dontShow = dontShowAgainToggle != null && dontShowAgainToggle.isOn;
        if (!dontShow)
        {
            PlayerPrefs.SetInt(HasSeenKey, 0);
            PlayerPrefs.Save();
        }

        if (LaunchedFromSettings)
        {
            LaunchedFromSettings = false;
            FindObjectOfType<UIManager>()?.LoadPreviousScene();
            return;
        }

        JustCompletedHowTo = true;
        SceneManager.LoadScene(wordokuSceneName);
    }

    public static bool ShouldShowHowTo()
    {
        if (JustCompletedHowTo)
        {
            JustCompletedHowTo = false;
            return false;
        }
        return PlayerPrefs.GetInt(HasSeenKey, 0) == 0;
    }

    public static void LaunchFromSettingsMenu()
    {
        LaunchedFromSettings = true;
        UIManager.SetPreviousScene();
        SceneManager.LoadScene("HowTo_Wordoku");
    }
}