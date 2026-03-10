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

        // '.' playable, '#' blocked
        public string[] layoutRows;

        // Placed words with coordinates + direction
        public List<CrosswordWord> placedWords;
    }

    // Internal working board: '\0' empty, otherwise letter
    private class Board
    {
        public int Rows;
        public int Cols;

        public char[,] Letters;     // placed letters
        public bool[,] Blocked;     // true = '#'

        public Board(int rows, int cols, bool[,] blocked)
        {
            Rows = rows;
            Cols = cols;

            Letters = new char[rows, cols];
            Blocked = blocked ?? new bool[rows, cols];
        }

        public bool InBounds(int r, int c) => r >= 0 && r < Rows && c >= 0 && c < Cols;

        public bool IsBlocked(int r, int c) => Blocked[r, c];

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
    /// Generates a crossword by:
    /// 1) making a classic-ish symmetric block mask (#/.)
    /// 2) placing words onto '.' cells, trying to intersect
    /// </summary>
    public static GeneratedCrossword GenerateFromClueBank(
        List<CrosswordEntry> clueBank,
        int rows,
        int cols,
        int targetWordCount,
        int seed = 0,
        int maxAttempts = 2500,
        int minWordLen = 3,
        int maxWordLen = 10,

        // --- NEW: layout controls ---
        float blockPercent = 0.18f,          // 0.12–0.22 usually feels “classic”
        int layoutGenAttempts = 200,         // tries to find a good mask
        bool forceCenterOpen = true          // many crosswords keep center open
    )
    {
        if (clueBank == null) clueBank = new List<CrosswordEntry>();

        // Clean + filter clue bank (answer/clue required)
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
            .DistinctBy(e => e.answer)
            .ToList();

        var rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        // Shuffle by length descending (helps intersections)
        pool = pool.OrderByDescending(e => e.answer.Length).ThenBy(_ => rng.Next()).ToList();

        // 1) Generate a symmetric, connected layout
        bool[,] blocked = GenerateClassicSymmetricMask(rows, cols, blockPercent, rng, layoutGenAttempts, forceCenterOpen);
        string[] layoutRows = MaskToLayoutRows(blocked);

        // 2) Create board with that mask
        var board = new Board(rows, cols, blocked);
        var placed = new List<(CrosswordEntry entry, Placement placement)>();

        if (pool.Count == 0)
            return BuildResult(board, placed, layoutRows);

        // Place first word (longest) roughly centered, across
        var first = pool[0];
        if (!TryPlaceFirst(board, first.answer, rng, out var firstPlacement))
        {
            // If first word fails, return empty puzzle (still returns layout)
            return BuildResult(board, placed, layoutRows);
        }

        ApplyPlacement(board, first.answer, firstPlacement);
        placed.Add((first, firstPlacement));

        var used = new HashSet<string> { first.answer };

        int attempts = 0;

        while (placed.Count < targetWordCount && attempts < maxAttempts)
        {
            attempts++;

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
                // Occasionally place non-intersecting word early so we don't stall
                if (placed.Count < 3 && TryPlaceNonIntersecting(board, candidate.answer, rng, out var placement2))
                {
                    ApplyPlacement(board, candidate.answer, placement2);
                    placed.Add((candidate, placement2));
                    used.Add(candidate.answer);
                }
            }
        }

        var crosswordWords = BuildCrosswordWordList(placed);

        return new GeneratedCrossword
        {
            rows = rows,
            cols = cols,
            layoutRows = layoutRows,
            placedWords = crosswordWords
        };
    }

    // -------------------------------
    // Public layout-only entry point
    // -------------------------------

    /// <summary>
    /// Generates only the block mask as string[] rows of '.' and '#'.
    /// Pass the result to CrosswordFiller.TryFillAllSlots to guarantee every
    /// letter run between black squares is a real word in both directions.
    /// </summary>
    public static string[] GenerateLayout(
        int rows              = 10,
        int cols              = 10,
        int seed              = 0,
        float blockPercent    = 0.16f,
        int layoutGenAttempts = 300,
        bool forceCenterOpen  = true
    )
    {
        var rng = (seed == 0) ? new System.Random() : new System.Random(seed);
        bool[,] blocked = GenerateClassicSymmetricMask(
            rows, cols, blockPercent, rng, layoutGenAttempts, forceCenterOpen);
        return MaskToLayoutRows(blocked);
    }

    // -------------------------------
    // Layout generation (classic-ish)
    // -------------------------------

    private static bool[,] GenerateClassicSymmetricMask(
        int rows,
        int cols,
        float blockPercent,
        System.Random rng,
        int attempts,
        bool forceCenterOpen
    )
    {
        blockPercent = Mathf.Clamp(blockPercent, 0.05f, 0.40f);

        int total = rows * cols;
        int targetBlocks = Mathf.RoundToInt(total * blockPercent);

        // We'll generate only on half the grid and mirror (180° rotational symmetry)
        // Each decision affects 2 cells (or 1 if it's the center cell of odd grid).
        for (int tryIndex = 0; tryIndex < attempts; tryIndex++)
        {
            bool[,] blocked = new bool[rows, cols];

            // Optionally force center open
            if (forceCenterOpen && rows % 2 == 1 && cols % 2 == 1)
            {
                blocked[rows / 2, cols / 2] = false;
            }

            int blocksPlaced = 0;

            // Build list of “unique” positions under 180° symmetry
            var uniqueCells = new List<(int r, int c)>();
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int rr = rows - 1 - r;
                int cc = cols - 1 - c;

                // only take one representative from each symmetric pair
                if (r < rr || (r == rr && c <= cc))
                    uniqueCells.Add((r, c));
            }

            // Shuffle them
            uniqueCells = uniqueCells.OrderBy(_ => rng.Next()).ToList();

            foreach (var cell in uniqueCells)
            {
                if (blocksPlaced >= targetBlocks) break;

                int r = cell.r;
                int c = cell.c;
                int rr = rows - 1 - r;
                int cc = cols - 1 - c;

                // If we force center open, skip blocking it
                if (forceCenterOpen && rows % 2 == 1 && cols % 2 == 1)
                {
                    int cr = rows / 2;
                    int cc2 = cols / 2;
                    if ((r == cr && c == cc2) || (rr == cr && cc == cc2))
                        continue;
                }

                // Decide to block with some bias until we hit target
                // (More likely early, less likely as we approach target)
                float t = (float)blocksPlaced / Mathf.Max(1, targetBlocks);
                float prob = Mathf.Lerp(0.65f, 0.20f, t);

                if (rng.NextDouble() > prob)
                    continue;

                // Apply symmetric blocking
                int add = (r == rr && c == cc) ? 1 : 2;

                // Don't overshoot too wildly
                if (blocksPlaced + add > targetBlocks + 1) continue;

                blocked[r, c] = true;
                blocked[rr, cc] = true;
                blocksPlaced += add;
            }

            // Make sure open cells are connected (single region)
            if (!IsOpenAreaConnected(blocked, rows, cols))
                continue;

            // Optional: prevent “too open” or “too blocked” by recalculating actual percent
            // (just in case symmetry rounding made it drift)
            int actualBlocks = CountBlocks(blocked, rows, cols);
            float actualPct = (float)actualBlocks / total;

            if (Mathf.Abs(actualPct - blockPercent) > 0.06f)
                continue;

            return blocked;
        }

        // Fallback: no blocks
        return new bool[rows, cols];
    }

    private static int CountBlocks(bool[,] blocked, int rows, int cols)
    {
        int count = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            if (blocked[r, c]) count++;
        return count;
    }

    private static bool IsOpenAreaConnected(bool[,] blocked, int rows, int cols)
    {
        // Find first open cell
        (int r, int c) start = (-1, -1);
        int openCount = 0;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (!blocked[r, c])
            {
                openCount++;
                if (start.r == -1) start = (r, c);
            }
        }

        // If everything is blocked (shouldn't happen), treat as invalid
        if (openCount == 0) return false;

        // Flood fill
        var visited = new bool[rows, cols];
        var q = new Queue<(int r, int c)>();
        q.Enqueue(start);
        visited[start.r, start.c] = true;

        int reached = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            reached++;

            void TryEnq(int rr, int cc)
            {
                if (rr < 0 || rr >= rows || cc < 0 || cc >= cols) return;
                if (visited[rr, cc]) return;
                if (blocked[rr, cc]) return;
                visited[rr, cc] = true;
                q.Enqueue((rr, cc));
            }

            TryEnq(cur.r - 1, cur.c);
            TryEnq(cur.r + 1, cur.c);
            TryEnq(cur.r, cur.c - 1);
            TryEnq(cur.r, cur.c + 1);
        }

        return reached == openCount;
    }

    private static string[] MaskToLayoutRows(bool[,] blocked)
    {
        int rows = blocked.GetLength(0);
        int cols = blocked.GetLength(1);

        var outRows = new string[rows];
        for (int r = 0; r < rows; r++)
        {
            char[] line = new char[cols];
            for (int c = 0; c < cols; c++)
                line[c] = blocked[r, c] ? '#' : '.';

            outRows[r] = new string(line);
        }
        return outRows;
    }

    // -------------------------------
    // Placement logic
    // -------------------------------

    private static bool TryPlaceFirst(Board board, string word, System.Random rng, out Placement placement)
    {
        placement = default;

        // Across, centered row (but must fit + not hit blocks)
        int row = board.Rows / 2;

        int maxStart = board.Cols - word.Length;
        if (maxStart < 0) return false;

        // Try a few nearby starts around center
        int centerStart = Mathf.Clamp(board.Cols / 2 - word.Length / 2, 0, maxStart);
        var starts = new List<int> { centerStart };

        // add small jitter options
        for (int d = 1; d <= 4; d++)
        {
            if (centerStart - d >= 0) starts.Add(centerStart - d);
            if (centerStart + d <= maxStart) starts.Add(centerStart + d);
        }

        starts = starts.OrderBy(_ => rng.Next()).ToList();

        foreach (int col in starts)
        {
            var p = new Placement { isAcross = true, startRow = row, startCol = col };
            if (CanPlace(board, word, p))
            {
                placement = p;
                return true;
            }
        }

        // If center row fails due to blocks, try any row
        for (int k = 0; k < 40; k++)
        {
            int r = rng.Next(0, board.Rows);
            int c = rng.Next(0, maxStart + 1);
            var p = new Placement { isAcross = true, startRow = r, startCol = c };
            if (CanPlace(board, word, p))
            {
                placement = p;
                return true;
            }
        }

        return false;
    }

    private static bool TryFindIntersectingPlacement(Board board, string word, System.Random rng, out Placement placement)
    {
        placement = default;

        // Existing letters on board
        var existing = new List<(int r, int c, char ch)>();
        for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Cols; c++)
        {
            char ch = board.Get(r, c);
            if (ch != '\0') existing.Add((r, c, ch));
        }

        if (existing.Count == 0) return false;

        existing = existing.OrderBy(_ => rng.Next()).ToList();
        var indices = Enumerable.Range(0, word.Length).OrderBy(_ => rng.Next()).ToList();

        foreach (int i in indices)
        {
            char target = word[i];

            foreach (var cell in existing)
            {
                if (cell.ch != target) continue;

                // across
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

                // down
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

        for (int k = 0; k < 120; k++)
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

        // Block + letter conflicts
        for (int i = 0; i < word.Length; i++)
        {
            int r = placement.startRow + (placement.isAcross ? 0 : i);
            int c = placement.startCol + (placement.isAcross ? i : 0);

            if (board.IsBlocked(r, c))
                return false;

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

    private static GeneratedCrossword BuildResult(Board board, List<(CrosswordEntry entry, Placement placement)> placed, string[] layoutRows)
    {
        return new GeneratedCrossword
        {
            rows = board.Rows,
            cols = board.Cols,
            layoutRows = layoutRows,
            placedWords = BuildCrosswordWordList(placed)
        };
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

// Tiny helper for DistinctBy (Unity-safe)
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