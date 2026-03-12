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

    public int totalScore;
    public int gamesPlayed;
    public int highestScore;

    // Seen content — tracked per player to avoid repeats
    public List<string> seenTriviaIds       = new List<string>(); // TriviaEntry.id
    public List<string> seenWordokuWords    = new List<string>(); // the 9-letter word
    public List<string> seenCrosswordIds    = new List<string>(); // "{categoryId}_{puzzleIndex}"
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