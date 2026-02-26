#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class WordokuAIImportWindow : EditorWindow
{
    private string categoryId = "science";
    private string subcategoryId = "physics_quantum";

    // IMPORTANT: keys must match your real WordokuEntry fields
    private string aiJson =
        "[\n" +
        "  {\n" +
        "    \"id\": \"example_wordoku_001\",\n" +
        "    \"word\": \"ASTRONOMY\",\n" +
        "    \"difficulty\": \"easy\"\n" +
        "  }\n" +
        "]";

    private Vector2 scroll;

    [MenuItem("TriviaForge/AI Wordoku Importer")]
    public static void ShowWindow()
    {
        GetWindow<WordokuAIImportWindow>("AI Wordoku Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI Wordoku Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        categoryId = EditorGUILayout.TextField("Category ID", categoryId);
        subcategoryId = EditorGUILayout.TextField("Subcategory ID", subcategoryId);

        EditorGUILayout.LabelField("AI JSON (array of WordokuEntry):");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(250));
        aiJson = EditorGUILayout.TextArea(aiJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Import into trivia_database.json", GUILayout.Height(30)))
            Import();
    }

    private void Import()
    {
        string resourcesPath = "Assets/Resources";
        string fullPath = Path.Combine(resourcesPath, "trivia_database.json");

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[WordokuAIImportWindow] Could not find {fullPath}. Make sure it's in Assets/Resources.");
            return;
        }

        // Load DB
        string dbJson = File.ReadAllText(fullPath);

        GameDatabase db;
        try { db = JsonUtility.FromJson<GameDatabase>(dbJson); }
        catch (Exception ex)
        {
            Debug.LogError("[WordokuAIImportWindow] Failed to parse DB JSON: " + ex.Message);
            return;
        }

        if (db == null || db.categories == null)
        {
            Debug.LogError("[WordokuAIImportWindow] Failed to parse GameDatabase (db/categories null).");
            return;
        }

        // Sanitize AI JSON
        string sanitized = SanitizeAiJson(aiJson);
        string trimmed = sanitized.Trim();

        if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
        {
            Debug.LogError("[WordokuAIImportWindow] AI JSON must be a JSON ARRAY that starts with '[' and ends with ']'. Paste ONLY the array.");
            return;
        }

        // Wrap so JsonUtility can parse
        string wrappedJson = "{ \"entries\": " + trimmed + " }";

        WordokuEntryList wrapper;
        try { wrapper = JsonUtility.FromJson<WordokuEntryList>(wrappedJson); }
        catch (Exception ex)
        {
            Debug.LogError("[WordokuAIImportWindow] Failed to parse AI JSON: " + ex.Message);
            int take = Mathf.Min(1500, wrappedJson.Length);
            Debug.LogError("[WordokuAIImportWindow] wrappedJson tail:\n" +
                           wrappedJson.Substring(wrappedJson.Length - take, take));
            return;
        }

        if (wrapper == null || wrapper.entries == null || wrapper.entries.Count == 0)
        {
            Debug.LogWarning("[WordokuAIImportWindow] No entries found after parsing.");
            return;
        }

        // Find category/subcategory
        CategoryData cat = db.categories.Find(c => c.id == categoryId);
        if (cat == null) { Debug.LogError($"[WordokuAIImportWindow] No category '{categoryId}'."); return; }
        if (cat.subcategories == null) { Debug.LogError($"[WordokuAIImportWindow] Category '{categoryId}' has no subcategories list."); return; }

        SubcategoryData sub = cat.subcategories.Find(s => s.id == subcategoryId);
        if (sub == null) { Debug.LogError($"[WordokuAIImportWindow] No subcategory '{subcategoryId}' under '{categoryId}'."); return; }

        if (sub.wordoku == null)
            sub.wordoku = new List<global::WordokuEntry>();

        // Validate + add
        int before = sub.wordoku.Count;
        int added = 0;

        foreach (var e in wrapper.entries)
        {
            if (e == null) continue;

            if (string.IsNullOrWhiteSpace(e.id))
            {
                Debug.LogWarning("[WordokuAIImportWindow] Skipping entry with missing id.");
                continue;
            }

            // If this line ever errors, it means your real class doesn't use "word" as the field name.
            if (string.IsNullOrWhiteSpace(e.word))
            {
                Debug.LogWarning($"[WordokuAIImportWindow] Skipping '{e.id}' (missing word).");
                continue;
            }

            string w = e.word.Trim().ToUpperInvariant();
            e.word = w;

            if (w.Length != 9)
            {
                Debug.LogWarning($"[WordokuAIImportWindow] Skipping '{e.id}' (word must be 9 letters). Got '{w}' length={w.Length}");
                continue;
            }

            sub.wordoku.Add(e);
            added++;
        }

        int after = sub.wordoku.Count;

        // Save
        string newDbJson = JsonUtility.ToJson(db, true);
        File.WriteAllText(fullPath, newDbJson);
        AssetDatabase.Refresh();

        Debug.Log($"[WordokuAIImportWindow] Imported {added} wordoku entries into '{categoryId}/{subcategoryId}'. Count was {before}, now {after}.");
    }

    private static string SanitizeAiJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "[]";

        string s = input
            .Replace('“', '"').Replace('”', '"')
            .Replace('‘', '\'').Replace('’', '\'');

        return s.TrimStart('\uFEFF');
    }

    [Serializable]
    private class WordokuEntryList
    {
        public List<global::WordokuEntry> entries;
    }
}
#endif