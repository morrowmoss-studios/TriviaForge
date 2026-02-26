#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

public class CrosswordAIImportWindow : EditorWindow
{
    private string categoryId = "science";
    private string subcategoryId = "physics_quantum";

    // These MUST match the actual field names on your CrosswordEntry class.
    // We don't guess anymore — we set via reflection.
    private string answerFieldName = "answer";
    private string clueFieldName = "clue";
    private string difficultyFieldName = "difficulty"; // optional

    // Paste AI JSON array of CrosswordEntry-shaped objects.
    // Keys must match your Crossword Entry fields.
    private string aiJson =
        "[\n" +
        "  {\n" +
        "    \"id\": \"example_crossword_001\",\n" +
        "    \"answer\": \"ION\",\n" +
        "    \"clue\": \"Atom with a net electric charge\",\n" +
        "    \"difficulty\": \"easy\"\n" +
        "  }\n" +
        "]";

    private Vector2 scroll;

    [MenuItem("TriviaForge/AI Crossword Importer")]
    public static void ShowWindow()
    {
        GetWindow<CrosswordAIImportWindow>("AI Crossword Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("AI Crossword Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        categoryId = EditorGUILayout.TextField("Category ID", categoryId);
        subcategoryId = EditorGUILayout.TextField("Subcategory ID", subcategoryId);

        EditorGUILayout.Space();
        GUILayout.Label("CrosswordEntry Field Names (must match your class)", EditorStyles.boldLabel);
        answerFieldName = EditorGUILayout.TextField("Answer Field", answerFieldName);
        clueFieldName = EditorGUILayout.TextField("Clue Field", clueFieldName);
        difficultyFieldName = EditorGUILayout.TextField("Difficulty Field (optional)", difficultyFieldName);

        EditorGUILayout.Space();

        if (GUILayout.Button("Print CrosswordEntry Fields (Console)", GUILayout.Height(24)))
        {
            PrintEntryFields(typeof(CrosswordEntry));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AI JSON (array of CrosswordEntry):");
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
            Debug.LogError($"[CrosswordAIImportWindow] Could not find {fullPath}. Make sure it's in Assets/Resources.");
            return;
        }

        // Load DB
        string dbJson = File.ReadAllText(fullPath);

        GameDatabase db;
        try { db = JsonUtility.FromJson<GameDatabase>(dbJson); }
        catch (Exception ex)
        {
            Debug.LogError("[CrosswordAIImportWindow] Failed to parse DB JSON: " + ex.Message);
            return;
        }

        if (db == null || db.categories == null)
        {
            Debug.LogError("[CrosswordAIImportWindow] Failed to parse GameDatabase (db/categories null).");
            return;
        }

        // Sanitize + validate AI JSON
        string sanitized = SanitizeAiJson(aiJson);
        string trimmed = sanitized.Trim();

        if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
        {
            Debug.LogError("[CrosswordAIImportWindow] AI JSON must be a JSON ARRAY that starts with '[' and ends with ']'. Paste ONLY the array.");
            return;
        }

        // Wrap array so JsonUtility can parse it
        string wrappedJson = "{ \"entries\": " + trimmed + " }";

        CrosswordEntryList wrapper;
        try { wrapper = JsonUtility.FromJson<CrosswordEntryList>(wrappedJson); }
        catch (Exception ex)
        {
            Debug.LogError("[CrosswordAIImportWindow] Failed to parse AI JSON: " + ex.Message);
            int take = Mathf.Min(1500, wrappedJson.Length);
            Debug.LogError("[CrosswordAIImportWindow] wrappedJson tail:\n" +
                           wrappedJson.Substring(wrappedJson.Length - take, take));
            return;
        }

        if (wrapper == null || wrapper.entries == null || wrapper.entries.Count == 0)
        {
            Debug.LogWarning("[CrosswordAIImportWindow] No entries found after parsing.");
            return;
        }

        // Find category/subcategory
        CategoryData cat = db.categories.Find(c => c.id == categoryId);
        if (cat == null) { Debug.LogError($"[CrosswordAIImportWindow] No category '{categoryId}'."); return; }
        if (cat.subcategories == null) { Debug.LogError($"[CrosswordAIImportWindow] Category '{categoryId}' has no subcategories list."); return; }

        SubcategoryData sub = cat.subcategories.Find(s => s.id == subcategoryId);
        if (sub == null) { Debug.LogError($"[CrosswordAIImportWindow] No subcategory '{subcategoryId}' under '{categoryId}'."); return; }

        if (sub.crosswords == null)
            sub.crosswords = new List<CrosswordEntry>();

        // Cache reflection lookups once (faster + cleaner)
        var entryType = typeof(CrosswordEntry);
        var answerMember = FindStringMember(entryType, answerFieldName);
        var clueMember = FindStringMember(entryType, clueFieldName);
        var diffMember = string.IsNullOrWhiteSpace(difficultyFieldName) ? null : FindStringMember(entryType, difficultyFieldName);

        if (answerMember == null || clueMember == null)
        {
            Debug.LogError("[CrosswordAIImportWindow] Your field names don't match CrosswordEntry.\n" +
                           $"AnswerField='{answerFieldName}' found? {(answerMember != null)}\n" +
                           $"ClueField='{clueFieldName}' found? {(clueMember != null)}\n" +
                           "Click 'Print CrosswordEntry Fields' to see the real names.");
            return;
        }

        // Validate + add
        int before = sub.crosswords.Count;
        int added = 0;

        foreach (var e in wrapper.entries)
        {
            if (e == null) continue;

            // id is a real field in your classes (you used it already)
            if (string.IsNullOrWhiteSpace(e.id))
            {
                Debug.LogWarning("[CrosswordAIImportWindow] Skipping entry with missing id.");
                continue;
            }

            string answer = GetString(e, answerMember);
            string clue = GetString(e, clueMember);

            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(clue))
            {
                Debug.LogWarning($"[CrosswordAIImportWindow] Skipping '{e.id}' (missing {answerFieldName} or {clueFieldName}).");
                continue;
            }

            // Normalize
            answer = answer.Trim().ToUpperInvariant();
            clue = clue.Trim();

            SetString(e, answerMember, answer);
            SetString(e, clueMember, clue);

            // difficulty normalization (optional)
            if (diffMember != null)
            {
                string d = GetString(e, diffMember);
                if (!string.IsNullOrWhiteSpace(d))
                {
                    d = d.Trim().ToLowerInvariant();

                    if (d != "easy" && d != "medium" && d != "hard" && d != "insanity")
                        d = "medium";

                    SetString(e, diffMember, d);
                }
            }
            sub.crosswords.Add(e);
            added++;
        }

        int after = sub.crosswords.Count;

        // Save
        string newDbJson = JsonUtility.ToJson(db, true);
        File.WriteAllText(fullPath, newDbJson);
        AssetDatabase.Refresh();

        Debug.Log($"[CrosswordAIImportWindow] Imported {added} crossword entries into '{categoryId}/{subcategoryId}'. Count was {before}, now {after}.");
    }

    // ---------- helpers ----------

    private static string SanitizeAiJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "[]";

        string s = input
            .Replace('“', '"').Replace('”', '"')
            .Replace('‘', '\'').Replace('’', '\'');

        return s.TrimStart('\uFEFF');
    }

    [Serializable]
    private class CrosswordEntryList
    {
        public List<CrosswordEntry> entries;
    }

    private static MemberInfo FindStringMember(Type t, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // public field
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.FieldType == typeof(string)) return f;

        // public property
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(string) && p.CanRead && p.CanWrite) return p;

        return null;
    }

    private static string GetString(object obj, MemberInfo member)
    {
        if (obj == null || member == null) return null;

        if (member is FieldInfo f) return (string)f.GetValue(obj);
        if (member is PropertyInfo p) return (string)p.GetValue(obj);
        return null;
    }

    private static void SetString(object obj, MemberInfo member, string value)
    {
        if (obj == null || member == null) return;

        if (member is FieldInfo f) f.SetValue(obj, value);
        else if (member is PropertyInfo p) p.SetValue(obj, value);
    }

    private static void PrintEntryFields(Type t)
    {
        Debug.Log($"[AI Importer] {t.Name} public string fields/properties:");

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (f.FieldType == typeof(string))
                Debug.Log($"  FIELD: {f.Name}");
        }

        foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (p.PropertyType == typeof(string))
                Debug.Log($"  PROP: {p.Name}");
        }
    }
}
#endif