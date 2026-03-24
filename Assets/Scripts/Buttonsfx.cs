using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    public enum SFXType { ButtonPress, UIClick, Fanfare }

    [SerializeField] private SFXType sfxType = SFXType.ButtonPress;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (AudioManager.Instance == null) return;

        switch (sfxType)
        {
            case SFXType.ButtonPress: AudioManager.Instance.PlayButtonPress(); break;
            case SFXType.UIClick:     AudioManager.Instance.PlayUIClick();     break;
            case SFXType.Fanfare:     AudioManager.Instance.PlayFanfare();     break;
        }
    }
}