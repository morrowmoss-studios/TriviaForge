using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Toggles")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle vibrationToggle;

    // PlayerPrefs keys
    private const string MusicKey = "TF_MusicOn";
    private const string SfxKey   = "TF_SfxOn";
    private const string VibKey   = "TF_VibrationOn";

    // current values in this menu session
    private bool musicOn;
    private bool sfxOn;
    private bool vibrationOn;

    // original values before any changes (for Cancel)
    private bool originalMusicOn;
    private bool originalSfxOn;
    private bool originalVibrationOn;

    private void Awake()
    {
        // Load saved values (default all ON)
        musicOn     = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        sfxOn       = PlayerPrefs.GetInt(SfxKey,   1) == 1;
        vibrationOn = PlayerPrefs.GetInt(VibKey,   1) == 1;

        originalMusicOn     = musicOn;
        originalSfxOn       = sfxOn;
        originalVibrationOn = vibrationOn;

        // Sync toggles with loaded state
        if (musicToggle     != null) musicToggle.isOn     = musicOn;
        if (sfxToggle       != null) sfxToggle.isOn       = sfxOn;
        if (vibrationToggle != null) vibrationToggle.isOn = vibrationOn;

        // Apply to AudioManager so it matches what we loaded
        ApplyMusic(musicOn);
        ApplySfx(sfxOn);
        ApplyVibration(vibrationOn);

        // Wire listeners
        if (musicToggle     != null) musicToggle.onValueChanged.AddListener(OnMusicToggled);
        if (sfxToggle       != null) sfxToggle.onValueChanged.AddListener(OnSfxToggled);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
    }

    private void OnDestroy()
    {
        if (musicToggle     != null) musicToggle.onValueChanged.RemoveListener(OnMusicToggled);
        if (sfxToggle       != null) sfxToggle.onValueChanged.RemoveListener(OnSfxToggled);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggled);
    }

    // -------- toggle callbacks --------

    private void OnMusicToggled(bool value)
    {
        musicOn = value;
        ApplyMusic(musicOn);
    }

    private void OnSfxToggled(bool value)
    {
        sfxOn = value;
        ApplySfx(sfxOn);
    }

    private void OnVibrationToggled(bool value)
    {
        vibrationOn = value;
        ApplyVibration(vibrationOn);
    }

    // -------- apply helpers --------

    private void ApplyMusic(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicEnabled(enabled);
    }

    private void ApplySfx(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXEnabled(enabled);
    }

    private void ApplyVibration(bool enabled)
    {
        PlayerPrefs.SetInt("TF_VibrationOn", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // -------- called by buttons --------

    // Hook this to CONFIRM
    public void SaveSettings()
    {
        PlayerPrefs.SetInt(MusicKey, musicOn     ? 1 : 0);
        PlayerPrefs.SetInt(SfxKey,   sfxOn       ? 1 : 0);
        PlayerPrefs.SetInt(VibKey,   vibrationOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Hook this to CANCEL
    public void RevertSettings()
    {
        musicOn     = originalMusicOn;
        sfxOn       = originalSfxOn;
        vibrationOn = originalVibrationOn;

        ApplyMusic(musicOn);
        ApplySfx(sfxOn);
        ApplyVibration(vibrationOn);

        if (musicToggle     != null) musicToggle.isOn     = musicOn;
        if (sfxToggle       != null) sfxToggle.isOn       = sfxOn;
        if (vibrationToggle != null) vibrationToggle.isOn = vibrationOn;
    }

    // Hook this to RESTORE PURCHASES button
    public void RestorePurchases()
    {
        if (TriviaForgeIAPManager.Instance != null)
            TriviaForgeIAPManager.Instance.RestorePurchases();
        else
            Debug.LogWarning("[SettingsMenu] TriviaForgeIAPManager instance not found.");
    }
}