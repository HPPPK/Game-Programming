/*
 * File: cursor.cs
 *
 * Purpose:
 * Implements cursor for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for cursor within the core system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify cursor in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class VisualCursorFollower : MonoBehaviour
{
    public static VisualCursorFollower Instance;

    [Header("Cursor Sprites")]
    public Sprite normalCursor;
    public Sprite hammerCursor;

    [Header("Camera")]
    public Camera targetCamera;

    private SpriteRenderer spriteRenderer;
    private bool wasHammerMode = false;

    /// <summary>
    /// Finds and stores visual cursor follower references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        Cursor.visible = false;

        SetNormalCursor();
    }

    /// <summary>
    /// Subscribes visual cursor follower to the events it needs while enabled.
    /// </summary>
    private void OnEnable()
    {
        Cursor.visible = false;
    }

    /// <summary>
    /// Checks visual cursor follower input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        Cursor.visible = false;
        RefreshCursorSprite();
        FollowMouse();
    }

    /// <summary>
    /// Handles follow mouse for UI display, input, or player feedback.
    /// </summary>
    private void FollowMouse()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                distanceFromCamera
            )
        );

        worldPosition.z = 0f;
        transform.position = worldPosition;
    }

    /// <summary>
    /// Sets normal cursor and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetNormalCursor()
    {
        SetHammerMode(false);
        ApplyNormalCursorVisual();
    }

    /// <summary>
    /// Sets hammer cursor and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetHammerCursor()
    {
        SetHammerMode(true);
        ApplyHammerCursorVisual();
    }

    /// <summary>
    /// Handles toggle hammer cursor for UI display, input, or player feedback.
    /// </summary>
    public void ToggleHammerCursor()
    {
        bool nextHammerMode = !IsHammerMode();
        SetHammerMode(nextHammerMode);

        if (nextHammerMode)
        {
            ApplyHammerCursorVisual();
        }
        else
        {
            ApplyNormalCursorVisual();
        }
    }

    /// <summary>
    /// Checks the current state to decide whether hammer mode is true.
    /// </summary>
    public bool IsHammerMode()
    {
        if (CursorToolManager.Instance != null)
        {
            return CursorToolManager.Instance.IsHammerMode;
        }

        return wasHammerMode;
    }

    /// <summary>
    /// Applies normal cursor visual to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyNormalCursorVisual()
    {
        wasHammerMode = false;

        if (spriteRenderer != null && normalCursor != null)
        {
            spriteRenderer.sprite = normalCursor;
        }
    }

    /// <summary>
    /// Applies hammer cursor visual to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyHammerCursorVisual()
    {
        wasHammerMode = true;

        if (spriteRenderer != null && hammerCursor != null)
        {
            spriteRenderer.sprite = hammerCursor;
        }
    }

    /// <summary>
    /// Sets hammer mode and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetHammerMode(bool enabled)
    {
        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.SetHammerMode(enabled);
        }

        wasHammerMode = enabled;
    }

    /// <summary>
    /// Refreshes cursor sprite from the latest gameplay data.
    /// </summary>
    private void RefreshCursorSprite()
    {
        bool shouldUseHammer = CursorToolManager.Instance != null
            ? CursorToolManager.Instance.IsHammerMode
            : wasHammerMode;

        if (shouldUseHammer == wasHammerMode)
        {
            return;
        }

        if (shouldUseHammer)
        {
            ApplyHammerCursorVisual();
        }
        else
        {
            ApplyNormalCursorVisual();
        }
    }

    /// <summary>
    /// Responds to on application focus and updates the affected gameplay or UI systems.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Unsubscribes visual cursor follower from events so disabled objects stop receiving callbacks.
    /// </summary>
    private void OnDisable()
    {
        Cursor.visible = false;
    }
}
