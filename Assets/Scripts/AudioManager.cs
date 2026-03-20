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
    public AudioClip sfxCorrect;        // buddhist-bell-01
    public AudioClip sfxWrong;          // thump
    public AudioClip sfxStrike;         // drum-hit-3
    public AudioClip sfxButtonPress;    // flashlight-button-press
    public AudioClip sfxUIClick;        // click_81
    public AudioClip sfxFanfare;        // orchestral-crescendo

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [SerializeField] private float crossfadeDuration = 1.5f;

    private List<AudioClip> chillPlaylist = new List<AudioClip>();
    private List<AudioClip> activePlaylist = new List<AudioClip>();

    private List<AudioClip> currentPlaylist;
    private int currentTrackIndex = -1;
    private bool isActivePlaylist = false;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine crossfadeCoroutine;
    private Coroutine autoAdvanceCoroutine;

    private const string PREF_MUSIC_VOL = "MusicVolume";
    private const string PREF_SFX_VOL = "SFXVolume";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;

        musicSourceA.loop = false;
        musicSourceB.loop = false;
        musicSourceA.volume = 0f;
        musicSourceB.volume = 0f;
    }

    void Start()
    {
        BuildPlaylists();
        PlayChillPlaylist();
    }

    void BuildPlaylists()
    {
        chillPlaylist.Clear();
        activePlaylist.Clear();

        if (Village_Ambiance) chillPlaylist.Add(Village_Ambiance);
        if (Forest_Walk)      chillPlaylist.Add(Forest_Walk);
        if (Celtic_Ambiance)  chillPlaylist.Add(Celtic_Ambiance);
        if (Celtic_Atmosphere)chillPlaylist.Add(Celtic_Atmosphere);

        if (Magic_Tavern)         activePlaylist.Add(Magic_Tavern);
        if (The_Longest_Journey)  activePlaylist.Add(The_Longest_Journey);
        if (One_Bard_Band)        activePlaylist.Add(One_Bard_Band);
        if (Tavern_Loop_One)      activePlaylist.Add(Tavern_Loop_One);

        ShuffleList(chillPlaylist);
        ShuffleList(activePlaylist);
    }

    // ── Public playlist switchers ──────────────────────────────────────────

    public void PlayChillPlaylist()
    {
        if (!isActivePlaylist || currentPlaylist != chillPlaylist)
        {
            isActivePlaylist = false;
            SwitchPlaylist(chillPlaylist);
        }
    }

    public void PlayActivePlaylist()
    {
        if (isActivePlaylist || currentPlaylist != activePlaylist)
        {
            isActivePlaylist = true;
            SwitchPlaylist(activePlaylist);
        }
    }

    void SwitchPlaylist(List<AudioClip> playlist)
    {
        if (playlist.Count == 0) return;
        currentPlaylist = playlist;
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    void PlayNextTrack()
    {
        if (currentPlaylist == null || currentPlaylist.Count == 0) return;

        currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Count;

        // Reshuffle when we loop back to start
        if (currentTrackIndex == 0)
            ShuffleList(currentPlaylist);

        AudioClip nextClip = currentPlaylist[currentTrackIndex];

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeTo(nextClip));
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        // Set up the inactive source with the new clip
        inactiveMusicSource.clip = newClip;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        float timer = 0f;
        float startVolume = activeMusicSource.volume;

        while (timer < crossfadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / crossfadeDuration;
            activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            inactiveMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        activeMusicSource.Stop();
        activeMusicSource.volume = 0f;

        // Swap references
        AudioSource temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;

        // Schedule next track
        if (autoAdvanceCoroutine != null) StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = StartCoroutine(AutoAdvance(newClip.length - crossfadeDuration));
    }

    IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(delay, 0.5f));
        PlayNextTrack();
    }

    // ── SFX ───────────────────────────────────────────────────────────────

    public void PlayCorrect()      => PlaySFX(sfxCorrect);
    public void PlayWrong()        => PlaySFX(sfxWrong);
    public void PlayStrike()       => PlaySFX(sfxStrike);
    public void PlayButtonPress()  => PlaySFX(sfxButtonPress);
    public void PlayUIClick()      => PlaySFX(sfxUIClick);
    public void PlayFanfare()      => PlaySFX(sfxFanfare);

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── Volume control ────────────────────────────────────────────────────

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (activeMusicSource.isPlaying)
            activeMusicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, musicVolume);
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume()   => sfxVolume;

    // ── Scene helpers ─────────────────────────────────────────────────────
    // Call these from your scene managers on Start()

    /// <summary>Call from MainMenu, ModeSelect, TriviaResult, Leaderboard</summary>
    public void OnMenuScene()  => PlayChillPlaylist();

    /// <summary>Call from TriviaMode, CrosswordMode, WordokuMode</summary>
    public void OnGameScene()  => PlayActivePlaylist();

    // ── Utility ───────────────────────────────────────────────────────────

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}