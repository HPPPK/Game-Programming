/*
 * File: cursor.cs
 *
 * Purpose:
 * Implements cursor for the core layer of Rail Rumble and supports the playable vertical slice of the project.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
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

    private void OnEnable()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        Cursor.visible = false;
        RefreshCursorSprite();
        FollowMouse();
    }

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

    public void SetNormalCursor()
    {
        SetHammerMode(false);
        ApplyNormalCursorVisual();
    }

    public void SetHammerCursor()
    {
        SetHammerMode(true);
        ApplyHammerCursorVisual();
    }

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

    public bool IsHammerMode()
    {
        if (CursorToolManager.Instance != null)
        {
            return CursorToolManager.Instance.IsHammerMode;
        }

        return wasHammerMode;
    }

    private void ApplyNormalCursorVisual()
    {
        wasHammerMode = false;

        if (spriteRenderer != null && normalCursor != null)
        {
            spriteRenderer.sprite = normalCursor;
        }
    }

    private void ApplyHammerCursorVisual()
    {
        wasHammerMode = true;

        if (spriteRenderer != null && hammerCursor != null)
        {
            spriteRenderer.sprite = hammerCursor;
        }
    }

    private void SetHammerMode(bool enabled)
    {
        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.SetHammerMode(enabled);
        }

        wasHammerMode = enabled;
    }

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

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        Cursor.visible = false;
    }
}
