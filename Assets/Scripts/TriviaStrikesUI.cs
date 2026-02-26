using UnityEngine;
using UnityEngine.UI;

public class TriviaStrikesUI : MonoBehaviour
{
    [Header("Assign 3 X Images in order (1,2,3)")]
    [SerializeField] private Image[] strikeImages;

    [Header("Optional: tint for inactive Xs")]
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color activeColor   = Color.white;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int strikes = Mathf.Clamp(TriviaSessionData.strikes, 0, TriviaSessionData.maxStrikes);

        for (int i = 0; i < strikeImages.Length; i++)
        {
            if (strikeImages[i] == null) continue;

            bool active = i < strikes;
            strikeImages[i].color = active ? activeColor : inactiveColor;
        }
    }
}