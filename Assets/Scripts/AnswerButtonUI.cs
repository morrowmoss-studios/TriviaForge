using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnswerButtonUI : MonoBehaviour
{
    [Header("UI Bits")]
    public Image outlineImage;
    public TextMeshProUGUI letterText;
    public TextMeshProUGUI answerText;

    [HideInInspector] public int answerIndex;

    private TriviaQuestionManager manager;

    public void Init(TriviaQuestionManager mgr, int index, string letter, string answer)
    {
        manager = mgr;
        answerIndex = index;

        if (letterText != null) letterText.text = letter;
        if (answerText != null) answerText.text = answer;

        ResetOutline();
    }

    public void OnClick()
    {
        if (manager != null)
            manager.OnAnswerClicked(this);
    }

    public void ResetOutline()
    {
        if (outlineImage != null && manager != null && manager.rightOutlineSprite != null)
            outlineImage.sprite = manager.rightOutlineSprite;
    }

    public void ShowAsCorrect()
    {
        if (outlineImage != null && manager != null && manager.rightOutlineSprite != null)
            outlineImage.sprite = manager.rightOutlineSprite;
    }

    public void ShowAsWrong()
    {
        if (outlineImage != null && manager != null && manager.wrongOutlineSprite != null)
            outlineImage.sprite = manager.wrongOutlineSprite;
    }
}
