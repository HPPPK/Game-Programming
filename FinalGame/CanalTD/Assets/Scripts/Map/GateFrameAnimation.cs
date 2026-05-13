/*
 * File: GateFrameAnimation.cs
 *
 * Purpose:
 * This script controls one gate's visual animation, click handling, and blocking
 * state. It is attached directly to a gate GameObject with a SpriteRenderer and
 * Collider2D.
 *
 * Main gameplay flow:
 * 1. Start() caches SpriteRenderer/Collider2D and applies the correct starting
 *    sprite based on isBlocking.
 * 2. Update() checks mouse clicks and calls CheckClick().
 * 3. CheckClick() only accepts clicks when the cursor is in hammer mode and
 *    GateTargetingManager is currently targeting gates.
 * 4. The gate reports itself to GateTargetingManager.SelectGate(this).
 * 5. OpenGate() plays the opening animation and makes the gate non-blocking.
 * 6. LockGate() plays the closing animation or applies the blocking visual.
 * 7. When the gate changes, PathGraphState.MarkDirty() tells path systems to
 *    refresh cached paths.
 *
 * Inspector setup:
 * - frames should contain the ordered animation sprites.
 * - frameRate controls time between animation frames.
 * - isBlocking defines whether the gate begins closed/blocking.
 * - isLocked prevents the gate from being opened.
 *
 * Dependency notes:
 * - GateTargetingManager controls selection, highlighting, confirm, and cancel.
 * - CursorToolManager controls whether the player is allowed to click gates.
 * - PathEdge checks IsBlocking() to know if a path through this gate is open.
 */
using UnityEngine;
using System.Collections;

public class GateFrameAnimation : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] frames;
    public float frameRate = 0.05f;

    [Header("Gate State")]
    public bool isBlocking = true;
    public bool isLocked = false;

    [Header("Path Link")]

    private bool isPlaying = false;
    private SpriteRenderer sr;
    private Collider2D col;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.sprite = isBlocking ? frames[0] : frames[frames.Length - 1];
            sr.enabled = isBlocking;
        }

        Debug.Log(name + " starts " + (isBlocking ? "BLOCKED" : "UNBLOCKED"));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckClick();
        }
    }

    void CheckClick()
    {
        if (isPlaying) return;
        if (col == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos = new Vector2(mouseWorld.x, mouseWorld.y);

        if (!col.OverlapPoint(mousePos))
        {
            return;
        }

        if (GateTargetingManager.Instance == null)
        {
            Debug.LogWarning("No GateTargetingManager found.");
            return;
        }

        if (!GateTargetingManager.Instance.IsHammerToolActive())
        {
            Debug.Log(name + " clicked, but gate targeting cursor is OFF.");
            GateTargetingManager.Instance.ShowToast("Select the hammer tool first.");
            return;
        }

        if (!GateTargetingManager.Instance.IsTargetingGate())
        {
            Debug.Log(name + " clicked, but not in gate targeting mode.");
            return;
        }

        // This only selects the gate. The real Open Gate / Lock Gate effect is triggered by the Confirm button.
        GateTargetingManager.Instance.SelectGate(this);
    }

    void OnMouseEnter()
    {
        if (GateTargetingManager.Instance != null)
        {
            GateTargetingManager.Instance.PreviewGate(this, true);
        }
    }

    void OnMouseExit()
    {
        if (GateTargetingManager.Instance != null)
        {
            GateTargetingManager.Instance.PreviewGate(this, false);
        }
    }

    public void SetHighlight(bool highlighted, Color color)
    {
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr == null) return;

        sr.color = color;

        if (highlighted)
        {
            sr.enabled = true;
        }
        else
        {
            sr.enabled = isBlocking;
        }
    }

    public bool CanOpen()
    {
        return !isLocked && isBlocking && !isPlaying;
    }

    public bool OpenGate()
    {
        if (isPlaying)
        {
            Debug.LogWarning(name + " is already changing.");
            return false;
        }

        if (isLocked)
        {
            Debug.LogWarning(name + " is locked and cannot be opened.");
            return false;
        }

        if (!isBlocking)
        {
            Debug.LogWarning(name + " is already open.");
            return false;
        }

        isBlocking = false;
        PathGraphState.MarkDirty();
        StartCoroutine(UnblockGate());
        Debug.Log("OpenGate opened: " + name);
        return true;
    }

    public bool LockGate()
    {
        if (isPlaying)
        {
            Debug.LogWarning(name + " is already changing and cannot be locked right now.");
            return false;
        }

        if (isLocked)
        {
            Debug.LogWarning(name + " is already locked.");
            return false;
        }

        if (!isBlocking)
        {
            isBlocking = true;
            PathGraphState.MarkDirty();
            StartCoroutine(BlockGate());
        }
        else
        {
            ApplyBlockingVisual();
            PathGraphState.MarkDirty();
            StartCoroutine(FinishGateTargetingNextFrame());
        }

        Debug.Log("LockGate built/closed " + name);
        return true;
    }

    public void UnlockGate()
    {
        isLocked = false;
        Debug.Log(name + " is now UNLOCKED");
    }

    IEnumerator UnblockGate()
    {
        isPlaying = true;

        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.enabled = true;

            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(frameRate);
            }

            sr.enabled = false;
        }

        isBlocking = false;
        isPlaying = false;

        Debug.Log(name + " is now UNBLOCKED");

        FinishGateTargeting();
    }

    IEnumerator BlockGate()
    {
        isPlaying = true;

        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.enabled = true;

            for (int i = frames.Length - 1; i >= 0; i--)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(frameRate);
            }
        }

        ApplyBlockingVisual();
        isPlaying = false;

        FinishGateTargeting();
    }

    IEnumerator FinishGateTargetingNextFrame()
    {
        yield return null;
        FinishGateTargeting();
    }

    void ApplyBlockingVisual()
    {
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr == null) return;

        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
        }

        sr.enabled = true;
        sr.color = Color.white;
        isBlocking = true;
    }

    void FinishGateTargeting()
    {
        if (GateTargetingManager.Instance != null &&
            GateTargetingManager.Instance.IsTargetingGate())
        {
            GateTargetingManager.Instance.ExitGateTargetMode();
        }

        // GateTargetingManager exits the cursor/tool state.
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }
}
