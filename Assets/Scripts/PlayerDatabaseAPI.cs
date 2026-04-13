using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// All player auth now goes through Firebase Authentication.
/// Player profile data (stats, seen content) lives in Firestore keyed to Firebase Auth UID.
/// </summary>
public static class PlayerDatabaseAPI
{
    private static PlayerProfile _currentPlayer;
    private static FirebaseAuth  _auth;
    private static FirebaseUser  _firebaseUser;
    private static bool          _loaded;

    private const string PlayersCollection  = "players";
    private const string UsernamesCollection = "usernames";

    // ── Auth helpers ──────────────────────────────────────────────────────

    public static string CurrentUsername =>
        PlayerPrefs.GetString("TF_CurrentUser", "");

    public static bool IsGuest =>
        PlayerPrefs.GetInt("TF_IsGuest", 0) == 1;

    private static FirebaseAuth Auth
    {
        get { if (_auth == null) _auth = FirebaseAuth.DefaultInstance; return _auth; }
    }

    // ── Account creation ──────────────────────────────────────────────────

    public static async Task<(bool success, string error)> CreateAccountAsync(
        string username, string email, string password)
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        try
        {
            // Check username isn't already taken
            var usernameDoc = await FirebaseManager.Db
                .Collection(UsernamesCollection).Document(username).GetSnapshotAsync();

            if (usernameDoc.Exists)
                return (false, "That username is already taken.");

            // Create Firebase Auth account
            var authResult = await Auth.CreateUserWithEmailAndPasswordAsync(email, password);
            _firebaseUser  = authResult.User;

            // Set display name in Firebase Auth
            await _firebaseUser.UpdateUserProfileAsync(new UserProfile { DisplayName = username });

            // Reserve username → uid mapping
            await FirebaseManager.Db.Collection(UsernamesCollection).Document(username)
                .SetAsync(new Dictionary<string, object> { { "uid", _firebaseUser.UserId } });

            // Create Firestore player profile
            var profile = new PlayerProfile
            {
                playerId    = _firebaseUser.UserId,
                displayName = username,
                email       = email
            };

            await FirebaseManager.Db.Collection(PlayersCollection)
                .Document(_firebaseUser.UserId).SetAsync(ProfileToDict(profile));

            _currentPlayer = profile;
            _loaded        = true;

            Debug.Log($"[PlayerDatabaseAPI] Account created: {username}");
            return (true, null);
        }
        catch (Firebase.FirebaseException ex)
        {
            string msg = ((AuthError)ex.ErrorCode) switch
            {
                AuthError.EmailAlreadyInUse => "That email is already registered.",
                AuthError.InvalidEmail      => "Please enter a valid email address.",
                AuthError.WeakPassword      => "Password must be at least 6 characters.",
                _                           => "Could not create account. Check your connection."
            };
            Debug.LogError($"[PlayerDatabaseAPI] CreateAccount failed: {ex.Message}");
            return (false, msg);
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
            // Look up uid from username
            var usernameDoc = await FirebaseManager.Db
                .Collection(UsernamesCollection).Document(username).GetSnapshotAsync();

            if (!usernameDoc.Exists)
                return (false, "Account not found.");

            string uid   = usernameDoc.ToDictionary()["uid"].ToString();
            string email = await GetEmailForUid(uid);

            if (string.IsNullOrEmpty(email))
                return (false, "Account not found.");

            // Sign in with Firebase Auth
            var authResult = await Auth.SignInWithEmailAndPasswordAsync(email, password);
            _firebaseUser  = authResult.User;

            // Load Firestore profile
            var snapshot = await FirebaseManager.Db
                .Collection(PlayersCollection).Document(_firebaseUser.UserId).GetSnapshotAsync();

            if (!snapshot.Exists)
                return (false, "Account data not found.");

            _currentPlayer = DictToProfile(_firebaseUser.UserId, snapshot.ToDictionary());
            _loaded        = true;

            Debug.Log($"[PlayerDatabaseAPI] Logged in: {username}");
            return (true, null);
        }
        catch (Firebase.FirebaseException ex)
        {
            string msg = ((AuthError)ex.ErrorCode) switch
            {
                AuthError.WrongPassword   => "Incorrect password.",
                AuthError.UserNotFound    => "Account not found.",
                AuthError.InvalidEmail    => "Invalid email.",
                AuthError.TooManyRequests => "Too many attempts. Try again later.",
                _                         => "Could not sign in. Check your connection."
            };
            Debug.LogError($"[PlayerDatabaseAPI] Login failed: {ex.Message}");
            return (false, msg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Login failed: {ex.Message}");
            return (false, "Could not sign in. Check your connection.");
        }
    }

    // ── Forgot password ───────────────────────────────────────────────────

    public static async Task<(bool success, string error)> SendPasswordResetAsync(string username)
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        try
        {
            var usernameDoc = await FirebaseManager.Db
                .Collection(UsernamesCollection).Document(username).GetSnapshotAsync();

            if (!usernameDoc.Exists)
                return (false, "No account found with that username.");

            string uid   = usernameDoc.ToDictionary()["uid"].ToString();
            string email = await GetEmailForUid(uid);

            if (string.IsNullOrEmpty(email))
                return (false, "Could not find account email.");

            await Auth.SendPasswordResetEmailAsync(email);

            Debug.Log($"[PlayerDatabaseAPI] Password reset sent for: {username}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Password reset failed: {ex.Message}");
            return (false, "Could not send reset email. Check your connection.");
        }
    }

    // ── Remember Me / Auto Login ──────────────────────────────────────────

    public static async Task<bool> TryAutoLoginAsync()
    {
        if (!FirebaseManager.IsReady)
            await FirebaseManager.WaitUntilReadyAsync();

        try
        {
            var user = Auth.CurrentUser;
            if (user == null) return false;

            _firebaseUser = user;

            var snapshot = await FirebaseManager.Db
                .Collection(PlayersCollection).Document(user.UserId).GetSnapshotAsync();

            if (!snapshot.Exists) return false;

            _currentPlayer = DictToProfile(user.UserId, snapshot.ToDictionary());
            _loaded        = true;

            PlayerPrefs.SetString("TF_CurrentUser", _currentPlayer.displayName);
            PlayerPrefs.SetInt("TF_IsGuest", 0);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerDatabaseAPI] Auto-login: {_currentPlayer.displayName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Auto-login failed: {ex.Message}");
            return false;
        }
    }

    // ── Delete account ────────────────────────────────────────────────────

    public static async Task DeleteAccountAsync()
    {
        if (!FirebaseManager.IsReady || _firebaseUser == null) return;

        try
        {
            string uid      = _firebaseUser.UserId;
            string username = _currentPlayer?.displayName ?? "";

            // Delete Firestore player document
            await FirebaseManager.Db.Collection(PlayersCollection).Document(uid).DeleteAsync();

            // Delete username mapping
            if (!string.IsNullOrEmpty(username))
                await FirebaseManager.Db.Collection(UsernamesCollection).Document(username).DeleteAsync();

            // Delete Firebase Auth user
            await _firebaseUser.DeleteAsync();

            _currentPlayer = null;
            _firebaseUser  = null;
            _loaded        = false;

            Debug.Log($"[PlayerDatabaseAPI] Account deleted: {username}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] DeleteAccount failed: {ex.Message}");
        }
    }

    public static void SignOut()
    {
        Auth.SignOut();
        _currentPlayer = null;
        _firebaseUser  = null;
        _loaded        = false;
        PlayerPrefs.DeleteKey("TF_CurrentUser");
        PlayerPrefs.SetInt("TF_IsGuest", 0);
        PlayerPrefs.Save();
    }

    // ── Legacy no-op ──────────────────────────────────────────────────────

    public static void Load() { }

    // ── Profile access ────────────────────────────────────────────────────

    public static PlayerProfile GetCurrentPlayer() => _currentPlayer;

    public static PlayerProfile GetOrCreatePlayer(string displayName)
    {
        if (_currentPlayer != null) return _currentPlayer;
        _currentPlayer = new PlayerProfile { playerId = displayName, displayName = displayName };
        return _currentPlayer;
    }

    // ── Save ──────────────────────────────────────────────────────────────

    public static void Save()
    {
        if (_currentPlayer == null || IsGuest || _firebaseUser == null) return;
        _ = PushToFirestoreAsync(_currentPlayer);
    }

    public static void SaveSeenOnly()
    {
        if (_currentPlayer == null || IsGuest || _firebaseUser == null) return;
        _ = PushSeenIdsAsync(_currentPlayer);
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
            _currentPlayer.fastestCrosswordSeconds = crosswordTimeSeconds;

        Save();
    }

    // ── Seen content ──────────────────────────────────────────────────────

    public static List<string> GetSeenTriviaIds()   => _currentPlayer?.seenTriviaIds   ?? new List<string>();
    public static List<string> GetSeenWordokuWords() => _currentPlayer?.seenWordokuWords ?? new List<string>();
    public static List<string> GetSeenCrosswordIds() => _currentPlayer?.seenCrosswordIds ?? new List<string>();

    // ── Leaderboard ───────────────────────────────────────────────────────

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
                var data    = doc.ToDictionary();
                string name = data.ContainsKey("displayName") ? data["displayName"].ToString() : doc.Id;
                long val    = data.ContainsKey(field) ? Convert.ToInt64(data[field]) : 0;
                result.Add((name, val));
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
        if (!FirebaseManager.IsReady || _firebaseUser == null) return;

        try
        {
            var docRef = FirebaseManager.Db.Collection(PlayersCollection).Document(_firebaseUser.UserId);

            var statsDict = new Dictionary<string, object>
            {
                { "playerId",                p.playerId },
                { "displayName",             p.displayName },
                { "email",                   p.email ?? "" },
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

            var seenDict = new Dictionary<string, object>();
            if (p.seenTriviaIds?.Count   > 0) seenDict["seenTriviaIds"]   = FieldValue.ArrayUnion(p.seenTriviaIds.Cast<object>().ToArray());
            if (p.seenWordokuWords?.Count > 0) seenDict["seenWordokuWords"] = FieldValue.ArrayUnion(p.seenWordokuWords.Cast<object>().ToArray());
            if (p.seenCrosswordIds?.Count > 0) seenDict["seenCrosswordIds"] = FieldValue.ArrayUnion(p.seenCrosswordIds.Cast<object>().ToArray());
            if (seenDict.Count > 0) await docRef.UpdateAsync(seenDict);

            Debug.Log($"[PlayerDatabaseAPI] Firestore synced: {p.displayName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Firestore push failed: {ex.Message}");
        }
    }

    private static async Task PushSeenIdsAsync(PlayerProfile p)
    {
        if (!FirebaseManager.IsReady || _firebaseUser == null) return;

        try
        {
            var docRef   = FirebaseManager.Db.Collection(PlayersCollection).Document(_firebaseUser.UserId);
            var seenDict = new Dictionary<string, object>();

            if (p.seenTriviaIds?.Count   > 0) seenDict["seenTriviaIds"]   = FieldValue.ArrayUnion(p.seenTriviaIds.Cast<object>().ToArray());
            if (p.seenWordokuWords?.Count > 0) seenDict["seenWordokuWords"] = FieldValue.ArrayUnion(p.seenWordokuWords.Cast<object>().ToArray());
            if (p.seenCrosswordIds?.Count > 0) seenDict["seenCrosswordIds"] = FieldValue.ArrayUnion(p.seenCrosswordIds.Cast<object>().ToArray());

            if (seenDict.Count > 0) await docRef.UpdateAsync(seenDict);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerDatabaseAPI] Seen IDs push failed: {ex.Message}");
        }
    }

    private static async Task<string> GetEmailForUid(string uid)
    {
        try
        {
            var doc = await FirebaseManager.Db.Collection(PlayersCollection).Document(uid).GetSnapshotAsync();
            if (!doc.Exists) return null;
            var data = doc.ToDictionary();
            return data.ContainsKey("email") ? data["email"].ToString() : null;
        }
        catch { return null; }
    }

    private static Dictionary<string, object> ProfileToDict(PlayerProfile p) =>
        new Dictionary<string, object>
        {
            { "playerId",                p.playerId },
            { "displayName",             p.displayName },
            { "email",                   p.email ?? "" },
            { "totalScore",              p.totalScore },
            { "gamesCompleted",          p.gamesCompleted },
            { "highestScore",            p.highestScore },
            { "highestStreak",           p.highestStreak },
            { "perfectSolves",           p.perfectSolves },
            { "correctAnswers",          p.correctAnswers },
            { "totalAnswers",            p.totalAnswers },
            { "fastestCrosswordSeconds", p.fastestCrosswordSeconds },
            { "seenTriviaIds",           p.seenTriviaIds   ?? new List<string>() },
            { "seenWordokuWords",        p.seenWordokuWords ?? new List<string>() },
            { "seenCrosswordIds",        p.seenCrosswordIds ?? new List<string>() },
            { "lastUpdated",             FieldValue.ServerTimestamp }
        };

    private static PlayerProfile DictToProfile(string uid, Dictionary<string, object> data)
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
            if (data[key] is List<object> raw) return raw.ConvertAll(o => o.ToString());
            return new List<string>();
        }

        return new PlayerProfile
        {
            playerId                = uid,
            displayName             = Get("displayName",             ""),
            email                   = Get("email",                   ""),
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