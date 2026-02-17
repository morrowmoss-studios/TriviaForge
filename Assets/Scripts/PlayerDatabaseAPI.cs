using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PlayerDatabaseAPI
{
    private static PlayerDatabase _db;
    private static bool _loaded;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "player_database.json");

    // ---------- LOAD / SAVE ----------

    public static void Load()
    {
        if (_loaded) return;

        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            _db = JsonUtility.FromJson<PlayerDatabase>(json);
            if (_db == null)
            {
                Debug.LogWarning("[PlayerDatabaseAPI] Failed to parse existing player DB, creating new one.");
                _db = new PlayerDatabase();
            }
        }
        else
        {
            Debug.Log("[PlayerDatabaseAPI] No player DB found, creating new one.");
            _db = new PlayerDatabase();
            Save();
        }

        _loaded = true;
        Debug.Log($"[PlayerDatabaseAPI] Loaded player DB from: {SavePath}");
    }

    public static void Save()
    {
        if (_db == null) _db = new PlayerDatabase();

        string json = JsonUtility.ToJson(_db, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[PlayerDatabaseAPI] Saved player DB to: {SavePath}");
    }

    private static void EnsureLoaded()
    {
        if (!_loaded) Load();
    }

    // ---------- PLAYER PROFILES ----------

    public static PlayerProfile GetOrCreatePlayer(string displayName)
    {
        EnsureLoaded();

        // Simple: one profile per displayName for now
        var player = _db.players.Find(p => p.displayName == displayName);
        if (player != null) return player;

        player = new PlayerProfile
        {
            playerId = System.Guid.NewGuid().ToString(),
            displayName = displayName,
            totalScore = 0,
            gamesPlayed = 0,
            highestScore = 0
        };

        _db.players.Add(player);
        Save();
        return player;
    }

    // ---------- SCORES / LEADERBOARD ----------

    public static void RegisterScore(string playerId, string displayName,
                                    int score,
                                    string gameMode,
                                    string categoryId,
                                    string subcategoryId)
    {
        EnsureLoaded();

        // Update player stats
        var player = _db.players.Find(p => p.playerId == playerId);
        if (player == null)
        {
            // fallback: create profile if missing
            player = new PlayerProfile
            {
                playerId = playerId,
                displayName = displayName
            };
            _db.players.Add(player);
        }

        player.gamesPlayed++;
        player.totalScore += score;
        if (score > player.highestScore)
            player.highestScore = score;

        // Add to global leaderboard list
        var entry = new HighScoreEntry
        {
            playerId = playerId,
            displayName = displayName,
            score = score,
            gameMode = gameMode,
            categoryId = categoryId,
            subcategoryId = subcategoryId,
            timestamp = System.DateTime.UtcNow.ToString("o")
        };

        _db.globalHighScores.Add(entry);

        // Optional: keep only top N scores
        _db.globalHighScores.Sort((a, b) => b.score.CompareTo(a.score));
        if (_db.globalHighScores.Count > 1000) // or smaller if you want
        {
            _db.globalHighScores.RemoveRange(1000, _db.globalHighScores.Count - 1000);
        }

        Save();
    }

    public static List<HighScoreEntry> GetTopScores(int maxCount,
                                                    string gameMode = null,
                                                    string categoryId = null,
                                                    string subcategoryId = null)
    {
        EnsureLoaded();

        var list = _db.globalHighScores;

        // Filter if requested
        if (!string.IsNullOrEmpty(gameMode))
            list = list.FindAll(e => e.gameMode == gameMode);

        if (!string.IsNullOrEmpty(categoryId))
            list = list.FindAll(e => e.categoryId == categoryId);

        if (!string.IsNullOrEmpty(subcategoryId))
            list = list.FindAll(e => e.subcategoryId == subcategoryId);

        list.Sort((a, b) => b.score.CompareTo(a.score));

        if (list.Count > maxCount)
            list = list.GetRange(0, maxCount);

        return list;
    }
}
