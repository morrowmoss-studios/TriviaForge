using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnswerButtonUI : MonoBehaviour
{
    [Header("UI Bits")]
    public Image outlineImage;              // the bubble outline image
    public TextMeshProUGUI letterText;      // "A.", "B.", "C.", "D."
    public TextMeshProUGUI answerText;      // actual answer text

    [HideInInspector] public int answerIndex;   // 0 = A, 1 = B, etc.

    // set by the manager when it creates/initializes this button
    private TriviaQuestionManager manager;

    // called from the manager when a question is loaded
    public void Init(TriviaQuestionManager mgr, int index, string letter, string answer)
    {
        manager = mgr;
        answerIndex = index;

        if (letterText != null)   letterText.text = letter;
        if (answerText != null)   answerText.text = answer;

        // reset outline to the default (teal)
        if (outlineImage != null && manager != null && manager.rightOutlineSprite != null)
        {
            outlineImage.sprite = manager.rightOutlineSprite;
        }
    }

    // wired to the Button's OnClick event
    public void OnClick()
    {
        if (manager != null)
        {
            manager.OnAnswerClicked(this);
        }
    }

    public void ShowWrong()
    {
        if (outlineImage != null && manager != null && manager.wrongOutlineSprite != null)
        {
            outlineImage.sprite = manager.wrongOutlineSprite;
        }
    }

    public void ResetOutline()
    {
        if (outlineImage != null && manager != null && manager.rightOutlineSprite != null)
        {
            outlineImage.sprite = manager.rightOutlineSprite;
        }
    }
}