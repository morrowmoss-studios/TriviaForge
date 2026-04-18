using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to the root GameObject in the Avatars scene.
/// Populate the avatarButtons array with one Button per avatar sprite in order.
/// The button's Image component should already have the correct sprite assigned.
/// </summary>
public class AvatarSelectionController : MonoBehaviour
{
    [Header("Avatar Buttons (in order, matching sprite array)")]
    [SerializeField] private Button[] avatarButtons;

    [Header("Currently selected highlight")]
    [SerializeField] private Color selectedColor   = new Color(0.4f, 0.9f, 1f, 1f);
    [SerializeField] private Color unselectedColor = Color.white;

    private int _selectedIndex = -1;

    private void Start()
    {
        _selectedIndex = PlayerDatabaseAPI.GetAvatarIndex();

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int idx = i; // capture for lambda
            avatarButtons[i].onClick.AddListener(() => OnAvatarSelected(idx));
        }

        RefreshHighlights();
    }

    private void OnAvatarSelected(int index)
    {
        _selectedIndex = index;
        PlayerDatabaseAPI.SaveAvatarIndex(index);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            var img = avatarButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == _selectedIndex) ? selectedColor : unselectedColor;
        }
    }

    // Wire to your Back/Confirm button
    public void OnConfirmPressed()
    {
        FindObjectOfType<UIManager>()?.LoadPreviousScene();
    }
}