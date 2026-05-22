/*
 * File: cursor.cs
 *
 * Purpose:
 * Displays a custom visual cursor sprite that follows the mouse. It can switch
 * between normal and hammer visuals depending on the current tool mode.
 *
 * Notes:
 * The class name is VisualCursorFollower even though the file name is cursor.cs,
 * matching the existing project setup.
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
