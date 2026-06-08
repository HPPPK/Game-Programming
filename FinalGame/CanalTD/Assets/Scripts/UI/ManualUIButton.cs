/*
 * File: ManualUIButton.cs
 *
 * Purpose:
 * Implements ManualUIButton for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ManualUIButton within the ui system.
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
 * - Verify ManualUIButton in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;
using UnityEngine.Events;

public class ManualUIButton : MonoBehaviour
{
    public RectTransform targetRect;
    public Camera uiCamera;
    public UnityEvent onClick;
    public bool debugLog = true;

    void Awake()
    {
        ResolveTargetRect();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        ResolveTargetRect();

        if (targetRect == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(name + " ManualUIButton has no target RectTransform.");
            }

            return;
        }

        Vector2 mousePosition = Input.mousePosition;
        bool hit = RectTransformUtility.RectangleContainsScreenPoint(
            targetRect,
            mousePosition,
            uiCamera
        );

        if (debugLog)
        {
            Debug.Log(
                "ManualUIButton " + name +
                " mouse = " + mousePosition +
                " hit = " + hit
            );
        }

        if (hit)
        {
            onClick?.Invoke();
        }
    }

    private void ResolveTargetRect()
    {
        if (targetRect != null)
        {
            return;
        }

        targetRect = GetComponent<RectTransform>();
    }
}
