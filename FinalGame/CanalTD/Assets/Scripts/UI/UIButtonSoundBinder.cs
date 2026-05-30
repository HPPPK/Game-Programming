/*
 * File: UIButtonSoundBinder.cs
 *
 * Purpose:
 * Automatically binds UI click sound to all buttons under this GameObject.
 */

using UnityEngine;
using UnityEngine.UI;

public class UIButtonSoundBinder : MonoBehaviour
{
    private Button[] buttons;

    private void Start()
    {
        BindAllButtons();
    }

    private void BindAllButtons()
    {
        buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.AddListener(PlayClickSound);
        }
    }

    private void OnDestroy()
    {
        if (buttons == null)
        {
            return;
        }

        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiClick();
        }
    }
}