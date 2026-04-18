using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the User_Display prefab root.
/// Populates the username TMP and avatar image on Start.
/// </summary>
public class UserDisplaySync : MonoBehaviour
{
    [SerializeField] private TMP_Text  usernameText;
    [SerializeField] private Image     avatarImage;
    [SerializeField] private Sprite[]  avatarSprites;
    [SerializeField] private Sprite    defaultSprite;

    private void Start()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        // Username
        if (usernameText != null)
        {
            bool isGuest = PlayerDatabaseAPI.IsGuest;
            string name  = PlayerDatabaseAPI.CurrentUsername;
            usernameText.text = isGuest || string.IsNullOrEmpty(name) ? "Guest" : name;
        }

        // Avatar
        if (avatarImage != null)
        {
            int idx = PlayerDatabaseAPI.GetAvatarIndex();
            if (idx >= 0 && avatarSprites != null && idx < avatarSprites.Length)
                avatarImage.sprite = avatarSprites[idx];
            else if (defaultSprite != null)
                avatarImage.sprite = defaultSprite;
        }
    }
}