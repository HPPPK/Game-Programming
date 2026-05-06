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
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

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

        ToggleGate();
    }

    void OnMouseEnter()
    {
        SetHighlight(true, hoverColor);
    }

    void OnMouseExit()
    {
        SetHighlight(false, normalColor);
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

    public bool ToggleGate()
    {
        if (isBlocking)
        {
            return OpenGate();
        }

        return LockGate();
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
            StartCoroutine(BlockGate());
        }
        else
        {
            ApplyBlockingVisual();
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
        sr.color = normalColor;
        isBlocking = true;
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
