/*
 * File: SimpleFrameAnimator.cs
 *
 * Purpose:
 * Implements SimpleFrameAnimator for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for SimpleFrameAnimator within the ui system.
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
 * - Verify SimpleFrameAnimator in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleFrameAnimator : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] frames;
    public float framesPerSecond = 8f;

    [Header("Playback")]
    public bool loop = true;
    public bool playOnStart = true;
    public bool randomStartFrame = true;

    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool isPlaying = false;

    /// <summary>
    /// Finds and stores simple frame animator references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Sets up simple frame animator when this scene object starts running.
    /// </summary>
    private void Start()
    {
        if (!HasFrames())
        {
            return;
        }

        if (randomStartFrame)
        {
            currentFrameIndex = Random.Range(0, frames.Length);
            ApplyCurrentFrame();
        }
        else
        {
            ResetToFirstFrame();
        }

        if (playOnStart)
        {
            Play();
        }
    }

    /// <summary>
    /// Checks simple frame animator input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (!isPlaying || !HasFrames() || spriteRenderer == null)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.01f, framesPerSecond);

        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer -= frameDuration;
        AdvanceFrame();
    }

    // Starts or resumes frame animation.
    /// <summary>
    /// Plays play in the current scene context.
    /// </summary>
    public void Play()
    {
        if (!HasFrames() || spriteRenderer == null)
        {
            return;
        }

        isPlaying = true;
    }

    // Pauses frame animation on the current frame.
    /// <summary>
    /// Stops stop and disables its related gameplay flow.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }

    // Moves animation back to frame 0 and displays it immediately.
    /// <summary>
    /// Resets to first frame for a new turn, wave, player, scene, or match state.
    /// </summary>
    public void ResetToFirstFrame()
    {
        currentFrameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    /// <summary>
    /// Advances frame to the next turn, wave, player, or tutorial step.
    /// </summary>
    private void AdvanceFrame()
    {
        currentFrameIndex++;

        if (currentFrameIndex >= frames.Length)
        {
            if (loop)
            {
                currentFrameIndex = 0;
            }
            else
            {
                currentFrameIndex = frames.Length - 1;
                Stop();
            }
        }

        ApplyCurrentFrame();
    }

    /// <summary>
    /// Applies current frame to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyCurrentFrame()
    {
        if (!HasFrames() || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = frames[currentFrameIndex];
    }

    /// <summary>
    /// Checks whether frames is present before the code depends on it.
    /// </summary>
    private bool HasFrames()
    {
        return frames != null && frames.Length > 0;
    }
}
