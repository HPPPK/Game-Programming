/*
 * File: CursorVisualFollower.cs
 *
 * Purpose:
 * Implements CursorVisualFollower for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CursorVisualFollower within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CursorVisualFollower in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
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
        Cursor.visible = false;
        RefreshVisual();
    }

    void Update()
    {
        Cursor.visible = false;

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
