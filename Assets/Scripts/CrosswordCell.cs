using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosswordCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image tileImage;
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private TMP_Text numberLabel;   // wire up NumberLabel in prefab
    [SerializeField] private Sprite playableSprite;
    [SerializeField] private Sprite blockedSprite;

    [Header("Highlight")]
    [SerializeField] private Color normalColor    = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("Grid Coords (read-only at runtime)")]
    public int row;
    public int col;

    private bool isBlocked;
    private char currentLetter = '\0';
    private CrosswordBoardManager manager;

    public void Init(CrosswordBoardManager mgr, int r, int c, bool blocked)
    {
        manager = mgr;
        row = r;
        col = c;
        SetBlocked(blocked);
        SetHighlighted(false);
        SetLetter('\0');
        SetNumber("");
    }

    private void Awake()
    {
        if (tileImage == null)
            tileImage = GetComponent<Image>();

        if (letterText == null)
            letterText = GetComponentInChildren<TMP_Text>(true);

        // Auto-find NumberLabel by name if not assigned in inspector
        if (numberLabel == null)
        {
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.name == "NumberLabel")
                {
                    numberLabel = tmp;
                    break;
                }
            }
        }

        if (letterText != null)
        {
            var rt = letterText.rectTransform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.localScale    = Vector3.one;
            rt.localRotation = Quaternion.identity;

            letterText.alignment = TextAlignmentOptions.Center;
            letterText.text = "";
            letterText.gameObject.SetActive(true);
        }

        if (numberLabel != null)
            numberLabel.gameObject.SetActive(true);
    }

    // ---------- LETTER ----------

    public void SetLetter(char ch)
    {
        currentLetter = ch;
        if (letterText == null) return;
        letterText.text = (ch == '\0' || ch == ' ') ? "" : ch.ToString().ToUpper();
    }

    public char GetLetter() => currentLetter;

    // ---------- NUMBER ----------

    public void SetNumber(string num)
    {
        if (numberLabel == null) return;
        numberLabel.text = num;
        numberLabel.gameObject.SetActive(!string.IsNullOrEmpty(num));
    }

    // ---------- BLOCK / HIGHLIGHT ----------

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        if (tileImage == null) return;
        tileImage.sprite = blocked ? blockedSprite : playableSprite;
        tileImage.color  = normalColor;
    }

    public bool IsBlocked => isBlocked;

    public void SetHighlighted(bool highlighted)
    {
        if (tileImage == null) return;
        tileImage.color = highlighted ? highlightColor : normalColor;
    }

    // ---------- CLICK ----------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;
        manager?.OnCellClicked(this);
    }
}