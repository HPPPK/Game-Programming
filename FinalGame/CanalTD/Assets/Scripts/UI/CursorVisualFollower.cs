/*
 * File: CursorVisualFollower.cs
 *
 * Purpose:
 * Shows a visual cursor sprite as a UI Image that follows the mouse. The real
 * system cursor remains visible and is still used for all clicking, so this
 * avoids Cursor.SetCursor hot spot offset problems.
 *
 * Unity setup:
 * - Create ScreenCanvas/CursorVisual.
 * - Add an Image component.
 * - Disable Raycast Target on that Image.
 * - Attach this script to CursorVisual or another UI object.
 * - Assign cursorImage, cursorGraphic, and hammerCursorSprite.
 */
using UnityEngine;
using UnityEngine.UI;

public class CursorVisualFollower : MonoBehaviour
{
    public RectTransform cursorImage;
    public Image cursorGraphic;
    public Sprite normalCursorSprite;
    public Sprite hammerCursorSprite;
    public bool showCustomVisual = true;

    void Awake()
    {
        if (cursorImage == null)
        {
            cursorImage = GetComponent<RectTransform>();
        }

        if (cursorGraphic == null)
        {
            cursorGraphic = GetComponent<Image>();
        }

        if (cursorGraphic != null)
        {
            cursorGraphic.raycastTarget = false;
        }
    }

    void Start()
    {
        RefreshVisual();
    }

    void Update()
    {
        if (cursorImage != null)
        {
            cursorImage.position = Input.mousePosition;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (cursorImage == null)
        {
            return;
        }

        if (!showCustomVisual)
        {
            SetVisualVisible(false);
            return;
        }

        bool isHammerMode =
            CursorToolManager.Instance != null &&
            CursorToolManager.Instance.IsHammerMode;

        if (isHammerMode)
        {
            SetCursorSprite(hammerCursorSprite);
            SetVisualVisible(hammerCursorSprite != null);
            return;
        }

        if (normalCursorSprite != null)
        {
            SetCursorSprite(normalCursorSprite);
            SetVisualVisible(true);
        }
        else
        {
            SetVisualVisible(false);
        }
    }

    private void SetCursorSprite(Sprite sprite)
    {
        if (cursorGraphic == null)
        {
            return;
        }

        if (cursorGraphic.sprite != sprite)
        {
            cursorGraphic.sprite = sprite;
        }
    }

    private void SetVisualVisible(bool visible)
    {
        if (cursorGraphic != null)
        {
            cursorGraphic.enabled = visible;
        }
    }
}
