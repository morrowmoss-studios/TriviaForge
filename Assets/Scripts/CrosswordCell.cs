using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image tileImage;      // auto-grabbed in Awake if null
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Sprite playableSprite;   // light/beige tile
    [SerializeField] private Sprite blockedSprite;    // dark/wood tile

    [Header("Highlight")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan; // set this in prefab

    [Header("Grid Coords (read-only at runtime)")]
    public int row;
    public int col;

    private bool isBlocked;
    private bool isHighlighted;

    private CrosswordBoardManager manager;

    // store whatever letter is currently shown in this cell
    private char currentLetter = '\0';

    // Called right after Instantiate by the board manager
    public void Init(CrosswordBoardManager mgr, int r, int c, bool blocked)
    {
        manager = mgr;
        row = r;
        col = c;
        SetBlocked(blocked);
        SetHighlighted(false);
        SetLetter('\0');   // start empty
    }

    private void Awake()
    {
        // auto–grab the Image if not wired
        if (tileImage == null)
            tileImage = GetComponent<Image>();

        // auto–find the TMP if we forgot to assign it
        if (letterText == null)
            letterText = GetComponentInChildren<TMP_Text>(true);

        if (letterText != null)
        {
            var rt = letterText.rectTransform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            letterText.alignment = TextAlignmentOptions.Center;
            letterText.text = "";
            letterText.gameObject.SetActive(true);
        }
    }

    // ---------- LETTER API ----------

    public void SetLetter(char ch)
    {
        currentLetter = ch;

        if (letterText == null) return;

        if (ch == '\0' || ch == ' ')
            letterText.text = "";
        else
            letterText.text = ch.ToString().ToUpper();
    }

    public char GetLetter()
    {
        return currentLetter;
    }

    // ---------- BLOCK / HIGHLIGHT ----------

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;

        if (tileImage == null) return;

        tileImage.sprite = blocked ? blockedSprite : playableSprite;
        tileImage.color = normalColor;
    }

    public bool IsBlocked => isBlocked;

    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;

        if (tileImage == null) return;

        tileImage.color = highlighted ? highlightColor : normalColor;
    }

    // ---------- CLICK ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;

        if (manager != null)
        {
            manager.OnCellClicked(this);
        }
    }
}
