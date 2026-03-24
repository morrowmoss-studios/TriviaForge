using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Chill Playlist (Main Menu / Settings only)")]
    public AudioClip Village_Ambiance;
    public AudioClip Celtic_Ambiance;

    [Header("Active Playlist (Gameplay + Results + Everything Else)")]
    public AudioClip Magic_Tavern;
    public AudioClip The_Longest_Journey;
    public AudioClip One_Bard_Band;
    public AudioClip Tavern_Loop_One;
    public AudioClip Forest_Walk;
    public AudioClip Celtic_Atmosphere;

    [Header("SFX")]
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;
    public AudioClip sfxStrike;
    public AudioClip sfxButtonPress;
    public AudioClip sfxUIClick;
    public AudioClip sfxFanfare;
    public AudioClip sfxTilePlaced;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [SerializeField] private float crossfadeDuration = 1.5f;

    private bool musicEnabled = true;
    private bool sfxEnabled   = true;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource sfxSource;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;

    private List<AudioClip> chillPlaylist  = new List<AudioClip>();
    private List<AudioClip> activePlaylist = new List<AudioClip>();
    private List<AudioClip> currentPlaylist;

    private int  currentTrackIndex = -1;
    private bool isActivePlaylist  = false;
    private bool isMusicPlaying    = false;

    private Coroutine crossfadeCoroutine;
    private Coroutine autoAdvanceCoroutine;

    // Only these scenes use the chill playlist — everything else uses active
    private readonly HashSet<string> chillScenes = new HashSet<string>
    {
        "MainMenu",
        "Login_PopUp",
        "CreateAccount_PopUp",
        "ModeSelect",
        "Settings",
        "AboutGame",
        "HowToPlay",
        "Credits",
        "PrivacyPolicy",
        "TermsOfUse"
    };

    private const string PREF_MUSIC_VOL     = "MusicVolume";
    private const string PREF_SFX_VOL       = "SFXVolume";
    private const string PREF_MUSIC_ENABLED = "MusicEnabled";
    private const string PREF_SFX_ENABLED   = "SFXEnabled";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        musicVolume  = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.5f);
        sfxVolume    = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
        musicEnabled = PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 1) == 1;
        sfxEnabled   = PlayerPrefs.GetInt(PREF_SFX_ENABLED,   1) == 1;

        sourceA   = CreateMusicSource("MusicSourceA");
        sourceB   = CreateMusicSource("MusicSourceB");
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop         = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.playOnAwake  = false;

        activeMusicSource   = sourceA;
        inactiveMusicSource = sourceB;

        BuildPlaylists();

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start on chill since we boot from MainMenu
        ForceStartChill();
    }

    AudioSource CreateMusicSource(string label)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop         = false;
        s.volume       = 0f;
        s.spatialBlend = 0f;
        s.playOnAwake  = false;
        return s;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Scene loaded callback ─────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Disable other AudioListeners
        foreach (var listener in FindObjectsOfType<AudioListener>())
        {
            if (listener == null) continue;
            if (listener.gameObject != this.gameObject)
                listener.enabled = false;
        }

        if (!musicEnabled) return;

        bool shouldBeChill = chillScenes.Contains(scene.name);

        if (shouldBeChill && isActivePlaylist)
        {
            // Switch to chill
            isActivePlaylist  = false;
            currentPlaylist   = chillPlaylist;
            currentTrackIndex = -1;
            PlayNextTrack();
        }
        else if (!shouldBeChill && !isActivePlaylist)
        {
            // Switch to active
            isActivePlaylist  = true;
            currentPlaylist   = activePlaylist;
            currentTrackIndex = -1;
            PlayNextTrack();
        }
        // Otherwise music is already playing the right playlist — leave it alone
    }

    // ── Playlist building ─────────────────────────────────────────────────

    void BuildPlaylists()
    {
        chillPlaylist.Clear();
        activePlaylist.Clear();

        // Chill — just two calm tracks for menus
        if (Village_Ambiance) chillPlaylist.Add(Village_Ambiance);
        if (Celtic_Ambiance)  chillPlaylist.Add(Celtic_Ambiance);

        // Active — everything else, used for gameplay AND results/scores/leaderboard
        if (Magic_Tavern)        activePlaylist.Add(Magic_Tavern);
        if (The_Longest_Journey) activePlaylist.Add(The_Longest_Journey);
        if (One_Bard_Band)       activePlaylist.Add(One_Bard_Band);
        if (Tavern_Loop_One)     activePlaylist.Add(Tavern_Loop_One);
        if (Forest_Walk)         activePlaylist.Add(Forest_Walk);
        if (Celtic_Atmosphere)   activePlaylist.Add(Celtic_Atmosphere);

        ShuffleList(chillPlaylist);
        ShuffleList(activePlaylist);
    }

    void ForceStartChill()
    {
        isActivePlaylist  = false;
        currentPlaylist   = chillPlaylist;
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    // ── Public helpers ────────────────────────────────────────────────────

    // These are still available if any script calls them directly
    public void OnGameScene()
    {
        if (isActivePlaylist && isMusicPlaying) return;
        isActivePlaylist  = true;
        currentPlaylist   = activePlaylist;
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    public void OnMenuScene()
    {
        if (!isActivePlaylist && isMusicPlaying) return;
        isActivePlaylist  = false;
        currentPlaylist   = chillPlaylist;
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    // ── Track playback ────────────────────────────────────────────────────

    void PlayNextTrack()
    {
        if (currentPlaylist == null || currentPlaylist.Count == 0) return;
        if (activeMusicSource == null || inactiveMusicSource == null) return;

        currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Count;
        if (currentTrackIndex == 0) ShuffleList(currentPlaylist);

        StopAllMusicCoroutines();
        crossfadeCoroutine = StartCoroutine(CrossfadeTo(currentPlaylist[currentTrackIndex]));
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        if (newClip == null) yield break;
        if (activeMusicSource == null || inactiveMusicSource == null) yield break;

        isMusicPlaying = false;

        inactiveMusicSource.clip         = newClip;
        inactiveMusicSource.volume       = 0f;
        inactiveMusicSource.spatialBlend = 0f;

        if (musicEnabled)
        {
            inactiveMusicSource.Play();
            isMusicPlaying = true;
        }

        float timer     = 0f;
        float startVol  = activeMusicSource.volume;
        float targetVol = musicEnabled ? musicVolume : 0f;

        while (timer < crossfadeDuration)
        {
            if (activeMusicSource == null || inactiveMusicSource == null) yield break;

            timer += Time.deltaTime;
            float t = timer / crossfadeDuration;
            activeMusicSource.volume   = Mathf.Lerp(startVol, 0f,       t);
            inactiveMusicSource.volume = Mathf.Lerp(0f,       targetVol, t);
            yield return null;
        }

        if (activeMusicSource == null || inactiveMusicSource == null) yield break;

        activeMusicSource.Stop();
        activeMusicSource.volume = 0f;

        AudioSource temp    = activeMusicSource;
        activeMusicSource   = inactiveMusicSource;
        inactiveMusicSource = temp;

        crossfadeCoroutine = null;

        autoAdvanceCoroutine = StartCoroutine(AutoAdvance(newClip.length - crossfadeDuration));
    }

    IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(delay, 0.5f));
        isMusicPlaying = false;
        PlayNextTrack();
    }

    void StopAllMusicCoroutines()
    {
        if (crossfadeCoroutine   != null) { StopCoroutine(crossfadeCoroutine);   crossfadeCoroutine   = null; }
        if (autoAdvanceCoroutine != null) { StopCoroutine(autoAdvanceCoroutine); autoAdvanceCoroutine = null; }
        isMusicPlaying = false;
    }

    // ── Enable / Disable ──────────────────────────────────────────────────

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        PlayerPrefs.SetInt(PREF_MUSIC_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled)
        {
            if (sourceA != null) { sourceA.volume = 0f; sourceA.Pause(); }
            if (sourceB != null) { sourceB.volume = 0f; sourceB.Pause(); }
            isMusicPlaying = false;
        }
        else
        {
            if (activeMusicSource != null && activeMusicSource.clip != null)
            {
                activeMusicSource.UnPause();
                if (!activeMusicSource.isPlaying) activeMusicSource.Play();
                activeMusicSource.volume = musicVolume;
                isMusicPlaying = true;
            }
            else
            {
                PlayNextTrack();
            }
        }
    }

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        PlayerPrefs.SetInt(PREF_SFX_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool GetMusicEnabled() => musicEnabled;
    public bool GetSFXEnabled()   => sfxEnabled;

    // ── Volume ────────────────────────────────────────────────────────────

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (musicEnabled && activeMusicSource != null)
            activeMusicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume()   => sfxVolume;

    // ── SFX ───────────────────────────────────────────────────────────────

    public void PlayCorrect()     => PlaySFX(sfxCorrect);
    public void PlayWrong()       => PlaySFX(sfxWrong);
    public void PlayStrike()      => PlaySFX(sfxStrike);
    public void PlayButtonPress() => PlaySFX(sfxButtonPress);
    public void PlayUIClick()     => PlaySFX(sfxUIClick);
    public void PlayFanfare()     => PlaySFX(sfxFanfare);
    public void PlayTilePlaced()  => PlaySFX(sfxTilePlaced);

    void PlaySFX(AudioClip clip)
    {
        if (!sfxEnabled || clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── Utility ───────────────────────────────────────────────────────────

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }
}