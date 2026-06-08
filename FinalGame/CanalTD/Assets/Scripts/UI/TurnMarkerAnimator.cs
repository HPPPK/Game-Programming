/*
 * File: TurnMarkerAnimator.cs
 *
 * Purpose:
 * Implements TurnMarkerAnimator for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TurnMarkerAnimator within the ui system.
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
 * - Verify TurnMarkerAnimator in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;

public class TurnMarkerAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float floatHeight = 0.15f;
    public float floatSpeed = 2f;
    public float scaleAmount = 0.15f;
    public float scaleSpeed = 2f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isAnimating = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (!isAnimating) return;

        // Float animation
        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = originalPosition + new Vector3(0, floatOffset, 0);

        // Scale animation
        float scaleOffset = 1 + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        transform.localScale = originalScale * scaleOffset;
    }

    public void Show()
    {
        isAnimating = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isAnimating = false;
        gameObject.SetActive(false);
    }

    public void SetMarkerColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void ResetMarkerColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
