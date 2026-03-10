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

    // Pre-generated crossword puzzles for this category
    public List<PreGeneratedPuzzle> puzzles;
}

[Serializable]
public class PreGeneratedPuzzle
{
    public List<string> layoutRows;
    public List<PlacedWord> placedWords;
}

[Serializable]
public class PlacedWord
{
    public string id;
    public bool isAcross;
    public int startRow;
    public int startCol;
    public string answer;
    public string clue;
}

[Serializable]
public class SubcategoryData
{
    public string id;
    public string name;

    public List<TriviaEntry> trivia;
    public List<WordokuEntry> wordoku;
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
    public string word;
}

[Serializable]
public class CrosswordEntry
{
    public string id;
    public string answer;
    public string clue;
    public string difficulty;
}