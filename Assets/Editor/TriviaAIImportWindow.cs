#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TriviaAIImportWindow : EditorWindow
{
    // IDs must match your JSON + ModeSelect setup
    private string categoryId = "science";
    private string subcategoryId = "physics_quantum";

    // Where you’ll paste the AI output (JSON array of TriviaEntry)
    private string aiJson = "[\n  {\n    \"id\": \"example_001\",\n    \"questionText\": \"Example question?\",\n    \"answers\": [\"A\", \"B\", \"C\", \"D\"],\n    \"correctIndex\": 1,\n    \"difficulty\": \"easy\"\n  }\n]";

    private Vector2 scroll;

    [MenuItem("TriviaForge/AI Trivia Importer")]
    public static void ShowWindow()
    {
        GetWindow<TriviaAIImportWindow>("AI Trivia Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI Trivia Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        categoryId = EditorGUILayout.TextField("Category ID", categoryId);
        subcategoryId = EditorGUILayout.TextField("Subcategory ID", subcategoryId);

        EditorGUILayout.LabelField("AI JSON (array of TriviaEntry):");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(250));
        aiJson = EditorGUILayout.TextArea(aiJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Import into trivia_database.json", GUILayout.Height(30)))
        {
            Import();
        }
    }

    private void Import()
    {
        // Path to your content DB
        string resourcesPath = "Assets/Resources";
        string fullPath = Path.Combine(resourcesPath, "trivia_database.json");

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[TriviaAIImportWindow] Could not find {fullPath}. " +
                           "Make sure you ran the Database Skeleton Generator first.");
            return;
        }

        string dbJson = File.ReadAllText(fullPath);
        GameDatabase db = JsonUtility.FromJson<GameDatabase>(dbJson);

        if (db == null || db.categories == null)
        {
            Debug.LogError("[TriviaAIImportWindow] Failed to parse GameDatabase from trivia_database.json");
            return;
        }

        // Wrap aiJson array so JsonUtility can parse it
        string wrappedJson = "{ \"entries\": " + aiJson + " }";

        TriviaEntryList wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<TriviaEntryList>(wrappedJson);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TriviaAIImportWindow] Failed to parse AI JSON: " + ex.Message);
            return;
        }

        if (wrapper == null || wrapper.entries == null || wrapper.entries.Count == 0)
        {
            Debug.LogWarning("[TriviaAIImportWindow] No entries found in AI JSON.");
            return;
        }

        // Find category
        CategoryData cat = db.categories.Find(c => c.id == categoryId);
        if (cat == null)
        {
            Debug.LogError($"[TriviaAIImportWindow] No category with id '{categoryId}' in database.");
            return;
        }

        // Find subcategory
        if (cat.subcategories == null)
        {
            Debug.LogError($"[TriviaAIImportWindow] Category '{categoryId}' has no subcategories list.");
            return;
        }

        SubcategoryData sub = cat.subcategories.Find(s => s.id == subcategoryId);
        if (sub == null)
        {
            Debug.LogError($"[TriviaAIImportWindow] No subcategory with id '{subcategoryId}' under '{categoryId}'.");
            return;
        }

        if (sub.trivia == null)
            sub.trivia = new List<TriviaEntry>();

        int beforeCount = sub.trivia.Count;
        sub.trivia.AddRange(wrapper.entries);
        int afterCount = sub.trivia.Count;

        // Save DB back to file
        string newDbJson = JsonUtility.ToJson(db, true);
        File.WriteAllText(fullPath, newDbJson);
        AssetDatabase.Refresh();

        Debug.Log($"[TriviaAIImportWindow] Imported {wrapper.entries.Count} trivia entries into " +
                  $"category '{categoryId}', subcategory '{subcategoryId}'. " +
                  $"Count was {beforeCount}, now {afterCount}.");
    }

    [System.Serializable]
    private class TriviaEntryList
    {
        public List<TriviaEntry> entries;
    }
}
#endif
