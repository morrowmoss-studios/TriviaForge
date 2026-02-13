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
    public List<WordokuEntry> wordoku;
    public List<CrosswordPuzzleEntry> crosswords;
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
    public string clue;
    public string hint;
}

[Serializable]
public class CrosswordPuzzleEntry
{
    public string id;
    public int rows;
    public int cols;
    public string[] layoutRows;
    public List<CrosswordWordEntry> words;
}

[Serializable]
public class CrosswordWordEntry
{
    public string id;
    public bool isAcross;
    public int startRow;
    public int startCol;
    public string answer;
    public string clue;
}