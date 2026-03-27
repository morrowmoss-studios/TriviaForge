using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class TriviaForgeIAPManager : MonoBehaviour, IStoreListener
{
    public static TriviaForgeIAPManager Instance { get; private set; }

    // Product ID – must match EXACTLY on Apple & Google consoles
    public const string ProductId_RemoveAds = "com.morrowmoss.triviaforge.removeads";

    // PlayerPrefs key for the "ads removed" flag
    public const string RemoveAdsPrefsKey = "TF_RemoveAdsPurchased";

    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    /// <summary>
    /// True if this device/account owns the Remove Ads purchase.
    /// </summary>
    public bool HasRemovedAds { get; private set; }

    /// <summary>
    /// Fired when Remove Ads is successfully purchased OR restored.
    /// Subscribe to this to update UI, hide ads, show confirmation, etc.
    /// </summary>
    public event Action OnAdsRemoved;

    // ------------------------------------------------------------
    //  Singleton
    // ------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        HasRemovedAds = PlayerPrefs.GetInt(RemoveAdsPrefsKey, 0) == 1;
    }

    private void Start()
    {
        if (!IsInitialized())
            InitializePurchasing();
    }

    // ------------------------------------------------------------
    //  Init
    // ------------------------------------------------------------
    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var module  = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);

        builder.AddProduct(ProductId_RemoveAds, ProductType.NonConsumable);

        Debug.Log("[IAP] Initializing Unity IAP...");
        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    // ------------------------------------------------------------
    //  Public API called by UI
    // ------------------------------------------------------------

    /// <summary>
    /// Called by your "Remove Ads" button.
    /// </summary>
    public void BuyRemoveAds()
    {
        if (HasRemovedAds)
        {
            Debug.Log("[IAP] Remove Ads already purchased on this device.");
            return;
        }

        if (!IsInitialized())
        {
            Debug.LogWarning("[IAP] BuyRemoveAds called but IAP not initialized.");
            return;
        }

        Product product = storeController.products.WithID(ProductId_RemoveAds);
        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[IAP] Initiating purchase: {product.definition.id}");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning("[IAP] Remove Ads product not available to purchase.");
        }
    }

    /// <summary>
    /// Called by your "Restore Purchases" button.
    /// iOS / macOS only — Google auto-restores on Android.
    /// </summary>
    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        if (!IsInitialized())
        {
            Debug.LogWarning("[IAP] RestorePurchases called but IAP not initialized.");
            return;
        }

        Debug.Log("[IAP] Restoring purchases (Apple platforms).");
        var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();

        apple.RestoreTransactions((result, message) =>
        {
            Debug.Log($"[IAP] RestorePurchases result: {result}, message={message}");
            // If owned, ProcessPurchase fires automatically and handles everything.
            if (result && !HasRemovedAds)
                Debug.Log("[IAP] Restore completed but no purchases found for this account.");
        });
#else
        Debug.Log("[IAP] RestorePurchases is only supported on Apple platforms.");
#endif
    }

    // ------------------------------------------------------------
    //  IStoreListener
    // ------------------------------------------------------------

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[IAP] OnInitialized");
        storeController        = controller;
        storeExtensionProvider = extensions;

        Product product = storeController.products.WithID(ProductId_RemoveAds);
        if (product != null && product.hasReceipt)
        {
            Debug.Log("[IAP] Remove Ads already owned (receipt present).");
            HasRemovedAds = true;
            PlayerPrefs.SetInt(RemoveAdsPrefsKey, 1);
            PlayerPrefs.Save();
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("[IAP] OnInitializeFailed: " + error);
    }

#if UNITY_2017_1_OR_NEWER
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAP] OnInitializeFailed: {error} - {message}");
    }
#endif

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log("[IAP] ProcessPurchase: " + args.purchasedProduct.definition.id);

        if (string.Equals(args.purchasedProduct.definition.id, ProductId_RemoveAds,
                StringComparison.Ordinal))
        {
            Debug.Log("[IAP] Remove Ads purchase/restore SUCCESS.");
            HasRemovedAds = true;

            PlayerPrefs.SetInt(RemoveAdsPrefsKey, 1);
            PlayerPrefs.Save();

            OnAdsRemoved?.Invoke();

            return PurchaseProcessingResult.Complete;
        }

        Debug.LogWarning("[IAP] ProcessPurchase for unhandled product: " +
                         args.purchasedProduct.definition.id);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] Purchase FAILED: {product.definition.id} – {failureReason}");
    }
}