/*
 * File: CursorToolManager.cs
 *
 * Purpose:
 * This singleton controls the player's cursor/tool state. It does not replace
 * Unity's system cursor, because custom cursor hot spots can offset UI and
 * world click detection on some displays.
 *
 * Runtime behavior:
 * - EnterHammerMode() switches the internal state used by gate targeting.
 * - ExitHammerMode() leaves hammer mode.
 * - ToggleHammerMode() is intended for the HammerButton.
 * - ExitToolMode() is kept as a compatibility wrapper for existing scripts.
 *
 * Inspector setup:
 * - VisualCursorFollower is the main visual cursor controller in the scene.
 * - Do not use Cursor.SetCursor here.
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

    public bool isHammerMode = false;
    public bool IsHammerMode
    {
        get { return isHammerMode; }
    }

    void Awake()
    {
        Instance = this;
    }

    public void EnterHammerMode()
    {
        SetHammerMode(true);
    }

    public void ExitHammerMode()
    {
        SetHammerMode(false);
    }

    public void ToggleHammerMode()
    {
        SetHammerMode(!isHammerMode);
    }

    public void SetHammerMode(bool enabled)
    {
        isHammerMode = enabled;
    }

    public void ExitToolMode()
    {
        ExitHammerMode();
    }

}
