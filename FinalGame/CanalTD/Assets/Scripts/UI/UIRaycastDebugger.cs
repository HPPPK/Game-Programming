/*
 * File: UIRaycastDebugger.cs
 *
 * Purpose:
 * Implements UIRaycastDebugger for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for UIRaycastDebugger within the ui system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify UIRaycastDebugger in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebugger : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("UIRaycastDebugger AWAKE");
    }

    private void OnEnable()
    {
        Debug.Log("UIRaycastDebugger ENABLED");
    }

    private void Start()
    {
        Debug.Log("UIRaycastDebugger START");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Debug.Log("Mouse clicked: " + Input.mousePosition);

        if (EventSystem.current == null)
        {
            Debug.LogWarning("UIRaycastDebugger: EventSystem.current is null");
            return;
        }

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        Debug.Log("UI Raycast hit count = " + results.Count);

        foreach (var r in results)
        {
            Debug.Log("UI hit: " + r.gameObject.name);
        }
    }
}