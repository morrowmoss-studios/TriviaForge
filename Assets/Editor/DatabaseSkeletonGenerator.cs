#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class DatabaseSkeletonGenerator
{
    [MenuItem("TriviaForge/Generate Database Skeleton")]
    public static void Generate()
    {
        // Make sure the ModeSelect scene is open and has a ModeSelectManager in it
        ModeSelectManager modeSelect = Object.FindObjectOfType<ModeSelectManager>();
        if (modeSelect == null)
        {
            Debug.LogError("[DatabaseSkeletonGenerator] Could not find ModeSelectManager in the open scene. " +
                           "Open your ModeSelect scene and try again.");
            return;
        }

        GameDatabase db = new GameDatabase
        {
            categories = new List<CategoryData>()
        };

        foreach (var catConfig in modeSelect.categories)
        {
            if (catConfig == null || string.IsNullOrEmpty(catConfig.id))
                continue;

            CategoryData cat = new CategoryData
            {
                id = catConfig.id,                   // JSON id
                name = catConfig.displayName,        // Human label
                subcategories = new List<SubcategoryData>()
            };

            foreach (var subConfig in catConfig.subcategories)
            {
                if (subConfig == null || string.IsNullOrEmpty(subConfig.id))
                    continue;

                SubcategoryData sub = new SubcategoryData
                {
                    id   = subConfig.id,
                    name = subConfig.displayName,
                    trivia = new List<TriviaEntry>(),
                    wordoku = new List<WordokuEntry>(),
                    crosswords = new List<CrosswordPuzzleEntry>()
                };

                cat.subcategories.Add(sub);
            }

            db.categories.Add(cat);
        }

        string json = JsonUtility.ToJson(db, true);

        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        string fullPath = Path.Combine(resourcesPath, "trivia_database.json");
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[DatabaseSkeletonGenerator] Wrote database skeleton with " +
                  $"{db.categories.Count} categories to: {fullPath}");
    }
}
#endif
