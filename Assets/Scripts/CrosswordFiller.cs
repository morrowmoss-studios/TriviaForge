using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CrosswordFiller
{
    [Serializable]
    public struct Slot
    {
        public bool isAcross;
        public int startRow;
        public int startCol;
        public int length;

        public override string ToString()
            => $"{(isAcross ? "A" : "D")}({startRow},{startCol}) len={length}";
    }

    private class WorkingState
    {
        public int rows;
        public int cols;
        public bool[,] blocked;
        public char[,] grid;                 // '\0' means empty letter
        public Dictionary<int, List<CrosswordEntry>> byLen;
        public HashSet<string> usedAnswers;  // optional dedupe
        public System.Random rng;

        // We keep placements as (slotIndex -> entry)
        public CrosswordEntry[] chosen;
    }

    public static bool TryFillAllSlots(
        string[] layoutRows,
        List<CrosswordEntry> cluePool,
        out List<CrosswordWord> resultWords,
        int seed = 0,
        int maxSolveAttempts = 30,
        int maxBacktrackNodes = 250000,
        bool allowDuplicates = false,
        int minLen = 3,
        int maxLen = 10
    )
    {
        resultWords = new List<CrosswordWord>();

        if (layoutRows == null || layoutRows.Length == 0) return false;

        layoutRows = NormalizeLayout(layoutRows);
        int rows = layoutRows.Length;
        int cols = layoutRows[0].Length;

        bool[,] blocked = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            blocked[r, c] = (layoutRows[r][c] == '#');

        var slots = ComputeSlots(layoutRows)
            .Where(s => s.length >= minLen && s.length <= maxLen)
            .ToList();

        if (slots.Count == 0) return false;

        // Clean + filter pool
        var pool = cluePool
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.answer) && !string.IsNullOrWhiteSpace(e.clue))
            .Select(e => new CrosswordEntry
            {
                id = e.id,
                answer = CleanAnswer(e.answer),
                clue = e.clue.Trim(),
                difficulty = e.difficulty
            })
            .Where(e => e.answer.Length >= minLen && e.answer.Length <= maxLen)
            .Where(e => e.answer.All(ch => ch >= 'A' && ch <= 'Z'))
            .GroupBy(e => e.answer) // distinct by answer
            .Select(g => g.First())
            .ToList();

        // Group by length
        var byLen = new Dictionary<int, List<CrosswordEntry>>();
        foreach (var e in pool)
        {
            if (!byLen.TryGetValue(e.answer.Length, out var list))
            {
                list = new List<CrosswordEntry>();
                byLen[e.answer.Length] = list;
            }
            list.Add(e);
        }

        // Quick fail: if any length has zero candidates, solver can still sometimes work if that slot isn't present.
        // But if a slot's length isn't available at all, it's impossible.
        foreach (var s in slots)
        {
            if (!byLen.ContainsKey(s.length) || byLen[s.length].Count == 0)
                return false;
        }

        // Try multiple solve attempts (randomization changes candidate order)
        for (int attempt = 0; attempt < maxSolveAttempts; attempt++)
        {
            int attemptSeed = (seed == 0) ? Environment.TickCount ^ (attempt * 7919) : seed + attempt * 7919;
            var rng = new System.Random(attemptSeed);

            var st = new WorkingState
            {
                rows = rows,
                cols = cols,
                blocked = blocked,
                grid = new char[rows, cols],
                byLen = byLen,
                usedAnswers = new HashSet<string>(),
                rng = rng,
                chosen = new CrosswordEntry[slots.Count]
            };

            int nodes = 0;

            // Optional: shuffle candidate lists for variety each attempt
            foreach (var kv in st.byLen)
                ShuffleInPlace(kv.Value, st.rng);

            if (SolveRecursive(st, slots, allowDuplicates, ref nodes, maxBacktrackNodes))
            {
                resultWords = BuildCrosswordWordListFromSolution(slots, st.chosen);
                return true;
            }
        }

        return false;
    }

    // -------------------------------
    // Backtracking solver
    // -------------------------------

    private static bool SolveRecursive(
        WorkingState st,
        List<Slot> slots,
        bool allowDuplicates,
        ref int nodes,
        int maxNodes
    )
    {
        if (nodes++ > maxNodes) return false;

        // Choose next unfilled slot using MRV: fewest candidates given current constraints
        int nextIndex = -1;
        List<CrosswordEntry> nextCandidates = null;
        int bestCount = int.MaxValue;

        for (int i = 0; i < slots.Count; i++)
        {
            if (st.chosen[i] != null) continue;

            var s = slots[i];

            // Build pattern constraints for this slot from current grid
            var candidates = GetCandidatesForSlot(st, s, allowDuplicates);
            int count = candidates.Count;

            if (count == 0) return false; // dead end immediately

            if (count < bestCount)
            {
                bestCount = count;
                nextIndex = i;
                nextCandidates = candidates;

                if (bestCount == 1) break; // can't do better
            }
        }

        // All slots filled
        if (nextIndex == -1) return true;

        // Heuristic: try candidates that create more intersections first
        nextCandidates = nextCandidates
            .OrderByDescending(e => IntersectionScore(st, slots[nextIndex], e.answer))
            .ThenBy(_ => st.rng.Next())
            .ToList();

        var slot = slots[nextIndex];

        foreach (var entry in nextCandidates)
        {
            if (!allowDuplicates && st.usedAnswers.Contains(entry.answer))
                continue;

            // Place, track changes so we can undo
            var changes = new List<(int r, int c)>();
            if (!TryPlace(st, slot, entry.answer, changes))
                continue;

            st.chosen[nextIndex] = entry;
            if (!allowDuplicates) st.usedAnswers.Add(entry.answer);

            if (SolveRecursive(st, slots, allowDuplicates, ref nodes, maxNodes))
                return true;

            // Undo
            UndoPlace(st, changes);
            st.chosen[nextIndex] = null;
            if (!allowDuplicates) st.usedAnswers.Remove(entry.answer);
        }

        return false;
    }

    private static List<CrosswordEntry> GetCandidatesForSlot(WorkingState st, Slot slot, bool allowDuplicates)
    {
        if (!st.byLen.TryGetValue(slot.length, out var list))
            return new List<CrosswordEntry>();

        // Build constraints from current grid letters
        // If grid cell is set, candidate must match it
        var candidates = new List<CrosswordEntry>(list.Count);

        foreach (var e in list)
        {
            if (!allowDuplicates && st.usedAnswers.Contains(e.answer))
                continue;

            if (FitsConstraints(st, slot, e.answer))
                candidates.Add(e);
        }

        return candidates;
    }

    private static bool FitsConstraints(WorkingState st, Slot slot, string answer)
    {
        for (int i = 0; i < answer.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);

            if (r < 0 || r >= st.rows || c < 0 || c >= st.cols)
                return false;

            if (st.blocked[r, c])
                return false;

            char existing = st.grid[r, c];
            char incoming = answer[i];

            if (existing != '\0' && existing != incoming)
                return false;
        }

        return true;
    }

    private static bool TryPlace(WorkingState st, Slot slot, string answer, List<(int r, int c)> changes)
    {
        // We already checked constraints, but re-check plus record changes
        for (int i = 0; i < answer.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);

            if (st.blocked[r, c]) return false;

            char existing = st.grid[r, c];
            char incoming = answer[i];

            if (existing != '\0' && existing != incoming)
                return false;

            if (existing == '\0')
            {
                st.grid[r, c] = incoming;
                changes.Add((r, c));
            }
        }

        return true;
    }

    private static void UndoPlace(WorkingState st, List<(int r, int c)> changes)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            var (r, c) = changes[i];
            st.grid[r, c] = '\0';
        }
    }

    private static int IntersectionScore(WorkingState st, Slot slot, string answer)
    {
        int score = 0;
        for (int i = 0; i < answer.Length; i++)
        {
            int r = slot.startRow + (slot.isAcross ? 0 : i);
            int c = slot.startCol + (slot.isAcross ? i : 0);

            if (st.grid[r, c] != '\0')
                score += 3; // already intersecting an existing placed letter

            // Bonus: prefer letters that are likely to intersect later (not perfect, but helps)
            // Middle letters tend to create better grids than edge-only overlaps.
            if (i > 0 && i < answer.Length - 1)
                score += 1;
        }
        return score;
    }

    // -------------------------------
    // Slot computation + helpers
    // -------------------------------

    private static List<Slot> ComputeSlots(string[] layout)
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

                if (len >= 2)
                    slots.Add(new Slot { isAcross = true, startRow = r, startCol = start, length = len });
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

                if (len >= 2)
                    slots.Add(new Slot { isAcross = false, startRow = start, startCol = c, length = len });
            }
        }

        return slots;
    }

    private static List<CrosswordWord> BuildCrosswordWordListFromSolution(List<Slot> slots, CrosswordEntry[] chosen)
    {
        var result = new List<CrosswordWord>();
        int numAcross = 0;
        int numDown = 0;

        // We number in the order slots are listed. If you want classic numbering by grid position, we can do that later.
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var e = chosen[i];
            if (e == null) continue;

            if (s.isAcross) numAcross++;
            else numDown++;

            result.Add(new CrosswordWord
            {
                id = (i + 1).ToString() + (s.isAcross ? "A" : "D"),
                isAcross = s.isAcross,
                startRow = s.startRow,
                startCol = s.startCol,
                answer = e.answer,
                clue = e.clue
            });
        }

        return result;
    }

    private static string[] NormalizeLayout(string[] input)
    {
        if (input == null || input.Length == 0)
            return new[] { ".........." };

        int rCount = input.Length;
        int cCount = 0;
        for (int r = 0; r < rCount; r++)
            cCount = Mathf.Max(cCount, input[r]?.Length ?? 0);

        if (cCount <= 0) cCount = 10;

        var output = new string[rCount];
        for (int r = 0; r < rCount; r++)
        {
            string line = input[r] ?? "";
            if (line.Length < cCount) line = line.PadRight(cCount, '.');
            if (line.Length > cCount) line = line.Substring(0, cCount);
            output[r] = line;
        }
        return output;
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

    private static void ShuffleInPlace<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}