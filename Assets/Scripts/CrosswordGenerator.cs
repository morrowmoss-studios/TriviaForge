using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CrosswordGenerator
{
    [Serializable]
    public class GeneratedCrossword
    {
        public int rows;
        public int cols;

        // '.' playable, '#' blocked (v0 uses NO blocks: all '.')
        public string[] layoutRows;

        // Placed words with coordinates + direction
        public List<CrosswordWord> placedWords;
    }

    // Internal working board: '\0' empty, otherwise letter
    private class Board
    {
        public int Rows;
        public int Cols;
        public char[,] Letters;

        public Board(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Letters = new char[rows, cols];
        }

        public bool InBounds(int r, int c) => r >= 0 && r < Rows && c >= 0 && c < Cols;

        public char Get(int r, int c) => Letters[r, c];
        public void Set(int r, int c, char ch) => Letters[r, c] = ch;

        public bool IsEmpty(int r, int c) => Letters[r, c] == '\0';
    }

    private struct Placement
    {
        public bool isAcross;
        public int startRow;
        public int startCol;
    }

    /// <summary>
    /// Generates a crossword by placing words into a blank grid.
    /// v0 rules: all cells are playable (no '#'), tries to intersect words, minimal constraints.
    /// </summary>
    public static GeneratedCrossword GenerateFromClueBank(
        List<CrosswordEntry> clueBank,
        int rows,
        int cols,
        int targetWordCount,
        int seed = 0,
        int maxAttempts = 2000,
        int minWordLen = 3,
        int maxWordLen = 10
    )
    {
        if (clueBank == null) clueBank = new List<CrosswordEntry>();

        // Clean + filter
        var pool = clueBank
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.answer) && !string.IsNullOrWhiteSpace(e.clue))
            .Select(e => new CrosswordEntry
            {
                id = e.id,
                answer = CleanAnswer(e.answer),
                clue = e.clue.Trim(),
                difficulty = string.IsNullOrWhiteSpace(e.difficulty) ? "medium" : e.difficulty.Trim().ToLowerInvariant()
            })
            .Where(e => e.answer.Length >= minWordLen && e.answer.Length <= maxWordLen)
            .Where(e => e.answer.All(ch => ch >= 'A' && ch <= 'Z'))
            .DistinctBy(e => e.answer) // avoid duplicate answers in pool
            .ToList();

        var rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        // Shuffle by length descending (helps get intersections)
        pool = pool.OrderByDescending(e => e.answer.Length).ThenBy(_ => rng.Next()).ToList();

        var board = new Board(rows, cols);
        var placed = new List<(CrosswordEntry entry, Placement placement)>();

        if (pool.Count == 0)
        {
            return BuildResult(board, placed);
        }

        // Place first word (longest) roughly centered, across
        var first = pool[0];
        if (!TryPlaceFirst(board, first.answer, rng, out var firstPlacement))
        {
            // If even first word fails, return empty
            return BuildResult(board, placed);
        }
        ApplyPlacement(board, first.answer, firstPlacement);
        placed.Add((first, firstPlacement));

        var used = new HashSet<string> { first.answer };

        int attempts = 0;
        int safety = maxAttempts;

        while (placed.Count < targetWordCount && attempts < safety)
        {
            attempts++;

            // Pick a candidate not used yet
            var candidate = pool[rng.Next(pool.Count)];
            if (used.Contains(candidate.answer)) continue;

            if (TryFindIntersectingPlacement(board, candidate.answer, rng, out var placement))
            {
                ApplyPlacement(board, candidate.answer, placement);
                placed.Add((candidate, placement));
                used.Add(candidate.answer);
            }
            else
            {
                // Occasionally place non-intersecting word to keep progress
                if (placed.Count < 3 && TryPlaceNonIntersecting(board, candidate.answer, rng, out var placement2))
                {
                    ApplyPlacement(board, candidate.answer, placement2);
                    placed.Add((candidate, placement2));
                    used.Add(candidate.answer);
                }
            }
        }

        // Convert to CrosswordWord list + give them ids (1A/2D style-ish)
        var crosswordWords = BuildCrosswordWordList(placed);

        return new GeneratedCrossword
        {
            rows = rows,
            cols = cols,
            layoutRows = MakeAllPlayableLayout(rows, cols),
            placedWords = crosswordWords
        };
    }

    // -------------------------------
    // Placement logic
    // -------------------------------

    private static bool TryPlaceFirst(Board board, string word, System.Random rng, out Placement placement)
    {
        placement = default;

        // Across, centered row
        int row = board.Rows / 2;
        int maxStart = board.Cols - word.Length;
        if (maxStart < 0) return false;

        int col = Mathf.Clamp(board.Cols / 2 - word.Length / 2, 0, maxStart);

        placement = new Placement { isAcross = true, startRow = row, startCol = col };

        return CanPlace(board, word, placement);
    }

    private static bool TryFindIntersectingPlacement(Board board, string word, System.Random rng, out Placement placement)
    {
        placement = default;

        // Build list of all existing letters on board
        var existing = new List<(int r, int c, char ch)>();
        for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Cols; c++)
        {
            char ch = board.Get(r, c);
            if (ch != '\0') existing.Add((r, c, ch));
        }

        if (existing.Count == 0) return false;

        // Shuffle existing letters for randomness
        existing = existing.OrderBy(_ => rng.Next()).ToList();

        // For each letter in the candidate word, try to match a board letter
        var indices = Enumerable.Range(0, word.Length).OrderBy(_ => rng.Next()).ToList();

        foreach (int i in indices)
        {
            char target = word[i];

            foreach (var cell in existing)
            {
                if (cell.ch != target) continue;

                // Try place across (so intersection is on i)
                // If across: startCol = cell.c - i
                var across = new Placement
                {
                    isAcross = true,
                    startRow = cell.r,
                    startCol = cell.c - i
                };

                if (CanPlace(board, word, across))
                {
                    placement = across;
                    return true;
                }

                // Try place down
                var down = new Placement
                {
                    isAcross = false,
                    startRow = cell.r - i,
                    startCol = cell.c
                };

                if (CanPlace(board, word, down))
                {
                    placement = down;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryPlaceNonIntersecting(Board board, string word, System.Random rng, out Placement placement)
    {
        placement = default;

        // Try a handful of random positions
        for (int k = 0; k < 80; k++)
        {
            bool isAcross = rng.NextDouble() < 0.5;

            int maxRow = isAcross ? board.Rows - 1 : board.Rows - word.Length;
            int maxCol = isAcross ? board.Cols - word.Length : board.Cols - 1;

            if (maxRow < 0 || maxCol < 0) return false;

            int r = rng.Next(0, maxRow + 1);
            int c = rng.Next(0, maxCol + 1);

            var p = new Placement { isAcross = isAcross, startRow = r, startCol = c };
            if (CanPlace(board, word, p) && !IntersectsAnything(board, word, p))
            {
                placement = p;
                return true;
            }
        }

        return false;
    }

    private static bool IntersectsAnything(Board board, string word, Placement placement)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int r = placement.startRow + (placement.isAcross ? 0 : i);
            int c = placement.startCol + (placement.isAcross ? i : 0);
            if (!board.InBounds(r, c)) return true;

            if (!board.IsEmpty(r, c))
                return true;
        }
        return false;
    }

    private static bool CanPlace(Board board, string word, Placement placement)
    {
        // Bounds
        int endRow = placement.startRow + (placement.isAcross ? 0 : (word.Length - 1));
        int endCol = placement.startCol + (placement.isAcross ? (word.Length - 1) : 0);

        if (!board.InBounds(placement.startRow, placement.startCol)) return false;
        if (!board.InBounds(endRow, endCol)) return false;

        // Letter conflicts
        for (int i = 0; i < word.Length; i++)
        {
            int r = placement.startRow + (placement.isAcross ? 0 : i);
            int c = placement.startCol + (placement.isAcross ? i : 0);

            char existing = board.Get(r, c);
            char incoming = word[i];

            if (existing != '\0' && existing != incoming)
                return false;
        }

        return true;
    }

    private static void ApplyPlacement(Board board, string word, Placement placement)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int r = placement.startRow + (placement.isAcross ? 0 : i);
            int c = placement.startCol + (placement.isAcross ? i : 0);
            board.Set(r, c, word[i]);
        }
    }

    // -------------------------------
    // Output formatting
    // -------------------------------

    private static GeneratedCrossword BuildResult(Board board, List<(CrosswordEntry entry, Placement placement)> placed)
    {
        return new GeneratedCrossword
        {
            rows = board.Rows,
            cols = board.Cols,
            layoutRows = MakeAllPlayableLayout(board.Rows, board.Cols),
            placedWords = BuildCrosswordWordList(placed)
        };
    }

    private static string[] MakeAllPlayableLayout(int rows, int cols)
    {
        var arr = new string[rows];
        string line = new string('.', cols);
        for (int r = 0; r < rows; r++) arr[r] = line;
        return arr;
    }

    private static List<CrosswordWord> BuildCrosswordWordList(List<(CrosswordEntry entry, Placement placement)> placed)
    {
        var list = new List<CrosswordWord>();
        int num = 1;

        foreach (var p in placed)
        {
            list.Add(new CrosswordWord
            {
                id = num.ToString() + (p.placement.isAcross ? "A" : "D"),
                isAcross = p.placement.isAcross,
                startRow = p.placement.startRow,
                startCol = p.placement.startCol,
                answer = p.entry.answer,
                clue = p.entry.clue
            });
            num++;
        }

        return list;
    }

    private static string CleanAnswer(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        raw = raw.Trim().ToUpperInvariant();

        var chars = new List<char>(raw.Length);
        foreach (char ch in raw)
        {
            if (ch >= 'A' && ch <= 'Z')
                chars.Add(ch);
        }
        return new string(chars.ToArray());
    }
}

// Tiny helper for DistinctBy without LINQ hell (Unity safe)
public static class LinqExtras
{
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> src, Func<T, TKey> key)
    {
        var set = new HashSet<TKey>();
        foreach (var item in src)
        {
            if (set.Add(key(item)))
                yield return item;
        }
    }
}