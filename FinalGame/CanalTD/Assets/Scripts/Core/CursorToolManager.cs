/*
 * File: CursorToolManager.cs
 *
 * Purpose:
 * Implements CursorToolManager for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CursorToolManager within the core system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Verify CursorToolManager in the scene or prefab where it is used and confirm the main happy path still works.
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
