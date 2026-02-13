using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameDatabaseAPI
{
    private static GameDatabase _db;
    private static bool _isLoaded;

    public static void LoadFromJson(TextAsset jsonAsset)
    {
        if (jsonAsset == null)
        {
            Debug.LogError("GameDatabaseAPI: jsonAsset is null, cannot load DB.");
            return;
        }

        _db = JsonUtility.FromJson<GameDatabase>(jsonAsset.text);
        if (_db == null)
        {
            Debug.LogError("GameDatabaseAPI: Failed to parse JSON.");
            return;
        }

        _isLoaded = true;
        Debug.Log("GameDatabaseAPI: Database loaded with " +
                  (_db.categories != null ? _db.categories.Count : 0) + " categories.");
    }

    private static SubcategoryData FindSubcat(string categoryId, string subcatId)
    {
        if (!_isLoaded || _db == null)
        {
            Debug.LogError("GameDatabaseAPI: Database not loaded.");
            return null;
        }

        if (_db.categories == null)
        {
            Debug.LogError("GameDatabaseAPI: No categories in DB.");
            return null;
        }

        var cat = _db.categories.FirstOrDefault(c => c.id == categoryId);
        if (cat == null)
        {
            Debug.LogError($"GameDatabaseAPI: No category with id '{categoryId}'.");
            return null;
        }

        if (cat.subcategories == null)
        {
            Debug.LogError($"GameDatabaseAPI: Category '{categoryId}' has no subcategories.");
            return null;
        }

        var sub = cat.subcategories.FirstOrDefault(s => s.id == subcatId);
        if (sub == null)
        {
            Debug.LogError($"GameDatabaseAPI: No subcategory '{subcatId}' in category '{categoryId}'.");
            return null;
        }

        return sub;
    }

    public static List<TriviaEntry> GetTrivia(string categoryId, string subcatId)
    {
        var sub = FindSubcat(categoryId, subcatId);
        return sub?.trivia ?? new List<TriviaEntry>();
    }

    public static List<WordokuEntry> GetWordoku(string categoryId, string subcatId)
    {
        var sub = FindSubcat(categoryId, subcatId);
        return sub?.wordoku ?? new List<WordokuEntry>();
    }

    public static List<CrosswordPuzzleEntry> GetCrosswords(string categoryId, string subcatId)
    {
        var sub = FindSubcat(categoryId, subcatId);
        return sub?.crosswords ?? new List<CrosswordPuzzleEntry>();
    }
}
