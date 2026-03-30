using System;
using System.Collections.Generic;

[Serializable]
public class PlayerDatabase
{
    public List<PlayerProfile>  players          = new List<PlayerProfile>();
    public List<HighScoreEntry> globalHighScores = new List<HighScoreEntry>();
}

[Serializable]
public class PlayerProfile
{
    public string playerId;
    public string displayName;
    public string passwordHash;

    // Core stats
    public int totalScore;
    public int gamesPlayed;              // kept for legacy compat
    public int gamesCompleted;           // kept in sync with gamesPlayed
    public int highestScore;
    public int highestStreak;
    public int perfectSolves;

    // Trivia accuracy
    public int correctAnswers;
    public int totalAnswers;

    // Crossword speed (0 = never set)
    public int fastestCrosswordSeconds;

    // Seen content — prevents repeat questions
    public List<string> seenTriviaIds    = new List<string>();
    public List<string> seenWordokuWords = new List<string>();
    public List<string> seenCrosswordIds = new List<string>();

    public float AccuracyRate =>
        totalAnswers > 0 ? (float)correctAnswers / totalAnswers : 0f;
}

[Serializable]
public class HighScoreEntry
{
    public string playerId;
    public string displayName;
    public int    score;
    public string gameMode;
    public string categoryId;
    public string subcategoryId;
    public string timestamp;
}