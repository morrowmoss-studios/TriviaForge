using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Chill Playlist (Menus / Results)")]
    public AudioClip Village_Ambiance;
    public AudioClip Forest_Walk;
    public AudioClip Celtic_Ambiance;
    public AudioClip Celtic_Atmosphere;

    [Header("Active Playlist (Gameplay)")]
    public AudioClip Magic_Tavern;
    public AudioClip The_Longest_Journey;
    public AudioClip One_Bard_Band;
    public AudioClip Tavern_Loop_One;

    [Header("SFX")]
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;
    public AudioClip sfxStrike;
    public AudioClip sfxButtonPress;
    public AudioClip sfxUIClick;
    public AudioClip sfxFanfare;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [SerializeField] private float crossfadeDuration = 1.5f;

    private bool musicEnabled = true;
    private bool sfxEnabled   = true;

    private List<AudioClip> chillPlaylist  = new List<AudioClip>();
    private List<AudioClip> activePlaylist = new List<AudioClip>();
    private List<AudioClip> currentPlaylist;

    private int  currentTrackIndex = -1;
    private bool isActivePlaylist  = false;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine   crossfadeCoroutine;
    private Coroutine   autoAdvanceCoroutine;

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

        musicVolume  = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.5f);
        sfxVolume    = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
        musicEnabled = PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 1) == 1;
        sfxEnabled   = PlayerPrefs.GetInt(PREF_SFX_ENABLED,   1) == 1;

        activeMusicSource   = musicSourceA;
        inactiveMusicSource = musicSourceB;

        if (musicSourceA != null) { musicSourceA.loop = false; musicSourceA.volume = 0f; }
        if (musicSourceB != null) { musicSourceB.loop = false; musicSourceB.volume = 0f; }
    }

    void Start()
    {
        BuildPlaylists();
        // AudioManager always starts the chill playlist on boot.
        // Scene managers call OnMenuScene/OnGameScene to switch playlists —
        // the IsMusicActive() guard prevents them from interrupting a
        // crossfade that's already running.
        ForceChill();
    }

    // ── Playlist building ─────────────────────────────────────────────────

    void BuildPlaylists()
    {
        chillPlaylist.Clear();
        activePlaylist.Clear();

        if (Village_Ambiance)  chillPlaylist.Add(Village_Ambiance);
        if (Forest_Walk)       chillPlaylist.Add(Forest_Walk);
        if (Celtic_Ambiance)   chillPlaylist.Add(Celtic_Ambiance);
        if (Celtic_Atmosphere) chillPlaylist.Add(Celtic_Atmosphere);

        if (Magic_Tavern)        activePlaylist.Add(Magic_Tavern);
        if (The_Longest_Journey) activePlaylist.Add(The_Longest_Journey);
        if (One_Bard_Band)       activePlaylist.Add(One_Bard_Band);
        if (Tavern_Loop_One)     activePlaylist.Add(Tavern_Loop_One);

        ShuffleList(chillPlaylist);
        ShuffleList(activePlaylist);
    }

    // ── Playlist switching ────────────────────────────────────────────────

    // Called by scene managers — only acts if we need to CHANGE playlist type.
    // If the correct playlist is already running, does nothing.
    public void OnMenuScene()
    {
        if (!isActivePlaylist && IsMusicActive()) return; // already on chill, leave it
        isActivePlaylist = false;
        SwitchPlaylist(chillPlaylist);
    }

    public void OnGameScene()
    {
        if (isActivePlaylist && IsMusicActive()) return; // already on active, leave it
        isActivePlaylist = true;
        SwitchPlaylist(activePlaylist);
    }

    // Internal — starts chill unconditionally (used only on first boot)
    void ForceChill()
    {
        isActivePlaylist = false;
        SwitchPlaylist(chillPlaylist);
    }

    // Keep these as aliases in case anything else calls them directly
    public void PlayChillPlaylist()  => OnMenuScene();
    public void PlayActivePlaylist() => OnGameScene();

    void SwitchPlaylist(List<AudioClip> playlist)
    {
        if (playlist == null || playlist.Count == 0) return;
        if (!SourcesValid()) return;

        currentPlaylist   = playlist;
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    void PlayNextTrack()
    {
        if (currentPlaylist == null || currentPlaylist.Count == 0) return;
        if (!SourcesValid()) return;

        currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Count;
        if (currentTrackIndex == 0) ShuffleList(currentPlaylist);

        StopAllMusicCoroutines();
        crossfadeCoroutine = StartCoroutine(CrossfadeTo(currentPlaylist[currentTrackIndex]));
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        if (newClip == null) yield break;
        if (!SourcesValid()) yield break;

        inactiveMusicSource.clip   = newClip;
        inactiveMusicSource.volume = 0f;

        if (musicEnabled)
            inactiveMusicSource.Play();

        float timer     = 0f;
        float startVol  = activeMusicSource.volume;
        float targetVol = musicEnabled ? musicVolume : 0f;

        while (timer < crossfadeDuration)
        {
            if (!SourcesValid()) yield break;

            timer += Time.deltaTime;
            float t = timer / crossfadeDuration;
            activeMusicSource.volume   = Mathf.Lerp(startVol, 0f,       t);
            inactiveMusicSource.volume = Mathf.Lerp(0f,       targetVol, t);
            yield return null;
        }

        if (!SourcesValid()) yield break;

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
        if (SourcesValid())
            PlayNextTrack();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    bool SourcesValid()
    {
        return activeMusicSource != null && inactiveMusicSource != null;
    }

    // True if music is actively playing OR a crossfade is in progress
    bool IsMusicActive()
    {
        return crossfadeCoroutine != null ||
               (SourcesValid() && activeMusicSource.isPlaying);
    }

    void StopAllMusicCoroutines()
    {
        if (crossfadeCoroutine   != null) { StopCoroutine(crossfadeCoroutine);   crossfadeCoroutine   = null; }
        if (autoAdvanceCoroutine != null) { StopCoroutine(autoAdvanceCoroutine); autoAdvanceCoroutine = null; }
    }

    // ── Enable / Disable ──────────────────────────────────────────────────

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        PlayerPrefs.SetInt(PREF_MUSIC_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!SourcesValid()) return;

        if (!enabled)
        {
            musicSourceA.volume = 0f;
            musicSourceB.volume = 0f;
            musicSourceA.Pause();
            musicSourceB.Pause();
        }
        else
        {
            if (activeMusicSource.clip != null)
            {
                activeMusicSource.UnPause();
                if (!activeMusicSource.isPlaying) activeMusicSource.Play();
                activeMusicSource.volume = musicVolume;
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
        if (musicEnabled && SourcesValid() && activeMusicSource.isPlaying)
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