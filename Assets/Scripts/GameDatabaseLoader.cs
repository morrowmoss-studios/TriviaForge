using UnityEngine;

public class GameDatabaseLoader : MonoBehaviour
{
    [SerializeField] private TextAsset databaseJson;

    private void Awake()
    {
        if (databaseJson == null)
        {
            // Try auto-load from Resources if not wired manually
            databaseJson = Resources.Load<TextAsset>("trivia_database");
        }

        if (databaseJson == null)
        {
            Debug.LogError("GameDatabaseLoader: No JSON assigned and not found in Resources.");
            return;
        }

        GameDatabaseAPI.LoadFromJson(databaseJson);
    }
}