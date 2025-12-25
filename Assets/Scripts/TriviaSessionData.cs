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
    public static string selectedDifficulty = "Medium"; //Default level


    // Question index so we know where we are in the list
    public static int currentQuestionIndex = 0;
}