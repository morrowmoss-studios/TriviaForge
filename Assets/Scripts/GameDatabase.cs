using System;
using System.Collections.Generic;

[Serializable]
public class GameDatabase
{
    public List<CategoryData> categories;
}

[Serializable]
public class CategoryData
{
    public string id;
    public string name;
    public List<SubcategoryData> subcategories;
}

[Serializable]
public class SubcategoryData
{
    public string id;
    public string name;

    public List<TriviaEntry> trivia;

    // ✅ Wordoku is just 9-letter words + difficulty
    public List<WordokuEntry> wordoku;

    // ✅ Crossword is a clue bank (answer + clue + difficulty)
    public List<CrosswordEntry> crosswords;
}

[Serializable]
public class TriviaEntry
{
    public string id;
    public string questionText;
    public string[] answers;
    public int correctIndex;
    public string difficulty;
}

[Serializable]
public class WordokuEntry
{
    public string id;
    public string word; // 9 letters
}

[Serializable]
public class CrosswordEntry
{
    public string id;
    public string answer;     // e.g., "ION"
    public string clue;       // e.g., "Atom with a net electric charge"
    public string difficulty; // "easy" | "medium" | "hard" | "insanity"
}