public static class WordokuSession
{
    public static bool hasSavedState   = false;
    public static string savedWord;
    public static char[] savedSolution  = new char[81];
    public static string[] savedBoard   = new string[81];
    public static bool[] savedLocked    = new bool[81];
    public static int savedHints        = 3;
    public static float savedElapsed    = 0f;
    public static int savedWrongPlacements = 0;
    public static bool[] savedWrong = new bool[81];

    public static void Clear()
    {
        hasSavedState = false;
        savedWord     = null;
    }
}