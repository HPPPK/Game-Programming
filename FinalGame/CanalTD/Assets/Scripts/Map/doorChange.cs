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
