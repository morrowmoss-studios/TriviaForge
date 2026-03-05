using System;
using System.Collections.Generic;
using UnityEngine;

public static class CrosswordFiller
{
    public static bool TryFillAllSlots(
        string[] layoutRows,
        List<CrosswordEntry> pool,
        out List<CrosswordWord> placedWords,
        int seed = 0,
        int maxSolveAttempts = 40,
        int maxBacktrackNodes = 500000,
        bool allowDuplicates = false,
        int minLen = 3,
        int maxLen = 10)
    {
        placedWords = new List<CrosswordWord>();

        if (layoutRows == null || layoutRows.Length == 0) return false;
        int rows = layoutRows.Length;
        int cols = layoutRows[0].Length;

        bool[,] blocked = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                blocked[r, c] = (layoutRows[r][c] == '#');

        // Compute slots
        List<Slot> slots = ComputeSlots(layoutRows, minLen);
        if (slots.Count == 0) return false;

        // Clean & index pool by length
        var byLen = new Dictionary<int, List<CrosswordEntry>>();
        foreach (var e in pool)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.answer) || string.IsNullOrWhiteSpace(e.clue)) continue;
            string ans = e.answer.Trim().ToUpperInvariant();
            if (ans.Length < minLen || ans.Length > maxLen) continue;

            if (!byLen.TryGetValue(ans.Length, out var list))
            {
                list = new List<CrosswordEntry>();
                byLen[ans.Length] = list;
            }

            list.Add(new CrosswordEntry
            {
                id = e.id,
                answer = ans,
                clue = e.clue.Trim(),
                difficulty = e.difficulty
            });
        }

        // Sort slots: hardest first (most constrained)
        // Start with longer, then those with more intersections potential (more neighbors)
        slots.Sort((a, b) =>
        {
            int c1 = b.length.CompareTo(a.length);
            if (c1 != 0) return c1;

            int n1 = b.intersectionPotential.CompareTo(a.intersectionPotential);
            if (n1 != 0) return n1;

            // Across before down doesn't matter, but keep stable
            return (a.isAcross == b.isAcross) ? 0 : (a.isAcross ? -1 : 1);
        });

        System.Random rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        // Multiple attempts with shuffle to avoid dead ends
        for (int attempt = 0; attempt < maxSolveAttempts; attempt++)
        {
            char[,] grid = new char[rows, cols];
            var used = new HashSet<string>();

            // Slight randomization of candidates within each length list
            var shuffledByLen = new Dictionary<int, List<CrosswordEntry>>();
            foreach (var kv in byLen)
            {
                var copy = new List<CrosswordEntry>(kv.Value);
                Shuffle(copy, rng);
                shuffledByLen[kv.Key] = copy;
            }

            int nodes = 0;
            var result = new List<Placed>();

            if (Backtrack(0))
            {
                placedWords = new List<CrosswordWord>(result.Count);
                int num = 1;
                foreach (var p in result)
                {
                    placedWords.Add(new CrosswordWord
                    {
                        id = num.ToString() + (p.slot.isAcross ? "A" : "D"),
                        isAcross = p.slot.isAcross,
                        startRow = p.slot.startRow,
                        startCol = p.slot.startCol,
                        answer = p.entry.answer,
                        clue = p.entry.clue
                    });
                    num++;
                }
                return true;
            }

            bool Backtrack(int slotIndex)
            {
                if (nodes++ > maxBacktrackNodes) return false;
                if (slotIndex >= slots.Count) return true;

                Slot slot = slots[slotIndex];

                if (!shuffledByLen.TryGetValue(slot.length, out var candidates) || candidates.Count == 0)
                    return false;

                // Build pattern constraints from current grid
                // pattern[i] = existing letter or '\0' if empty
                Span<char> pattern = stackalloc char[slot.length];
                int fixedCount = 0;

                for (int i = 0; i < slot.length; i++)
                {
                    int r = slot.startRow + (slot.isAcross ? 0 : i);
                    int c = slot.startCol + (slot.isAcross ? i : 0);
                    char ch = grid[r, c];
                    pattern[i] = ch;
                    if (ch != '\0') fixedCount++;
                }

                // Choose candidates that match pattern
                // Heuristic: if fixedCount is high, try those first.
                // We iterate in randomized order already.
                for (int i = 0; i < candidates.Count; i++)
                {
                    var entry = candidates[i];

                    if (!allowDuplicates && used.Contains(entry.answer))
                        continue;

                    if (!MatchesPattern(entry.answer, pattern))
                        continue;

                    // Place, recurse, undo
                    var written = Place(grid, blocked, slot, entry.answer);
                    if (written == null) continue; // conflict / blocked / bounds (shouldn't happen)

                    used.Add(entry.answer);
                    result.Add(new Placed { slot = slot, entry = entry });

                    if (Backtrack(slotIndex + 1))
                        return true;

                    // undo
                    result.RemoveAt(result.Count - 1);
                    used.Remove(entry.answer);
                    Unplace(grid, written);
                }

                return false;
            }
        }

        placedWords = new List<CrosswordWord>();
        return false;
    }

    // ----------------- internals -----------------

    private struct Slot
    {
        public bool isAcross;
        public int startRow;
        public int startCol;
        public int length;
        public int intersectionPotential;
    }

    private struct Placed
    {
        public Slot slot;
        public CrosswordEntry entry;
    }

    private static List<Slot> ComputeSlots(string[] layout, int minLen)
    {
        var slots = new List<Slot>();
        int rCount = layout.Length;
        int cCount = layout[0].Length;

        bool IsBlocked(int r, int c)
        {
            if (r < 0 || r >= rCount) return true;
            if (c < 0 || c >= cCount) return true;
            return layout[r][c] == '#';
        }

        // Across
        for (int r = 0; r < rCount; r++)
        {
            int c = 0;
            while (c < cCount)
            {
                while (c < cCount && IsBlocked(r, c)) c++;
                int start = c;
                while (c < cCount && !IsBlocked(r, c)) c++;
                int len = c - start;
                if (len >= minLen)
                {
                    slots.Add(new Slot
                    {
                        isAcross = true,
                        startRow = r,
                        startCol = start,
                        length = len,
                        intersectionPotential = CountIntersectionsPotential(layout, true, r, start, len)
                    });
                }
            }
        }

        // Down
        for (int c = 0; c < cCount; c++)
        {
            int r = 0;
            while (r < rCount)
            {
                while (r < rCount && IsBlocked(r, c)) r++;
                int start = r;
                while (r < rCount && !IsBlocked(r, c)) r++;
                int len = r - start;
                if (len >= minLen)
                {
                    slots.Add(new Slot
                    {
                        isAcross = false,
                        startRow = start,
                        startCol = c,
                        length = len,
                        intersectionPotential = CountIntersectionsPotential(layout, false, start, c, len)
                    });
                }
            }
        }

        return slots;
    }

    private static int CountIntersectionsPotential(string[] layout, bool isAcross, int sr, int sc, int len)
    {
        // crude heuristic: how many cells in this slot have perpendicular open neighbors
        int rows = layout.Length;
        int cols = layout[0].Length;
        int count = 0;

        bool IsOpen(int r, int c)
        {
            if (r < 0 || r >= rows) return false;
            if (c < 0 || c >= cols) return false;
            return layout[r][c] != '#';
        }

        for (int i = 0; i < len; i++)
        {
            int r = sr + (isAcross ? 0 : i);
            int c = sc + (isAcross ? i : 0);

            if (isAcross)
            {
                if (IsOpen(r - 1, c) || IsOpen(r + 1, c)) count++;
            }
            else
            {
                if (IsOpen(r, c - 1) || IsOpen(r, c + 1)) count++;
            }
        }

        return count;
    }

    private static bool MatchesPattern(string word, Span<char> pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = pattern[i];
            if (p != '\0' && p != word[i]) return false;
        }
        return true;
    }

    // returns list of coords written (so we can undo precisely)
    private static List<(int r, int c)> Place(char[,] grid, bool[,] blocked, Slot slot, string word)
    {
        var written = new List<(int r, int c)>(word.Length);

        for (int i = 0; i < word.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);

            if (blocked[r, c]) return null;

            char existing = grid[r, c];
            char incoming = word[i];

            if (existing != '\0' && existing != incoming) return null;

            if (existing == '\0')
            {
                grid[r, c] = incoming;
                written.Add((r, c));
            }
        }

        return written;
    }

    private static void Unplace(char[,] grid, List<(int r, int c)> written)
    {
        for (int i = 0; i < written.Count; i++)
        {
            var p = written[i];
            grid[p.r, p.c] = '\0';
        }
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}