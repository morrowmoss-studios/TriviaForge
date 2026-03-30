using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static FirebaseFirestore Db     { get; private set; }
    public static bool IsReady             { get; private set; }

    public static event Action OnFirebaseReady;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status == DependencyStatus.Available)
            {
                Db      = FirebaseFirestore.DefaultInstance;
                IsReady = true;
                Debug.Log("[FirebaseManager] Firebase ready.");
                OnFirebaseReady?.Invoke();
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Dependency error: {status}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Init failed: {ex.Message}");
        }
    }

    public static async Task WaitUntilReadyAsync()
    {
        while (!IsReady)
            await Task.Delay(50);
    }
}