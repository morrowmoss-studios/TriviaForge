using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordokuCell : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text letterText;          // main big letter
    [SerializeField] private TMP_Text notesText;           // small notes letters
    [SerializeField] private Image tileBackground;         // background image on the cell
    [SerializeField] private Sprite brownTile;             // filled
    [SerializeField] private Sprite whiteTile;             // empty

    [Header("Notes Font (optional)")]
    [SerializeField] private TMP_FontAsset notesFont;      // can be left null, we'll auto-load

    // Name of the TMP Font Asset in Resources to use for notes if notesFont is not assigned
    private const string NotesFontResourceName = "Roboto_Notes";

    [Header("Grid Coords")]
    public int row;
    public int col;

    private bool locked;
    private string currentLetter = "";
    private HashSet<char> notes = new HashSet<char>();

    private WordokuManager manager;

    // base main-letter font size so we can scale notes relative to it
    private float baseLetterFontSize = 30f;

    private void Awake()
    {
        // --------- GRAB REFERENCES ---------

        // Find the main letter TMP if not wired
        if (letterText == null)
        {
            // First TMP we find becomes the letter text
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (letterText != null)
        {
            baseLetterFontSize = letterText.fontSize;
        }

        // Auto-grab background image if not wired
        if (tileBackground == null)
        {
            tileBackground = GetComponent<Image>();
        }

        // Try to auto-load notes font from Resources if not assigned
        if (notesFont == null)
        {
            notesFont = Resources.Load<TMP_FontAsset>(NotesFontResourceName);
            if (notesFont == null)
            {
                Debug.LogWarning($"WordokuCell: Could not load notes font '{NotesFontResourceName}' from Resources. Notes will use the main font instead.");
            }
        }

        // Auto-create / grab notes TMP
        EnsureNotesTextExists();

        // Use custom font for notes if provided
        if (notesFont != null && notesText != null)
        {
            notesText.font = notesFont;
        }

        manager = FindObjectOfType<WordokuManager>();

        LayoutTexts();
        UpdateTileVisual();
        UpdateNotesVisual();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
        LayoutTexts();
        UpdateTileVisual();
    }

    // ---------- INTERNAL WIRING ----------

    private void EnsureNotesTextExists()
    {
        if (notesText != null) return;

        // Try to find an existing second TMP
        if (letterText != null)
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length > 1)
            {
                notesText = tmps.FirstOrDefault(t => t.gameObject != letterText.gameObject);
            }
        }

        // If still none, create one
        if (notesText == null && letterText != null)
        {
            RectTransform cellRT = GetComponent<RectTransform>();

            GameObject notesGO = new GameObject("Notes_Text", typeof(RectTransform));
            notesGO.transform.SetParent(cellRT, false);

            notesText = notesGO.AddComponent<TextMeshProUGUI>();
            notesText.font = letterText.font;                   // will be overridden by notesFont later if set
            notesText.fontSize = letterText.fontSize * 0.55f;   // initial size, will be replaced dynamically
            notesText.fontStyle = FontStyles.Bold;
            notesText.color = Color.black;
            notesText.alignment = TextAlignmentOptions.Center;
            notesText.raycastTarget = false;
            notesText.enableWordWrapping = false;
            notesText.overflowMode = TextOverflowModes.Truncate;
        }
    }

    private void LayoutTexts()
    {
        RectTransform cellRT = GetComponent<RectTransform>();

        // Main letter layout
        if (letterText != null)
        {
            RectTransform textRT = letterText.rectTransform;
            textRT.SetParent(cellRT, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.localScale = Vector3.one;
            textRT.localRotation = Quaternion.identity;

            letterText.margin = Vector4.zero;
            letterText.alignment = TextAlignmentOptions.Center;
            letterText.enableWordWrapping = false;
            letterText.autoSizeTextContainer = false;
        }

        // Notes layout (same rect, smaller text)
        if (notesText != null)
        {
            RectTransform notesRT = notesText.rectTransform;
            notesRT.SetParent(cellRT, false);
            notesRT.anchorMin = Vector2.zero;
            notesRT.anchorMax = Vector2.one;
            notesRT.offsetMin = Vector2.zero;
            notesRT.offsetMax = Vector2.zero;
            notesRT.pivot = new Vector2(0.5f, 0.5f);
            notesRT.localScale = Vector3.one;
            notesRT.localRotation = Quaternion.identity;

            notesText.alignment = TextAlignmentOptions.Center;
            notesText.enableWordWrapping = false;
            notesText.autoSizeTextContainer = false;
        }
    }

    // ---------- VISUALS ----------

    private void UpdateTileVisual()
    {
        if (tileBackground == null) return;

        // Any letter (locked or player-placed) = brown
        // Empty = white
        if (!string.IsNullOrEmpty(currentLetter))
        {
            if (brownTile != null)
                tileBackground.sprite = brownTile;
        }
        else
        {
            if (whiteTile != null)
                tileBackground.sprite = whiteTile;
        }
    }

    private void UpdateNotesVisual()
    {
        if (notesText == null) return;

        if (notes.Count == 0 || !string.IsNullOrEmpty(currentLetter))
        {
            notesText.text = "";
            notesText.gameObject.SetActive(false);
            return;
        }

        // === dynamic font size based on number of notes ===
        int noteCount = Mathf.Clamp(notes.Count, 1, 9);

        // t = 0 when 1 note, 1 when 9 notes
        float t = (noteCount - 1) / 8f;

        // scale factor: 1 note ~ 0.8 of main letter, 9 notes ~ 0.5
        float factor = Mathf.Lerp(0.8f, 0.5f, t);

        notesText.fontSize = baseLetterFontSize * factor;

        // === build 3x3 grid ===
        char?[] slots = new char?[9];

        if (manager != null && manager.CurrentLetters != null)
        {
            foreach (char n in notes)
            {
                int idx = System.Array.IndexOf(manager.CurrentLetters, n);
                if (idx >= 0 && idx < 9)
                {
                    slots[idx] = n;
                }
            }
        }
        else
        {
            // Fallback: just drop them in order
            int i = 0;
            foreach (char n in notes)
            {
                if (i >= 9) break;
                slots[i++] = n;
            }
        }

        StringBuilder sb = new StringBuilder();

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int slot = r * 3 + c;
                char ch = slots[slot].HasValue ? slots[slot].Value : ' ';
                sb.Append(ch == '\0' ? ' ' : ch);
                if (c < 2) sb.Append(' '); // spacing between columns
            }
            if (r < 2) sb.AppendLine();
        }

        notesText.text = sb.ToString();
        notesText.gameObject.SetActive(true);
    }

    // ---------- API ----------

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        if (letterText != null)
            letterText.text = letter;

        // Once a real letter is placed, clear notes
        notes.Clear();
        UpdateNotesVisual();

        LayoutTexts();
        UpdateTileVisual();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        if (letterText != null)
            letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;

        // keep tile brown/white based on letter, not lock state
        UpdateTileVisual();
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText != null)
            letterText.text = "";

        notes.Clear();
        UpdateNotesVisual();

        UpdateTileVisual();
    }

    // ---------- CLICK TO PLACE OR NOTE ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (locked) return;

        var selected = LetterSelectionManager.Instance.SelectedLetter;
        if (!selected.HasValue) return;

        if (manager != null && manager.NotesMode)
        {
            ToggleNote(selected.Value);
        }
        else
        {
            PlaceLetter(selected.Value);
        }
    }

    private void ToggleNote(char letter)
    {
        // Don't allow notes on already-filled cells
        if (!string.IsNullOrEmpty(currentLetter))
            return;

        if (notes.Contains(letter))
            notes.Remove(letter);
        else
            notes.Add(letter);

        UpdateNotesVisual();
    }

    // ---------- CORE PLACEMENT ----------

    public void PlaceLetter(char letter)
    {
        if (manager == null)
        {
            Debug.LogError($"WordokuCell at [{row},{col}] has NO WordokuManager reference!");
            return;
        }

        Debug.Log($"Trying {letter} at [{row},{col}]");

        // Optional: enforce the real solution while testing
        if (manager.enforceSolutionWhileTesting)
        {
            char expected = manager.GetSolutionLetter(row, col);
            if (letter != expected)
            {
                Debug.Log($"Rejected {letter} at [{row},{col}] – solution expects {expected}");
                return;
            }
        }

        bool valid = manager.IsValidPlacement(row, col, letter);
        Debug.Log($"Validation result for {letter} at [{row},{col}] = {valid}");

        if (!valid)
            return;

        // Actually place it
        currentLetter = letter.ToString();
        if (letterText != null)
            letterText.text = currentLetter;

        // Clear notes when final answer placed
        notes.Clear();
        UpdateNotesVisual();

        LayoutTexts();
        UpdateTileVisual();

        // Tell the manager the board changed so letter buttons can update
        manager.NotifyBoardChanged();
    }
}
