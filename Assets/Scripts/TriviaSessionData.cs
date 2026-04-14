public static class TriviaSessionData
{
    // What happened on the last question
    public static string questionText;
    public static string[] answers = new string[4];
    public static int correctIndex;
    public static int chosenIndex;
    public static bool wasCorrect;
    public static int totalQuestions;
    public static string selectedGameMode;
    public static string selectedCategory;
    public static string selectedSubcategory;
    public static string selectedCategoryId;
    public static string selectedSubcategoryId;
    public static string selectedDifficulty = "Medium";
    public static int strikes    = 0;
    public const  int maxStrikes = 3;
    public static bool roundOver = false;
    // Saved timer value when navigating away mid-question (e.g. to Settings)
    public static float savedTimeRemaining = -1f;

    // Question index so we know where we are in the list
    public static int currentQuestionIndex = 0;

    // Persisted shuffled question list — survives scene reloads within a session
    // Stores just the DB entry ids so we can reconstruct Questions without
    // holding full objects across scenes
    public static System.Collections.Generic.List<TriviaEntry> sessionQuestions = null;

    // Wordoku end-of-game stats
    public static float wordokuTimeSeconds     = 0f;
    public static int   wordokuWrongPlacements = 0;

    // Crossword end-of-game stats
    public static int  crosswordWrongPlacements = 0;
    public static bool crosswordPerfectGame     = false;

    public static void ClearSession()
    {
        currentQuestionIndex        = 0;
        strikes                     = 0;
        roundOver                   = false;
        sessionQuestions            = null;
        wordokuTimeSeconds          = 0f;
        wordokuWrongPlacements      = 0;
        crosswordWrongPlacements    = 0;
        crosswordPerfectGame        = false;
    }
}