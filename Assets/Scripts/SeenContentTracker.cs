using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks which trivia questions, wordoku words, and crossword puzzles
/// a logged-in player has already seen. Guests are not tracked.
/// </summary>
public static class SeenContentTracker
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string CurrentUser =>
        PlayerPrefs.GetString("TF_CurrentUser", "");

    private static bool IsGuest =>
        string.IsNullOrEmpty(CurrentUser) || CurrentUser == "Guest";

    private static PlayerProfile GetProfile()
    {
        if (IsGuest) return null;
        PlayerDatabaseAPI.Load();
        return PlayerDatabaseAPI.GetOrCreatePlayer(CurrentUser);
    }

    private static void Save() => PlayerDatabaseAPI.Save();

    // ── TRIVIA ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Filter a list of trivia entries, removing any already seen by this player.
    /// If filtering would leave fewer than minRemaining entries, resets the seen
    /// list for this pool and returns the full list reshuffled.
    /// </summary>
    public static List<TriviaEntry> FilterSeenTrivia(
        List<TriviaEntry> entries, int minRemaining = 10)
    {
        if (IsGuest || entries == null || entries.Count == 0)
            return entries;

        var profile = GetProfile();
        if (profile == null) return entries;

        var unseen = entries.Where(e => !profile.seenTriviaIds.Contains(e.id)).ToList();

        if (unseen.Count < minRemaining)
        {
            // Reset seen list for this pool so the player can go again
            var idsInPool = entries.Select(e => e.id).ToHashSet();
            profile.seenTriviaIds.RemoveAll(id => idsInPool.Contains(id));
            Save();

            Debug.Log("[SeenContentTracker] Trivia pool exhausted — resetting seen list.");
            return entries;
        }

        return unseen;
    }

    /// <summary>Mark a batch of trivia entries as seen after a session.</summary>
    public static void MarkTriviaAsSeen(IEnumerable<TriviaEntry> entries)
    {
        if (IsGuest || entries == null) return;

        var profile = GetProfile();
        if (profile == null) return;

        foreach (var e in entries)
        {
            if (!profile.seenTriviaIds.Contains(e.id))
                profile.seenTriviaIds.Add(e.id);
        }

        Save();
    }

    /// <summary>Mark a single trivia entry as seen.</summary>
    public static void MarkTriviaAsSeen(TriviaEntry entry)
    {
        if (IsGuest || entry == null) return;

        var profile = GetProfile();
        if (profile == null) return;

        if (!profile.seenTriviaIds.Contains(entry.id))
        {
            profile.seenTriviaIds.Add(entry.id);
            Save();
        }
    }

    // ── WORDOKU ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Filter a list of wordoku words, removing already seen ones.
    /// Resets if pool would drop below minRemaining.
    /// </summary>
    public static List<WordokuEntry> FilterSeenWordoku(
        List<WordokuEntry> entries, int minRemaining = 5)
    {
        if (IsGuest || entries == null || entries.Count == 0)
            return entries;

        var profile = GetProfile();
        if (profile == null) return entries;

        var unseen = entries.Where(e => !profile.seenWordokuWords.Contains(e.word)).ToList();

        if (unseen.Count < minRemaining)
        {
            var wordsInPool = entries.Select(e => e.word).ToHashSet();
            profile.seenWordokuWords.RemoveAll(w => wordsInPool.Contains(w));
            Save();

            Debug.Log("[SeenContentTracker] Wordoku pool exhausted — resetting seen list.");
            return entries;
        }

        return unseen;
    }

    /// <summary>Mark a wordoku word as seen.</summary>
    public static void MarkWordokuAsSeen(string word)
    {
        if (IsGuest || string.IsNullOrEmpty(word)) return;

        var profile = GetProfile();
        if (profile == null) return;

        if (!profile.seenWordokuWords.Contains(word))
        {
            profile.seenWordokuWords.Add(word);
            Save();
        }
    }

    // ── CROSSWORD ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pick an unseen crossword puzzle index from a category's puzzle list.
    /// Returns the puzzle and marks it as seen.
    /// Resets if all puzzles in the category have been seen.
    /// </summary>
    public static PreGeneratedPuzzle PickUnseenCrossword(
        string categoryId, List<PreGeneratedPuzzle> puzzles, System.Random rng)
    {
        if (puzzles == null || puzzles.Count == 0) return null;

        if (IsGuest)
        {
            // Guests just get a random puzzle
            return puzzles[rng.Next(puzzles.Count)];
        }

        var profile = GetProfile();
        if (profile == null) return puzzles[rng.Next(puzzles.Count)];

        // Build candidate indices — those not yet seen for this category
        var unseenIndices = Enumerable.Range(0, puzzles.Count)
            .Where(i => !profile.seenCrosswordIds.Contains(CrosswordKey(categoryId, i)))
            .ToList();

        if (unseenIndices.Count == 0)
        {
            // Reset seen puzzles for this category
            profile.seenCrosswordIds.RemoveAll(k => k.StartsWith(categoryId + "_"));
            Save();

            Debug.Log($"[SeenContentTracker] Crossword pool for '{categoryId}' exhausted — resetting.");
            unseenIndices = Enumerable.Range(0, puzzles.Count).ToList();
        }

        int chosenIndex = unseenIndices[rng.Next(unseenIndices.Count)];
        profile.seenCrosswordIds.Add(CrosswordKey(categoryId, chosenIndex));
        Save();

        return puzzles[chosenIndex];
    }

    private static string CrosswordKey(string categoryId, int index) =>
        $"{categoryId}_{index}";
}