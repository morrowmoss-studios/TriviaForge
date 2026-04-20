using System;
using UnityEngine;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using Unity.Services.LevelPlay;
#endif

public class TriviaForgeAdManager : MonoBehaviour
{
    public static TriviaForgeAdManager Instance { get; private set; }

    [Header("LevelPlay App Keys")]
    [SerializeField] private string iOSAppKey     = "260d03395";
    [SerializeField] private string androidAppKey = "260d06d1d";

    [Header("Rewarded Ad Unit IDs")]
    [SerializeField] private string rewardedAdUnitId_iOS     = "38hqmj8aam5tpqy9";
    [SerializeField] private string rewardedAdUnitId_Android = "19fvraur51434jc4";

    [Header("Interstitial Ad Unit IDs")]
    [SerializeField] private string interstitialAdUnitId_iOS     = "xybnq2xbx5is8paa";
    [SerializeField] private string interstitialAdUnitId_Android = "qsp1wa0lhu70740e";

    private Action _onRewardEarned;
    private Action _onRewardClosed;
    private Action _onInterstitialClosed;

    public bool AdsDisabled { get; private set; }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    private bool _sdkInitialized;
    private LevelPlayRewardedAd     _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;
#endif

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AdsDisabled = PlayerPrefs.GetInt(TriviaForgeIAPManager.RemoveAdsPrefsKey, 0) == 1;
    }

    private void Start()
    {
        if (AdsDisabled) return;

        if (TriviaForgeIAPManager.Instance != null)
            TriviaForgeIAPManager.Instance.OnAdsRemoved += OnAdsRemovedByPurchase;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        InitLevelPlay();
#else
        Debug.Log("[Ads] Editor build – ads simulated.");
#endif
    }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR

    // ── Init ──────────────────────────────────────────────────────────────

    private void InitLevelPlay()
    {
        string appKey = Application.platform == RuntimePlatform.IPhonePlayer
            ? iOSAppKey : androidAppKey;

        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed  += OnSdkInitFailed;

        Debug.Log($"[Ads] Initializing LevelPlay appKey={appKey}");
        LevelPlay.Init(appKey);
    }

    private void OnSdkInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[Ads] LevelPlay initialized.");
        _sdkInitialized = true;
        SetupRewardedAd();
        SetupInterstitialAd();
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[Ads] LevelPlay init failed: {error}");
    }

    // ── Rewarded ──────────────────────────────────────────────────────────

    private void SetupRewardedAd()
    {
        string id = Application.platform == RuntimePlatform.IPhonePlayer
            ? rewardedAdUnitId_iOS : rewardedAdUnitId_Android;

        _rewardedAd = new LevelPlayRewardedAd(id);
        _rewardedAd.OnAdLoaded        += _ => Debug.Log("[Ads] Rewarded loaded.");
        _rewardedAd.OnAdLoadFailed    += err => Debug.LogWarning($"[Ads] Rewarded load failed: {err}");
        _rewardedAd.OnAdRewarded      += (info, reward) => { _onRewardEarned?.Invoke(); _onRewardEarned = null; };
        _rewardedAd.OnAdClosed        += _ => { _onRewardClosed?.Invoke(); _onRewardClosed = null; _rewardedAd.LoadAd(); };
        _rewardedAd.OnAdDisplayFailed += (info, err) => { Debug.LogWarning($"[Ads] Rewarded show failed: {err}"); _onRewardClosed?.Invoke(); _onRewardClosed = null; };
        _rewardedAd.LoadAd();
    }

    // ── Interstitial ──────────────────────────────────────────────────────

    private void SetupInterstitialAd()
    {
        string id = Application.platform == RuntimePlatform.IPhonePlayer
            ? interstitialAdUnitId_iOS : interstitialAdUnitId_Android;

        _interstitialAd = new LevelPlayInterstitialAd(id);
        _interstitialAd.OnAdLoaded        += _ => Debug.Log("[Ads] Interstitial loaded.");
        _interstitialAd.OnAdLoadFailed    += err => Debug.LogWarning($"[Ads] Interstitial load failed: {err}");
        _interstitialAd.OnAdClosed        += _ => { _onInterstitialClosed?.Invoke(); _onInterstitialClosed = null; _interstitialAd.LoadAd(); };
        _interstitialAd.OnAdDisplayFailed += (info, err) => { Debug.LogWarning($"[Ads] Interstitial show failed: {err}"); _onInterstitialClosed?.Invoke(); _onInterstitialClosed = null; };
        _interstitialAd.LoadAd();
    }

#endif // MOBILE + !EDITOR

    // ── Public API ────────────────────────────────────────────────────────

    public void ShowRewarded(Action onRewarded, Action onClosed = null)
    {
        if (AdsDisabled)
        {
            onRewarded?.Invoke();
            onClosed?.Invoke();
            return;
        }

#if !(UNITY_IOS || UNITY_ANDROID) || UNITY_EDITOR
        Debug.Log("[Ads] (Editor) Simulating rewarded.");
        onRewarded?.Invoke();
        onClosed?.Invoke();
#else
        if (!_sdkInitialized || _rewardedAd == null || !_rewardedAd.IsAdReady())
        {
            Debug.LogWarning("[Ads] Rewarded not ready.");
            onClosed?.Invoke();
            return;
        }

        _onRewardEarned = onRewarded;
        _onRewardClosed = onClosed;
        _rewardedAd.ShowAd();
#endif
    }

    public void ShowInterstitial(Action onClosed = null)
    {
        if (AdsDisabled)
        {
            onClosed?.Invoke();
            return;
        }

#if !(UNITY_IOS || UNITY_ANDROID) || UNITY_EDITOR
        Debug.Log("[Ads] (Editor) Simulating interstitial.");
        onClosed?.Invoke();
#else
        if (!_sdkInitialized || _interstitialAd == null || !_interstitialAd.IsAdReady())
        {
            Debug.LogWarning("[Ads] Interstitial not ready, skipping.");
            onClosed?.Invoke();
            return;
        }

        _onInterstitialClosed = onClosed;
        _interstitialAd.ShowAd();
#endif
    }

    public bool IsRewardedReady
    {
        get
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            return _sdkInitialized && _rewardedAd != null && _rewardedAd.IsAdReady();
#else
            return true;
#endif
        }
    }

    public bool IsInterstitialReady
    {
        get
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            return _sdkInitialized && _interstitialAd != null && _interstitialAd.IsAdReady();
#else
            return true;
#endif
        }
    }

    public void OnAdsRemovedByPurchase()
    {
        AdsDisabled = true;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        _rewardedAd     = null;
        _interstitialAd = null;
        _sdkInitialized = false;
#endif
    }

    private void OnDestroy()
    {
        if (TriviaForgeIAPManager.Instance != null)
            TriviaForgeIAPManager.Instance.OnAdsRemoved -= OnAdsRemovedByPurchase;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdLoaded        -= _ => {};
            _rewardedAd.OnAdLoadFailed    -= _ => {};
            _rewardedAd.OnAdClosed        -= _ => {};
            _rewardedAd.OnAdRewarded      -= (i, r) => {};
            _rewardedAd.OnAdDisplayFailed -= (i, e) => {};
        }

        if (_interstitialAd != null)
        {
            _interstitialAd.OnAdLoaded        -= _ => {};
            _interstitialAd.OnAdLoadFailed    -= _ => {};
            _interstitialAd.OnAdClosed        -= _ => {};
            _interstitialAd.OnAdDisplayFailed -= (i, e) => {};
        }

        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed  -= OnSdkInitFailed;
#endif
    }
}