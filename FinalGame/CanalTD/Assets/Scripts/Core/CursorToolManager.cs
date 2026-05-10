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
 * - ExitToolMode() is kept as a compatibility wrapper for existing scripts.
 *
 * Inspector setup:
 * - Use CursorVisualFollower on a UI Image to show a visual hammer cursor.
 * - Keep the actual system cursor visible for accurate clicking.
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
        isHammerMode = true;
    }

    public void ExitHammerMode()
    {
        isHammerMode = false;
    }

    public void ExitToolMode()
    {
        ExitHammerMode();
    }

}
