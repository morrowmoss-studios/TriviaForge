using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// All player data now lives in Firestore.
/// Local PlayerProfile is a session cache — loaded on login, pushed to Firestore on every Save().
/// Guests skip all Firestore operations entirely.
/// </summary>
public static class PlayerDatabaseAPI
{
    private static PlayerProfile _currentPlayer;
    private static bool _loaded;

    private const string PlayersCollection = "players";

    // ── Session helpers ───────────────────────────────────────────────────

    public static string CurrentUsername =>
        PlayerPrefs.GetString("TF_CurrentUser", "");

    public static bool IsGuest =>
        PlayerPrefs.GetInt("TF_IsGuest", 0) == 1;

    // ── Password hashing ──────────────────────────────────────────────────

    public static string HashPassword(string password)
    {
        using var sha  = SHA256.Create();
        byte[] bytes   = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    // ── Account creation ──────────────────────────────────────────────────

    public static async Task<(bool success, string error)> CreateAccountAsync(
        string username, string password)
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        try
        {
            var docRef   = FirebaseManager.Db.Collection(PlayersCollection).Document(username);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
                return (false, "That username is already taken.");

            var profile = new PlayerProfile
            {
                playerId                = Guid.NewGuid().ToString(),
                displayName             = username,
                passwordHash            = HashPassword(password),
                totalScore              = 0,
                gamesCompleted          = 0,
                gamesPlayed             = 0,
                highestScore            = 0,
                highestStreak           = 0,
                perfectSolves           = 0,
                correctAnswers          = 0,
                totalAnswers            = 0,
                fastestCrosswordSeconds = 0
            };

            await docRef.SetAsync(ProfileToDict(profile));

            _currentPlayer = profile;
            _loaded        = true;

            Debug.Log($"[PlayerDatabaseAPI] Account created: {username}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] CreateAccount failed: {ex.Message}");
            return (false, "Could not create account. Check your connection.");
        }
    }

    // ── Login ─────────────────────────────────────────────────────────────

    public static async Task<(bool success, string error)> LoginAsync(
        string username, string password)
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        try
        {
            var snapshot = await FirebaseManager.Db
                .Collection(PlayersCollection)
                .Document(username)
                .GetSnapshotAsync();

            if (!snapshot.Exists)
                return (false, "Account not found.");

            var data        = snapshot.ToDictionary();
            string stored   = data.ContainsKey("passwordHash") ? data["passwordHash"].ToString() : "";
            string incoming = HashPassword(password);

            if (stored != incoming)
                return (false, "Incorrect password.");

            _currentPlayer = DictToProfile(username, data);
            _loaded        = true;

            Debug.Log($"[PlayerDatabaseAPI] Logged in: {username}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Login failed: {ex.Message}");
            return (false, "Could not sign in. Check your connection.");
        }
    }

    // ── Legacy no-op (called by SeenContentTracker etc.) ─────────────────

    /// <summary>
    /// No-op — data is loaded on LoginAsync. Safe to call anywhere.
    /// </summary>
    public static void Load() { /* data loaded on login */ }

    // ── Profile access ────────────────────────────────────────────────────

    public static PlayerProfile GetCurrentPlayer() => _currentPlayer;

    /// <summary>
    /// Returns the cached player, or creates a temporary one if none exists.
    /// Matches the old call signature used by SeenContentTracker.
    /// </summary>
    public static PlayerProfile GetOrCreatePlayer(string displayName)
    {
        if (_currentPlayer != null) return _currentPlayer;

        _currentPlayer = new PlayerProfile
        {
            playerId    = Guid.NewGuid().ToString(),
            displayName = displayName
        };
        return _currentPlayer;
    }

    // ── Save (local cache → Firestore) ────────────────────────────────────

    /// <summary>
    /// Full save -- stats + seen IDs. Call at end of game only.
    /// </summary>
    public static void Save()
    {
        if (_currentPlayer == null || IsGuest) return;
        _ = PushToFirestoreAsync(_currentPlayer);
    }

    /// <summary>
    /// Seen IDs only -- lightweight write for mid-session tracking.
    /// Does not rewrite stats, avoiding rate limiting during gameplay.
    /// </summary>
    public static void SaveSeenOnly()
    {
        if (_currentPlayer == null || IsGuest) return;
        _ = PushSeenIdsAsync(_currentPlayer);
    }

    private static async Task PushSeenIdsAsync(PlayerProfile p)
    {
        if (!FirebaseManager.IsReady) return;

        try
        {
            var docRef = FirebaseManager.Db
                .Collection(PlayersCollection)
                .Document(p.displayName);

            var seenDict = new Dictionary<string, object>();

            if (p.seenTriviaIds?.Count > 0)
                seenDict["seenTriviaIds"] = FieldValue.ArrayUnion(p.seenTriviaIds.Cast<object>().ToArray());

            if (p.seenWordokuWords?.Count > 0)
                seenDict["seenWordokuWords"] = FieldValue.ArrayUnion(p.seenWordokuWords.Cast<object>().ToArray());

            if (p.seenCrosswordIds?.Count > 0)
                seenDict["seenCrosswordIds"] = FieldValue.ArrayUnion(p.seenCrosswordIds.Cast<object>().ToArray());

            if (seenDict.Count > 0)
                await docRef.UpdateAsync(seenDict);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Seen IDs push failed: {ex.Message}");
        }
    }

    // ── Score registration ────────────────────────────────────────────────

    public static void RegisterScore(string playerId, string displayName,
                                     int score, string gameMode,
                                     string categoryId, string subcategoryId)
    {
        if (_currentPlayer == null || IsGuest) return;

        _currentPlayer.gamesCompleted++;
        _currentPlayer.gamesPlayed++;
        _currentPlayer.totalScore += score;

        if (score > _currentPlayer.highestScore)
            _currentPlayer.highestScore = score;

        Save();
    }

    /// <summary>
    /// Call at the end of any game session to record streak, accuracy, and perfect solve data.
    /// </summary>
    public static void RegisterGameStats(int highestStreakThisGame,
                                         bool perfectSolve,
                                         int correctAnswers,
                                         int totalAnswers,
                                         int crosswordTimeSeconds = 0)
    {
        if (_currentPlayer == null || IsGuest) return;

        if (highestStreakThisGame > _currentPlayer.highestStreak)
            _currentPlayer.highestStreak = highestStreakThisGame;

        if (perfectSolve)
            _currentPlayer.perfectSolves++;

        _currentPlayer.correctAnswers += correctAnswers;
        _currentPlayer.totalAnswers   += totalAnswers;

        if (crosswordTimeSeconds > 0 &&
            (_currentPlayer.fastestCrosswordSeconds == 0 ||
             crosswordTimeSeconds < _currentPlayer.fastestCrosswordSeconds))
        {
            _currentPlayer.fastestCrosswordSeconds = crosswordTimeSeconds;
        }

        Save();
    }

    // ── Seen content ──────────────────────────────────────────────────────

    public static List<string> GetSeenTriviaIds()    => _currentPlayer?.seenTriviaIds    ?? new List<string>();
    public static List<string> GetSeenWordokuWords()  => _currentPlayer?.seenWordokuWords  ?? new List<string>();
    public static List<string> GetSeenCrosswordIds()  => _currentPlayer?.seenCrosswordIds  ?? new List<string>();

    // ── Leaderboard ───────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the top N players sorted by the given Firestore field (descending).
    /// </summary>
    public static async Task<List<(string username, long value)>> GetLeaderboardAsync(
        string field, int limit = 25)
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        var result = new List<(string, long)>();

        try
        {
            var snapshot = await FirebaseManager.Db
                .Collection(PlayersCollection)
                .OrderByDescending(field)
                .Limit(limit)
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                long val = data.ContainsKey(field) ? Convert.ToInt64(data[field]) : 0;
                result.Add((doc.Id, val));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Leaderboard query failed ({field}): {ex.Message}");
        }

        return result;
    }

    // ── Firestore helpers ─────────────────────────────────────────────────

    private static async Task PushToFirestoreAsync(PlayerProfile p)
    {
        if (!FirebaseManager.IsReady) return;

        try
        {
            var docRef = FirebaseManager.Db
                .Collection(PlayersCollection)
                .Document(p.displayName);

            // Write stats separately from seen ID arrays
            // Stats use MergeAll to update only changed fields
            var statsDict = new Dictionary<string, object>
            {
                { "playerId",                p.playerId },
                { "displayName",             p.displayName },
                { "passwordHash",            p.passwordHash ?? "" },
                { "totalScore",              p.totalScore },
                { "gamesCompleted",          p.gamesCompleted },
                { "highestScore",            p.highestScore },
                { "highestStreak",           p.highestStreak },
                { "perfectSolves",           p.perfectSolves },
                { "correctAnswers",          p.correctAnswers },
                { "totalAnswers",            p.totalAnswers },
                { "fastestCrosswordSeconds", p.fastestCrosswordSeconds },
                { "lastUpdated",             FieldValue.ServerTimestamp }
            };

            await docRef.SetAsync(statsDict, SetOptions.MergeAll);

            // Seen IDs use ArrayUnion so we only send new items, not the whole array
            if (p.seenTriviaIds?.Count > 0 || p.seenWordokuWords?.Count > 0 || p.seenCrosswordIds?.Count > 0)
            {
                var seenDict = new Dictionary<string, object>();

                if (p.seenTriviaIds?.Count > 0)
                    seenDict["seenTriviaIds"] = FieldValue.ArrayUnion(p.seenTriviaIds.Cast<object>().ToArray());

                if (p.seenWordokuWords?.Count > 0)
                    seenDict["seenWordokuWords"] = FieldValue.ArrayUnion(p.seenWordokuWords.Cast<object>().ToArray());

                if (p.seenCrosswordIds?.Count > 0)
                    seenDict["seenCrosswordIds"] = FieldValue.ArrayUnion(p.seenCrosswordIds.Cast<object>().ToArray());

                await docRef.UpdateAsync(seenDict);
            }

            Debug.Log($"[PlayerDatabaseAPI] Firestore synced: {p.displayName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Firestore push failed: {ex.Message}");
        }
    }

    private static Dictionary<string, object> ProfileToDict(PlayerProfile p)
    {
        return new Dictionary<string, object>
        {
            { "playerId",                p.playerId },
            { "displayName",             p.displayName },
            { "passwordHash",            p.passwordHash ?? "" },
            { "totalScore",              p.totalScore },
            { "gamesCompleted",          p.gamesCompleted },
            { "highestScore",            p.highestScore },
            { "highestStreak",           p.highestStreak },
            { "perfectSolves",           p.perfectSolves },
            { "correctAnswers",          p.correctAnswers },
            { "totalAnswers",            p.totalAnswers },
            { "fastestCrosswordSeconds", p.fastestCrosswordSeconds },
            { "seenTriviaIds",           p.seenTriviaIds },
            { "seenWordokuWords",        p.seenWordokuWords },
            { "seenCrosswordIds",        p.seenCrosswordIds },
            { "lastUpdated",             FieldValue.ServerTimestamp }
        };
    }

    private static PlayerProfile DictToProfile(string username, Dictionary<string, object> data)
    {
        T Get<T>(string key, T fallback)
        {
            if (!data.ContainsKey(key)) return fallback;
            try { return (T)Convert.ChangeType(data[key], typeof(T)); }
            catch { return fallback; }
        }

        List<string> GetList(string key)
        {
            if (!data.ContainsKey(key)) return new List<string>();
            if (data[key] is List<object> raw)
                return raw.ConvertAll(o => o.ToString());
            return new List<string>();
        }

        return new PlayerProfile
        {
            playerId                = Get("playerId",                Guid.NewGuid().ToString()),
            displayName             = username,
            passwordHash            = Get("passwordHash",            ""),
            totalScore              = Get("totalScore",              0),
            gamesCompleted          = Get("gamesCompleted",          0),
            gamesPlayed             = Get("gamesCompleted",          0),
            highestScore            = Get("highestScore",            0),
            highestStreak           = Get("highestStreak",           0),
            perfectSolves           = Get("perfectSolves",           0),
            correctAnswers          = Get("correctAnswers",          0),
            totalAnswers            = Get("totalAnswers",            0),
            fastestCrosswordSeconds = Get("fastestCrosswordSeconds", 0),
            seenTriviaIds           = GetList("seenTriviaIds"),
            seenWordokuWords        = GetList("seenWordokuWords"),
            seenCrosswordIds        = GetList("seenCrosswordIds")
        };
    }
}