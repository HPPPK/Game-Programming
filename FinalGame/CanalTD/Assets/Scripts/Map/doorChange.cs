/*
 * File: doorChange.cs
 *
 * Purpose:
 * Implements doorChange for the map layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for doorChange within the map system.
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
 * - Verify doorChange in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;
using UnityEngine.Events;

public class doorChange : MonoBehaviour
{
    public enum DoorState
    {
        Closed,
        Open
    }

    [Header("State")]
    [SerializeField] private DoorState currentState = DoorState.Closed;

    [Header("Sprite Mode (Optional)")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Visual Mode (Optional)")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;

    [Header("Events (Optional)")]
    [SerializeField] private UnityEvent onDoorOpened;
    [SerializeField] private UnityEvent onDoorClosed;

    [Header("Interaction")]
    [SerializeField] private bool clickToToggle = true;

    public DoorState CurrentState => currentState;
    public bool IsOpen => currentState == DoorState.Open;

    private void Awake()
    {
        ApplyVisuals();
    }

    public void ToggleDoor()
    {
        SetDoorState(IsOpen ? DoorState.Closed : DoorState.Open);
    }

    private void OnMouseDown()
    {
        if (!clickToToggle)
        {
            return;
        }

        ToggleDoor();
    }

    public void OpenDoor()
    {
        SetDoorState(DoorState.Open);
    }

    public void CloseDoor()
    {
        SetDoorState(DoorState.Closed);
    }

    public void SetDoorState(DoorState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        ApplyVisuals();

        if (IsOpen)
        {
            onDoorOpened?.Invoke();
        }
        else
        {
            onDoorClosed?.Invoke();
        }
    }

    private void ApplyVisuals()
    {
        bool open = IsOpen;

        if (targetRenderer != null)
        {
            targetRenderer.sprite = open ? openSprite : closedSprite;
        }

        if (closedVisual != null)
        {
            closedVisual.SetActive(!open);
        }

        if (openVisual != null)
        {
            openVisual.SetActive(open);
        }
    }
}
