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
    public string email;

    // Core stats
    public int totalScore;
    public int gamesPlayed;
    public int gamesCompleted;
    public int highestScore;
    public int highestStreak;
    public int perfectSolves;

    // Trivia accuracy
    public int correctAnswers;
    public int totalAnswers;

    // Crossword speed (0 = never set)
    public int fastestCrosswordSeconds;

    // Shame tracking
    public int crosswordBestScarletLetters;  // lowest wrong placements in a crossword (-1 = never set)
    public int wordokuBestScarletLetters;    // lowest wrong placements in a wordoku (-1 = never set)
    public int wordokuFastestSeconds;        // fastest wordoku completion (0 = never set)

    // Avatar
    public int avatarIndex;                  // index into the avatar sprite array (-1 = none chosen)

    // Seen content
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