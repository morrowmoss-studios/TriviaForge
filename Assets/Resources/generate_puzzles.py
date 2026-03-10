#!/usr/bin/env python3
"""
TriviaForge Crossword Pre-Generator
------------------------------------
Run from the same folder as trivia_database.json:
    python3 generate_puzzles.py

Generates 25 crossword puzzles per category, validates that no nonsense
words are created in either direction, and saves them back into the JSON.
"""

import json
import random
import re
import sys
import os

# ─────────────────────────────────────────────
# Config
# ─────────────────────────────────────────────
GRID_SIZE       = 13      # 13x13 gives plenty of room
PUZZLES_PER_CAT = 25
MIN_WORDS       = 8       # minimum words to consider a puzzle valid
TARGET_WORDS    = 16      # aim for this many placed words
MAX_PLACE_TRIES = 6000    # attempts to place each word
INPUT_FILE      = "trivia_database.json"
OUTPUT_FILE     = "trivia_database.json"  # overwrites in place

# ─────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────

def clean(word):
    return re.sub(r'[^A-Z]', '', word.upper())


class CrosswordGrid:
    """
    NxN grid. Cells start as None (empty).
    Words are placed and perpendicular runs are validated against the pool.
    """

    def __init__(self, size, word_set):
        self.size = size
        self.grid = [[None] * size for _ in range(size)]
        self.word_set = word_set
        self.placed = []

    def in_bounds(self, r, c):
        return 0 <= r < self.size and 0 <= c < self.size

    def get(self, r, c):
        if not self.in_bounds(r, c):
            return None
        return self.grid[r][c]

    def perp_run(self, r, c, placing_across):
        """Return full perpendicular letter run through (r,c)."""
        if placing_across:
            # perpendicular is vertical
            sr = r
            while sr - 1 >= 0 and self.grid[sr - 1][c] is not None:
                sr -= 1
            run = []
            rr = sr
            while rr < self.size and self.grid[rr][c] is not None:
                run.append(self.grid[rr][c])
                rr += 1
            return ''.join(run)
        else:
            # perpendicular is horizontal
            sc = c
            while sc - 1 >= 0 and self.grid[r][sc - 1] is not None:
                sc -= 1
            run = []
            cc = sc
            while cc < self.size and self.grid[r][cc] is not None:
                run.append(self.grid[r][cc])
                cc += 1
            return ''.join(run)

    def _validate_perps(self, word, row, col, across):
        """
        Simulate placing word and verify every new perpendicular run is a valid
        word in the pool (if length > 1). Returns True if all checks pass.
        """
        backup = {}
        for i, ch in enumerate(word):
            r = row + (0 if across else i)
            c = col + (i if across else 0)
            backup[(r, c)] = self.grid[r][c]
            self.grid[r][c] = ch

        valid = True
        for i, ch in enumerate(word):
            r = row + (0 if across else i)
            c = col + (i if across else 0)

            # Only check newly placed cells
            if backup[(r, c)] is not None:
                continue

            run = self.perp_run(r, c, placing_across=across)
            if len(run) > 1 and run not in self.word_set:
                valid = False
                break

        for (r, c), v in backup.items():
            self.grid[r][c] = v

        return valid

    def can_place(self, word, row, col, across):
        length = len(word)

        # Bounds
        if across:
            if col < 0 or col + length > self.size: return False
        else:
            if row < 0 or row + length > self.size: return False

        # No merging at ends
        if across:
            if self.get(row, col - 1) is not None: return False
            if self.get(row, col + length) is not None: return False
        else:
            if self.get(row - 1, col) is not None: return False
            if self.get(row + length, col) is not None: return False

        has_intersection = False

        for i, ch in enumerate(word):
            r = row + (0 if across else i)
            c = col + (i if across else 0)
            existing = self.grid[r][c]

            if existing is not None:
                if existing != ch:
                    return False
                has_intersection = True

        # First word doesn't need an intersection
        if self.placed and not has_intersection:
            return False

        return self._validate_perps(word, row, col, across)

    def place(self, word, clue, row, col, across):
        for i, ch in enumerate(word):
            r = row + (0 if across else i)
            c = col + (i if across else 0)
            self.grid[r][c] = ch
        self.placed.append({
            'word': word, 'clue': clue,
            'row': row, 'col': col, 'across': across
        })

    def to_puzzle(self):
        """Return (layoutRows, placedWords) trimmed to used area."""
        used_cells = set()
        for p in self.placed:
            for i in range(len(p['word'])):
                r = p['row'] + (0 if p['across'] else i)
                c = p['col'] + (i if p['across'] else 0)
                used_cells.add((r, c))

        if not used_cells:
            return None, None

        min_r = min(r for r, c in used_cells)
        max_r = max(r for r, c in used_cells)
        min_c = min(c for r, c in used_cells)
        max_c = max(c for r, c in used_cells)

        layout = []
        for r in range(min_r, max_r + 1):
            row_str = ''
            for c in range(min_c, max_c + 1):
                row_str += '.' if (r, c) in used_cells else '#'
            layout.append(row_str)

        words = []
        for idx, p in enumerate(self.placed):
            words.append({
                'id':       f"{idx + 1}{'A' if p['across'] else 'D'}",
                'isAcross': p['across'],
                'startRow': p['row'] - min_r,
                'startCol': p['col'] - min_c,
                'answer':   p['word'],
                'clue':     p['clue']
            })

        return layout, words


# ─────────────────────────────────────────────
# Generator
# ─────────────────────────────────────────────

def generate_crossword(entries, rng, size=GRID_SIZE,
                       target=TARGET_WORDS, max_tries=MAX_PLACE_TRIES):
    if not entries:
        return None

    word_set = set(e['answer'] for e in entries)
    grid = CrosswordGrid(size, word_set)

    shuffled = list(entries)
    rng.shuffle(shuffled)

    # Place first word horizontally near center
    first = None
    center_row = size // 2
    for e in shuffled:
        w = e['answer']
        col = (size - len(w)) // 2
        if grid.can_place(w, center_row, col, across=True):
            grid.place(w, e['clue'], center_row, col, across=True)
            first = e
            break

    if first is None:
        return None

    used = {first['answer']}
    remaining = [e for e in shuffled if e['answer'] not in used]

    attempts = 0
    stall = 0

    while len(grid.placed) < target and attempts < max_tries and remaining:
        attempts += 1
        candidate = rng.choice(remaining)
        word = candidate['answer']
        clue = candidate['clue']

        # Gather all placed letters as potential intersections
        intersections = []
        for p in grid.placed:
            for i, ch in enumerate(p['word']):
                r = p['row'] + (0 if p['across'] else i)
                c = p['col'] + (i if p['across'] else 0)
                intersections.append((r, c, ch))

        rng.shuffle(intersections)

        placed_this = False
        for (ir, ic, ich) in intersections:
            for j, wch in enumerate(word):
                if wch != ich:
                    continue

                # Try across
                r_a, c_a = ir, ic - j
                if grid.can_place(word, r_a, c_a, across=True):
                    grid.place(word, clue, r_a, c_a, across=True)
                    used.add(word)
                    remaining = [e for e in remaining if e['answer'] not in used]
                    placed_this = True
                    break

                # Try down
                r_d, c_d = ir - j, ic
                if grid.can_place(word, r_d, c_d, across=False):
                    grid.place(word, clue, r_d, c_d, across=False)
                    used.add(word)
                    remaining = [e for e in remaining if e['answer'] not in used]
                    placed_this = True
                    break

            if placed_this:
                break

        if not placed_this:
            remaining.remove(candidate)
            remaining.append(candidate)
            stall += 1
            if stall > len(remaining) * 3:
                break  # no progress possible, bail early

    if len(grid.placed) < MIN_WORDS:
        return None

    layout, words = grid.to_puzzle()
    if layout is None:
        return None

    return layout, words


# ─────────────────────────────────────────────
# Main
# ─────────────────────────────────────────────

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    input_path = os.path.join(script_dir, INPUT_FILE)

    if not os.path.exists(input_path):
        print(f"ERROR: Could not find {input_path}")
        print("Make sure generate_puzzles.py is in the same folder as trivia_database.json")
        sys.exit(1)

    print(f"Loading {input_path} ...")
    with open(input_path, 'r', encoding='utf-8') as f:
        db = json.load(f)

    rng = random.Random()

    for cat in db['categories']:
        cat_id = cat['id']

        # Collect all crossword entries from all subcategories
        all_entries = []
        for sub in cat.get('subcategories', []):
            for e in sub.get('crosswords', []):
                ans = clean(e.get('answer', ''))
                clue = (e.get('clue') or '').strip()
                if not ans or not clue: continue
                if len(ans) < 3 or len(ans) > 10: continue
                all_entries.append({
                    'answer':     ans,
                    'clue':       clue,
                    'difficulty': e.get('difficulty', 'medium')
                })

        # Dedupe by answer
        seen = set()
        entries = []
        for e in all_entries:
            if e['answer'] not in seen:
                seen.add(e['answer'])
                entries.append(e)

        print(f"\n{'='*52}")
        print(f"  {cat_id.upper()}  ({len(entries)} unique words)")
        print(f"{'='*52}")

        puzzles = []
        fails   = 0
        max_fails = PUZZLES_PER_CAT * 15

        while len(puzzles) < PUZZLES_PER_CAT:
            result = generate_crossword(entries, rng)
            if result is None:
                fails += 1
                if fails > max_fails:
                    print(f"  WARNING: too many failures, stopping at {len(puzzles)} puzzles")
                    break
                continue

            layout, word_list = result
            puzzles.append({
                'layoutRows':  layout,
                'placedWords': word_list
            })

            n = len(puzzles)
            print(f"  [{n:>2}/{PUZZLES_PER_CAT}] {len(word_list):>2} words  "
                  f"grid {len(layout)}x{len(layout[0])}  "
                  f"(fails: {fails})")

        cat['puzzles'] = puzzles
        print(f"  Saved {len(puzzles)} puzzles for {cat_id}.")

    output_path = os.path.join(script_dir, OUTPUT_FILE)
    print(f"\nWriting to {output_path} ...")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(db, f, indent=2, ensure_ascii=False)

    print("\n✅  Done! Drop the updated trivia_database.json into Unity's Resources folder.")
    print("    Then update CrosswordBoardManager to read from cat['puzzles'] instead of generating.")


if __name__ == '__main__':
    main()
