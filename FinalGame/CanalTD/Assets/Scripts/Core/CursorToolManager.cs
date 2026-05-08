/*
 * File: CursorToolManager.cs
 *
 * Purpose:
 * This singleton controls the player's cursor/tool state. The normal state uses
 * the default cursor, while hammer mode uses a hammer cursor so the player can
 * clearly see that they are choosing a gate target.
 *
 * Runtime behavior:
 * - Start() applies the normal cursor at scene load.
 * - EnterHammerMode() switches the internal state and applies the hammer cursor.
 * - ExitToolMode() leaves hammer mode and restores the normal cursor.
 * - The script avoids repeatedly applying the same cursor every frame.
 *
 * Inspector setup:
 * - normalCursor is optional; null means Unity's default cursor.
 * - hammerCursor should be assigned for gate-targeting feedback.
 * - Cursor texture import settings matter: Texture Type Cursor is safest.
 * - hot spot values define which pixel of the cursor counts as the click point.
 *
 * Dependency notes:
 * - CardDrawManager enters hammer mode when a gate card starts targeting.
 * - GateFrameAnimation checks isHammerMode before accepting a gate click.
 * - GateTargetingManager exits tool mode when targeting finishes or is cancelled.
 */
using UnityEngine;

public class CursorToolManager : MonoBehaviour
{
    public static CursorToolManager Instance;

    [Header("Cursor Textures")]
    [Tooltip("Cursor textures must be imported as Texture Type Cursor, or a compatible Default texture with Read/Write Enabled, Alpha Is Transparency enabled, Generate Mip Maps disabled, and RGBA32 format.")]
    public Texture2D normalCursor;
    [Tooltip("Cursor textures must be imported as Texture Type Cursor, or a compatible Default texture with Read/Write Enabled, Alpha Is Transparency enabled, Generate Mip Maps disabled, and RGBA32 format.")]
    public Texture2D hammerCursor;

    [Header("Hot Spots")]
    public Vector2 normalHotSpot = Vector2.zero;
    public Vector2 hammerHotSpot = Vector2.zero;

    public bool isHammerMode = false;

    private enum CursorState
    {
        Default,
        Hammer
    }

    private CursorState currentCursorState = CursorState.Default;
    private bool hasAppliedCursor = false;
    private bool warnedMissingHammerCursor = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ApplyNormalCursor();
    }

    public void EnterHammerMode()
    {
        if (isHammerMode && hasAppliedCursor && currentCursorState == CursorState.Hammer)
        {
            return;
        }

        isHammerMode = true;
        ApplyHammerCursor();
    }

    public void ExitToolMode()
    {
        if (!isHammerMode && hasAppliedCursor && currentCursorState == CursorState.Default)
        {
            return;
        }

        isHammerMode = false;
        ApplyNormalCursor();
    }

    private void ApplyNormalCursor()
    {
        if (hasAppliedCursor && currentCursorState == CursorState.Default)
        {
            return;
        }

        Cursor.SetCursor(normalCursor, normalHotSpot, CursorMode.Auto);
        currentCursorState = CursorState.Default;
        hasAppliedCursor = true;
    }

    private void ApplyHammerCursor()
    {
        if (hammerCursor == null)
        {
            if (!warnedMissingHammerCursor)
            {
                Debug.LogWarning(
                    "Hammer cursor texture is not assigned. Falling back to the default cursor. " +
                    "Cursor texture import settings should be: Texture Type Cursor or compatible Default texture, " +
                    "Read/Write Enabled true, Alpha Is Transparency true, Generate Mip Maps false, Format RGBA32."
                );

                warnedMissingHammerCursor = true;
            }

            ApplyNormalCursor();
            return;
        }

        if (hasAppliedCursor && currentCursorState == CursorState.Hammer)
        {
            return;
        }

        // Unity cursor texture import requirements:
        // Texture Type: Cursor, or a compatible Default texture
        // Read/Write Enabled: true
        // Alpha Is Transparency: true
        // Generate Mip Maps: false
        // Format: RGBA32
        Cursor.SetCursor(hammerCursor, hammerHotSpot, CursorMode.Auto);
        currentCursorState = CursorState.Hammer;
        hasAppliedCursor = true;
    }
}
