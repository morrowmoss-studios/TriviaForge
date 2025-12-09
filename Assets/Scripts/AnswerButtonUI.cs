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
    
    [SerializeField] private Sprite disabledOutlineSprite; // ✅ your greyed-out version


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
    
    public void DisableAnswer()
    {
        // Change outline to greyed-out version
        if (outlineImage != null && disabledOutlineSprite != null)
            outlineImage.sprite = disabledOutlineSprite;

        // Dim the text
        if (answerText != null)
            answerText.alpha = 0.2f;

        if (letterText != null)
            letterText.alpha = 0.2f;

        // Disable the button
        GetComponent<Button>().interactable = false;
    }

}
