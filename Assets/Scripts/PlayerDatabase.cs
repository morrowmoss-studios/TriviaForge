using System;
using System.Collections.Generic;

[Serializable]
public class PlayerDatabase
{
    public List<PlayerProfile> players = new List<PlayerProfile>();
    public List<HighScoreEntry> globalHighScores = new List<HighScoreEntry>();
}

[Serializable]
public class PlayerProfile
{
    public string playerId;      // unique ID (could be a GUID)
    public string displayName;   // username

    // You can extend this later:
    public int totalScore;
    public int gamesPlayed;
    public int highestScore;
}

[Serializable]
public class HighScoreEntry
{
    public string playerId;      // link back to PlayerProfile
    public string displayName;   // cache name for display
    public int score;
    public string gameMode;      // "Trivia", "Crossword", "Wordoku"
    public string categoryId;    // e.g. "science"
    public string subcategoryId; // e.g. "physics_quantum"
    public string timestamp;     // ISO string if you want: DateTime.UtcNow.ToString("o")
}