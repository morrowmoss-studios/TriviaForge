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
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private TMP_Text notesText;
    [SerializeField] private Image tileBackground;
    [SerializeField] private Sprite brownTile;
    [SerializeField] private Sprite whiteTile;
    [SerializeField] private Color lockedLetterColor = Color.black;
    [SerializeField] private Color playerLetterColor = Color.white;

    [Header("Notes Font (optional)")]
    [SerializeField] private TMP_FontAsset notesFont;
    private const string NotesFontResourceName = "Roboto_Notes";

    [Header("Wrong Guess Tint")]
    [SerializeField] private Color normalTileColor = Color.white;
    [SerializeField] private Color wrongGuessColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(0.9f, 0.7f, 1f, 1f);

    [Header("Grid Coords")]
    public int row;
    public int col;

    private bool locked;
    private string currentLetter = "";
    private HashSet<char> notes = new HashSet<char>();

    private WordokuManager manager;

    private float baseLetterFontSize = 30f;
    private bool isWrong = false;
    private bool highlightSelectedLetter = false;

    public bool IsWrong => isWrong;

    public void RestoreWrongState(bool wrong)
    {
        isWrong = wrong;
        UpdateTileVisual();
    }

    private void Awake()
    {
        if (letterText == null)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (letterText != null)
            baseLetterFontSize = letterText.fontSize;

        if (tileBackground == null)
            tileBackground = GetComponent<Image>();

        if (tileBackground != null && normalTileColor == Color.white)
            normalTileColor = tileBackground.color;

        if (notesFont == null)
        {
            notesFont = Resources.Load<TMP_FontAsset>(NotesFontResourceName);
            if (notesFont == null)
                Debug.LogWarning($"WordokuCell: Could not load notes font '{NotesFontResourceName}' from Resources.");
        }

        EnsureNotesTextExists();

        if (notesFont != null && notesText != null)
            notesText.font = notesFont;

        manager = FindObjectOfType<WordokuManager>();

        LayoutTexts();
        UpdateTileVisual();
        UpdateNotesVisual();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCellInternal(false);
        LayoutTexts();
        UpdateTileVisual();
    }

    // ---------- INTERNAL WIRING ----------

    private void EnsureNotesTextExists()
    {
        if (notesText != null) return;

        if (letterText != null)
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length > 1)
                notesText = tmps.FirstOrDefault(t => t.gameObject != letterText.gameObject);
        }

        if (notesText == null && letterText != null)
        {
            RectTransform cellRT = GetComponent<RectTransform>();
            GameObject notesGO = new GameObject("Notes_Text", typeof(RectTransform));
            notesGO.transform.SetParent(cellRT, false);

            notesText = notesGO.AddComponent<TextMeshProUGUI>();
            notesText.font = letterText.font;
            notesText.fontSize = letterText.fontSize * 0.55f;
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

        if (notesText != null)
        {
            RectTransform notesRT = notesText.rectTransform;
            notesRT.SetParent(cellRT, false);
            notesRT.anchorMin = Vector2.zero;
            notesRT.anchorMax = Vector2.one;

            const float pad = 6f;
            notesRT.offsetMin = new Vector2(pad, pad);
            notesRT.offsetMax = new Vector2(-pad, -pad);

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

        if (highlightSelectedLetter && whiteTile != null)
        {
            tileBackground.sprite = whiteTile;
        }
        else
        {
            if (!string.IsNullOrEmpty(currentLetter))
            {
                if (brownTile != null) tileBackground.sprite = brownTile;
            }
            else
            {
                if (whiteTile != null) tileBackground.sprite = whiteTile;
            }
        }

        if (isWrong)
            tileBackground.color = wrongGuessColor;
        else if (highlightSelectedLetter)
            tileBackground.color = highlightColor;
        else
            tileBackground.color = normalTileColor;
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

        var ordered = notes.OrderBy(c => c).ToList();
        int count   = ordered.Count;

        float factor = count <= 3 ? 0.75f
                     : count <= 6 ? 0.62f
                     :              0.48f;

        notesText.fontSize = baseLetterFontSize * factor;

        int cols = 3;
        int rows = Mathf.CeilToInt((float)count / cols);

        var sb = new StringBuilder();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                sb.Append(idx < count ? ordered[idx].ToString() : " ");
                if (c < cols - 1) sb.Append(' ');
            }
            if (r < rows - 1) sb.AppendLine();
        }

        notesText.text = sb.ToString();
        notesText.gameObject.SetActive(true);
        notesText.ForceMeshUpdate();
    }

    // ---------- API ----------

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        if (letterText != null)
            letterText.text = letter;

        notes.Clear();
        isWrong = false;
        UpdateNotesVisual();
        LayoutTexts();
        UpdateTileVisual();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        if (letterText != null)
            letterText.color = locked ? lockedLetterColor : playerLetterColor;
        UpdateTileVisual();
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText != null) letterText.text = "";
        notes.Clear();
        isWrong = false;
        UpdateNotesVisual();
        UpdateTileVisual();

        if (manager != null)
            manager.NotifyBoardChanged();
    }

    private void ClearCellInternal(bool notify)
    {
        currentLetter = "";
        if (letterText != null) letterText.text = "";
        notes.Clear();
        isWrong = false;
        UpdateNotesVisual();
        UpdateTileVisual();

        if (notify && manager != null)
            manager.NotifyBoardChanged();
    }

    public void RemoveNotesForLetters(HashSet<char> completedLetters)
    {
        if (notes.Count == 0) return;

        int before = notes.Count;
        notes.RemoveWhere(c => completedLetters.Contains(c));

        if (notes.Count != before)
            UpdateNotesVisual();
    }

    // ---------- CLICK ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (locked) return;

        var selected = LetterSelectionManager.Instance.SelectedLetter;

        if (manager != null && manager.NotesMode)
        {
            if (!selected.HasValue) return;
            ToggleNote(selected.Value);
            return;
        }

        if (!selected.HasValue)
        {
            ClearCell();
            return;
        }

        char sel = selected.Value;

        if (!string.IsNullOrEmpty(currentLetter) && currentLetter[0] == sel)
        {
            ClearCell();
            return;
        }

        PlaceLetter(sel);
    }

    private void ToggleNote(char letter)
    {
        if (!string.IsNullOrEmpty(currentLetter)) return;

        if (notes.Contains(letter)) notes.Remove(letter);
        else notes.Add(letter);

        UpdateNotesVisual();
    }

    // ---------- CORE PLACEMENT ----------

    public void PlaceLetter(char letter)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayTilePlaced();

        if (manager == null)
        {
            Debug.LogError($"WordokuCell at [{row},{col}] has NO WordokuManager reference!");
            return;
        }

        Debug.Log($"Trying {letter} at [{row},{col}]");

        currentLetter = letter.ToString();
        if (letterText != null)
            letterText.text = currentLetter;

        notes.Clear();
        UpdateNotesVisual();

        char expected = manager.GetSolutionLetter(row, col);
        isWrong = (letter != expected);

        if (isWrong)
            manager.ReportWrongPlacement();

        LayoutTexts();
        UpdateTileVisual();

        manager.NotifyBoardChanged();
    }

    public void SetLetterHighlight(bool isHighlighted)
    {
        highlightSelectedLetter = isHighlighted;
        UpdateTileVisual();
    }
}