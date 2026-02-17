using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static API for reading game content (trivia, wordoku, crosswords)
/// from trivia_database.json in a Resources folder.
/// </summary>
public static class GameDatabaseAPI
{
    private const string ResourcePath = "trivia_database"; // looks for Assets/Resources/trivia_database.json

    private static GameDatabase _db;
    private static bool _loaded;

    /// <summary>
    /// Ensure the database is loaded from Resources. Safe to call multiple times.
    /// </summary>
    private static void EnsureLoaded()
    {
        if (_loaded) return;

        TextAsset jsonAsset = Resources.Load<TextAsset>(ResourcePath);
        if (jsonAsset == null)
        {
            Debug.LogError($"GameDatabaseAPI: Could not find '{ResourcePath}.json' in a Resources folder.");
            return;
        }

        try
        {
            _db = JsonUtility.FromJson<GameDatabase>(jsonAsset.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameDatabaseAPI: Failed to parse database JSON. Exception: {ex.Message}");
            _db = null;
        }

        if (_db == null || _db.categories == null)
        {
            Debug.LogError("GameDatabaseAPI: Database JSON parsed, but data is null or has no categories.");
            return;
        }

        _loaded = true;
        Debug.Log($"GameDatabaseAPI: Loaded database with {_db.categories.Count} categories.");
    }

    /// <summary>
    /// Get all trivia entries for a given category/subcategory id pair.
    /// Returns an empty list if something goes wrong.
    /// </summary>
    public static List<TriviaEntry> GetTrivia(string categoryId, string subcategoryId)
    {
        EnsureLoaded();

        if (!_loaded || _db == null)
        {
            Debug.LogError("GameDatabaseAPI: Database not loaded.");
            return new List<TriviaEntry>();
        }

        CategoryData cat = _db.categories.Find(c => c.id == categoryId);
        if (cat == null)
        {
            Debug.LogError($"GameDatabaseAPI: No category with id '{categoryId}'.");
            return new List<TriviaEntry>();
        }

        if (cat.subcategories == null)
        {
            Debug.LogError($"GameDatabaseAPI: Category '{categoryId}' has no subcategories list.");
            return new List<TriviaEntry>();
        }

        SubcategoryData sub = cat.subcategories.Find(s => s.id == subcategoryId);
        if (sub == null)
        {
            Debug.LogError($"GameDatabaseAPI: No subcategory with id '{subcategoryId}' under category '{categoryId}'.");
            return new List<TriviaEntry>();
        }

        if (sub.trivia == null)
        {
            Debug.LogWarning($"GameDatabaseAPI: Subcategory '{subcategoryId}' has no trivia list yet.");
            return new List<TriviaEntry>();
        }

        return sub.trivia;
    }

    /// <summary>
    /// Optional: get raw database if you ever need to inspect it.
    /// </summary>
    public static GameDatabase GetRawDatabase()
    {
        EnsureLoaded();
        return _db;
    }
}
