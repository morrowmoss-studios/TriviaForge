using System.Collections.Generic;

public static class CrosswordSession
{
    public static List<CrosswordWord> currentWords;

    // Cached puzzle so returning from Clues scene doesn't regenerate
    public static PreGeneratedPuzzle activePuzzle;

    // Saved player progress: key = "row,col", value = letter char
    public static Dictionary<string, char> savedGridState;
    public static int savedWrongPlacements;
    public static int savedHintsRemaining = 3;

    public static void ClearPuzzle()
    {
        currentWords         = null;
        activePuzzle         = null;
        savedGridState       = null;
        savedWrongPlacements = 0;
        savedHintsRemaining  = 3;
    }
}