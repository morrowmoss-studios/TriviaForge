using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Word-first crossword generator.
///
/// Instead of building a fixed layout and trying to fill it (which fails when
/// the word pool doesn't match slot lengths), this generator:
///   1. Places words one at a time onto an unbounded coordinate space.
///   2. Each new word MUST intersect an already-placed word at exactly one shared letter.
///   3. Parallel-adjacency is forbidden (no accidental touching words).
///   4. After placing, the bounding box is checked — if it exceeds the grid it is undone.
///   5. Once enough words are placed, the used cells become the layout and '#' fills the rest.
///
/// This guarantees the puzzle is always solvable because we build the answer first,
/// then derive the layout from it — rather than fighting a pre-made layout.
/// </summary>
public static class CrosswordGenerator
{
    // ── public result type ───────────────────────────────────────────────────

    [Serializable]
    public class GeneratedCrossword
    {
        public int rows;
        public int cols;
        public string[] layoutRows;       // '#' = blocked, '.' = playable
        public List<CrosswordWord> placedWords;
    }

    // ── internal types ───────────────────────────────────────────────────────

    private class PlacedWord
    {
        public CrosswordEntry Entry;
        public int Row;
        public int Col;
        public bool IsAcross;
    }

    private struct PlacementOption
    {
        public bool IsAcross;
        public int Row;
        public int Col;
        public int Shared;
    }

    // ── public entry point ───────────────────────────────────────────────────

    public static GeneratedCrossword Generate(
        List<CrosswordEntry> pool,
        int rows              = 10,
        int cols              = 10,
        int targetWords       = 18,
        int seed              = 0,
        int maxAttempts       = 40,
        int maxIterPerAttempt = 6000)
    {
        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("[CrosswordGenerator] Pool is empty.");
            return EmptyResult(rows, cols);
        }

        var clean = CleanPool(pool);
        if (clean.Count == 0)
        {
            Debug.LogError("[CrosswordGenerator] Pool is empty after cleaning.");
            return EmptyResult(rows, cols);
        }

        var rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Shuffle(clean, rng);

            var result = TryGenerate(clean, rows, cols, targetWords, rng, maxIterPerAttempt);
            if (result != null && result.placedWords.Count >= Mathf.Max(8, targetWords - 4))
            {
                Debug.Log("[CrosswordGenerator] Success on attempt " + (attempt + 1) +
                          " with " + result.placedWords.Count + " words.");
                return result;
            }
        }

        Debug.LogError("[CrosswordGenerator] All " + maxAttempts + " attempts failed. " +
                       "Add more words to the pool (especially 3-6 letter words).");
        return EmptyResult(rows, cols);
    }

    // ── core single-attempt generator ────────────────────────────────────────

    private static GeneratedCrossword TryGenerate(
        List<CrosswordEntry> pool,
        int rows, int cols,
        int target,
        System.Random rng,
        int maxIter)
    {
        // Vector2Int: x = col, y = row — Unity-native type, no named-tuple issues
        var letters     = new Dictionary<Vector2Int, char>();
        var acrossCells = new HashSet<Vector2Int>();
        var downCells   = new HashSet<Vector2Int>();

        var placed    = new List<PlacedWord>();
        var usedWords = new HashSet<string>();

        // ── first word ───────────────────────────────────────────────────────
        var starters = pool.FindAll(e => e.answer.Length >= 5 && e.answer.Length <= 8);
        if (starters.Count == 0) starters = pool;

        var first = starters[rng.Next(starters.Count)];
        // Randomise starting direction so puzzles grow in different orientations
        bool firstAcross = (rng.Next(2) == 0);
        PlaceWord(letters, acrossCells, downCells, first.answer, 0, 0, firstAcross);
        placed.Add(new PlacedWord { Entry = first, Row = 0, Col = 0, IsAcross = firstAcross });
        usedWords.Add(first.answer);

        // ── grow the puzzle ───────────────────────────────────────────────────
        for (int iter = 0; iter < maxIter && placed.Count < target; iter++)
        {
            var candidate = pool[rng.Next(pool.Count)];
            if (usedWords.Contains(candidate.answer)) continue;

            var options = FindPlacements(letters, acrossCells, downCells,
                                         candidate.answer, rows, cols);
            if (options.Count == 0) continue;

            options.Sort((a, b) => b.Shared.CompareTo(a.Shared));

            foreach (var opt in options)
            {
                PlaceWord(letters, acrossCells, downCells,
                          candidate.answer, opt.Row, opt.Col, opt.IsAcross);

                if (BoundingBoxFits(letters, rows, cols))
                {
                    placed.Add(new PlacedWord
                    {
                        Entry    = candidate,
                        Row      = opt.Row,
                        Col      = opt.Col,
                        IsAcross = opt.IsAcross
                    });
                    usedWords.Add(candidate.answer);
                    break;
                }
                else
                {
                    UndoPlace(letters, acrossCells, downCells,
                              candidate.answer, opt.Row, opt.Col, opt.IsAcross);
                }
            }
        }

        if (placed.Count < 6) return null;

        // ── normalize to top-left = (0,0) ─────────────────────────────────────
        int minR = int.MaxValue, minC = int.MaxValue;
        foreach (var kv in letters)
        {
            if (kv.Key.y < minR) minR = kv.Key.y;
            if (kv.Key.x < minC) minC = kv.Key.x;
        }

        char[,] grid = new char[rows, cols];
        foreach (var kv in letters)
        {
            int r = kv.Key.y - minR;
            int c = kv.Key.x - minC;
            if (r >= 0 && r < rows && c >= 0 && c < cols)
                grid[r, c] = kv.Value;
        }

        var layoutRows = new string[rows];
        for (int r = 0; r < rows; r++)
        {
            var sb = new System.Text.StringBuilder(cols);
            for (int c = 0; c < cols; c++)
                sb.Append(grid[r, c] == '\0' ? '#' : '.');
            layoutRows[r] = sb.ToString();
        }

        var wordList = new List<CrosswordWord>();
        int num = 1;
        foreach (var pw in placed)
        {
            wordList.Add(new CrosswordWord
            {
                id       = num + (pw.IsAcross ? "A" : "D"),
                isAcross = pw.IsAcross,
                startRow = pw.Row - minR,
                startCol = pw.Col - minC,
                answer   = pw.Entry.answer,
                clue     = pw.Entry.clue
            });
            num++;
        }

        return new GeneratedCrossword
        {
            rows        = rows,
            cols        = cols,
            layoutRows  = layoutRows,
            placedWords = wordList
        };
    }

    // ── placement logic ───────────────────────────────────────────────────────

    private static List<PlacementOption> FindPlacements(
        Dictionary<Vector2Int, char> letters,
        HashSet<Vector2Int> acrossCells,
        HashSet<Vector2Int> downCells,
        string word,
        int rows, int cols)
    {
        var results = new List<PlacementOption>();

        for (int i = 0; i < word.Length; i++)
        {
            char ch = word[i];

            foreach (var kv in letters)
            {
                if (kv.Value != ch) continue;

                int gr = kv.Key.y;   // row
                int gc = kv.Key.x;   // col

                // Across: word[i] aligns with (gr, gc)
                int sr = gr;
                int sc = gc - i;
                if (CanPlace(letters, acrossCells, downCells, word, sr, sc, true))
                {
                    int shared = CountShared(letters, word, sr, sc, true);
                    if (shared >= 1)
                        results.Add(new PlacementOption
                            { IsAcross = true, Row = sr, Col = sc, Shared = shared });
                }

                // Down: word[i] aligns with (gr, gc)
                sr = gr - i;
                sc = gc;
                if (CanPlace(letters, acrossCells, downCells, word, sr, sc, false))
                {
                    int shared = CountShared(letters, word, sr, sc, false);
                    if (shared >= 1)
                        results.Add(new PlacementOption
                            { IsAcross = false, Row = sr, Col = sc, Shared = shared });
                }
            }
        }

        return results;
    }

    private static bool CanPlace(
        Dictionary<Vector2Int, char> letters,
        HashSet<Vector2Int> acrossCells,
        HashSet<Vector2Int> downCells,
        string word,
        int r, int c,
        bool isAcross)
    {
        int L  = word.Length;
        int dr = isAcross ? 0 : 1;
        int dc = isAcross ? 1 : 0;

        var ownCells = isAcross ? acrossCells : downCells;

        // Cell before start must be empty
        var before = new Vector2Int(c - dc, r - dr);
        if (letters.ContainsKey(before)) return false;

        // Cell after end must be empty
        var after = new Vector2Int(c + dc * L, r + dr * L);
        if (letters.ContainsKey(after)) return false;

        int perpDr = isAcross ? 1 : 0;
        int perpDc = isAcross ? 0 : 1;

        for (int i = 0; i < L; i++)
        {
            int cr = r + dr * i;
            int cc = c + dc * i;
            char ch = word[i];
            var pos = new Vector2Int(cc, cr);

            char existing;
            if (letters.TryGetValue(pos, out existing))
            {
                // Letter conflict
                if (existing != ch) return false;
                // Must be claimed by the opposite direction (valid crossing)
                if (ownCells.Contains(pos)) return false;
            }
            else
            {
                // New cell — perpendicular neighbours must be empty
                var perp1 = new Vector2Int(cc + perpDc, cr + perpDr);
                var perp2 = new Vector2Int(cc - perpDc, cr - perpDr);
                if (letters.ContainsKey(perp1)) return false;
                if (letters.ContainsKey(perp2)) return false;
            }
        }

        return true;
    }

    private static int CountShared(
        Dictionary<Vector2Int, char> letters,
        string word, int r, int c, bool isAcross)
    {
        int dr = isAcross ? 0 : 1;
        int dc = isAcross ? 1 : 0;
        int shared = 0;
        for (int i = 0; i < word.Length; i++)
        {
            var pos = new Vector2Int(c + dc * i, r + dr * i);
            if (letters.ContainsKey(pos)) shared++;
        }
        return shared;
    }

    private static void PlaceWord(
        Dictionary<Vector2Int, char> letters,
        HashSet<Vector2Int> acrossCells,
        HashSet<Vector2Int> downCells,
        string word, int r, int c, bool isAcross)
    {
        int dr = isAcross ? 0 : 1;
        int dc = isAcross ? 1 : 0;
        var ownCells = isAcross ? acrossCells : downCells;

        for (int i = 0; i < word.Length; i++)
        {
            var pos = new Vector2Int(c + dc * i, r + dr * i);
            letters[pos] = word[i];
            ownCells.Add(pos);
        }
    }

    private static void UndoPlace(
        Dictionary<Vector2Int, char> letters,
        HashSet<Vector2Int> acrossCells,
        HashSet<Vector2Int> downCells,
        string word, int r, int c, bool isAcross)
    {
        int dr = isAcross ? 0 : 1;
        int dc = isAcross ? 1 : 0;
        var ownCells = isAcross ? acrossCells : downCells;

        for (int i = 0; i < word.Length; i++)
        {
            var pos = new Vector2Int(c + dc * i, r + dr * i);
            ownCells.Remove(pos);

            // Only remove letter if no other direction still claims this cell
            if (!acrossCells.Contains(pos) && !downCells.Contains(pos))
                letters.Remove(pos);
        }
    }

    // ── utility ───────────────────────────────────────────────────────────────

    private static bool BoundingBoxFits(
        Dictionary<Vector2Int, char> letters,
        int maxRows, int maxCols)
    {
        if (letters.Count == 0) return true;

        int minR = int.MaxValue, maxR = int.MinValue;
        int minC = int.MaxValue, maxC = int.MinValue;

        foreach (var kv in letters)
        {
            int r = kv.Key.y;
            int c = kv.Key.x;
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (c < minC) minC = c;
            if (c > maxC) maxC = c;
        }

        return (maxR - minR) < maxRows && (maxC - minC) < maxCols;
    }

    private static List<CrosswordEntry> CleanPool(List<CrosswordEntry> raw)
    {
        var seen   = new HashSet<string>();
        var result = new List<CrosswordEntry>();

        foreach (var e in raw)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.answer) || string.IsNullOrWhiteSpace(e.clue)) continue;

            string ans = e.answer.Trim().ToUpperInvariant();

            var chars = new System.Text.StringBuilder();
            foreach (char ch in ans)
                if (ch >= 'A' && ch <= 'Z') chars.Append(ch);
            ans = chars.ToString();

            if (ans.Length < 3 || ans.Length > 9) continue;
            if (seen.Contains(ans)) continue;

            seen.Add(ans);
            result.Add(new CrosswordEntry
            {
                id         = e.id,
                answer     = ans,
                clue       = e.clue.Trim(),
                difficulty = string.IsNullOrWhiteSpace(e.difficulty)
                           ? "medium"
                           : e.difficulty.Trim().ToLowerInvariant()
            });
        }

        return result;
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j   = rng.Next(i + 1);
            T   tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private static GeneratedCrossword EmptyResult(int rows, int cols)
    {
        var layout = new string[rows];
        for (int r = 0; r < rows; r++)
            layout[r] = new string('#', cols);

        return new GeneratedCrossword
        {
            rows        = rows,
            cols        = cols,
            layoutRows  = layout,
            placedWords = new List<CrosswordWord>()
        };
    }
}