using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordokuCell : MonoBehaviour,
    IPointerClickHandler,
    IDropHandler
{
    [SerializeField] private TMP_Text letterText;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite brownTile;
    [SerializeField] private Sprite whiteTile;

    public int row, col;

    private bool locked;
    private string currentLetter = "";

    private Image cellImage;
    private WordokuManager manager;

    private void Awake()
    {
        if (!letterText)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);

        cellImage = GetComponent<Image>(); // ← THIS WAS THE MISSING LINK
        manager = FindObjectOfType<WordokuManager>();

        ForceLockText();
        UpdateTileVisual();
    }

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
        ClearCell();
        ForceLockText();
        UpdateTileVisual();
    }

    // ---------------- VISUAL STATE ----------------

    private void UpdateTileVisual()
    {
        if (!cellImage) return;

        // FINAL RULE (as agreed):
        // Any letter (locked OR player-placed) → brown
        // Empty → white
        cellImage.sprite = string.IsNullOrEmpty(currentLetter)
            ? whiteTile
            : brownTile;
    }

    private void ForceLockText()
    {
        if (!letterText) return;

        RectTransform rt = letterText.rectTransform;
        RectTransform cellRT = GetComponent<RectTransform>();

        rt.SetParent(cellRT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        letterText.alignment = TextAlignmentOptions.Center;
        letterText.enableWordWrapping = false;
        letterText.autoSizeTextContainer = false;

        letterText.ForceMeshUpdate();
    }

    // ---------------- GAME LOGIC ----------------

    public void SetLetter(string letter)
    {
        currentLetter = letter;
        letterText.text = letter;
        ForceLockText();
        UpdateTileVisual();
    }

    public string GetLetter() => currentLetter;

    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        letterText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        UpdateTileVisual();
    }

    public bool isLocked => locked;

    public void ClearCell()
    {
        currentLetter = "";
        if (letterText) letterText.text = "";
        UpdateTileVisual();
    }

    // ---------------- INPUT ----------------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (locked) return;

        var selected = LetterSelectionManager.Instance.SelectedLetter;
        if (selected.HasValue)
        {
            PlaceLetter(selected.Value);
            LetterSelectionManager.Instance.ClearSelection();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (locked) return;

        var letterButton = eventData.pointerDrag?.GetComponent<LetterChoiceButton>();
        if (!letterButton) return;

        PlaceLetter(letterButton.GetLetter());
    }

    public void PlaceLetter(char letter)
    {
        if (manager == null)
        {
            Debug.LogError($"WordokuCell at [{row},{col}] has NO WordokuManager reference!");
            return;
        }

        Debug.Log($"Trying {letter} at [{row},{col}]");

        // 1) Optional: strict “must match solution” mode for testing
        if (manager.enforceSolutionWhileTesting)
        {
            char expected = manager.GetSolutionLetter(row, col);
            if (letter != expected)
            {
                Debug.Log($"Rejected {letter} at [{row},{col}] – solution expects {expected}");
                return;
            }
        }

        // 2) Normal Sudoku-rule validation
        bool valid = manager.IsValidPlacement(row, col, letter);
        Debug.Log($"Validation result for {letter} at [{row},{col}] = {valid}");

        if (!valid)
            return;

        // 3) Actually place the letter
        currentLetter = letter.ToString();
        letterText.text = currentLetter;
        ForceLockText();
        UpdateTileVisual();

        // 4) 🔔 Tell the manager the board changed (so it can hide finished letters)
        manager.NotifyBoardChanged();
    }

}
