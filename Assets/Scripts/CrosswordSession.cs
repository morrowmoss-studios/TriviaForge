using System.Collections.Generic;

public static class CrosswordSession
{
    public static List<CrosswordWord> currentWords;

    // Cached puzzle so returning from Clues scene doesn't regenerate
    public static PreGeneratedPuzzle activePuzzle;

    public static void ClearPuzzle()
    {
        currentWords  = null;
        activePuzzle  = null;
    }
}