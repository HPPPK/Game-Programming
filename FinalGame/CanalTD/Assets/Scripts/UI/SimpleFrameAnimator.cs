/*
 * File: SimpleFrameAnimator.cs
 *
 * Purpose:
 * Lightweight sprite frame animation for HomeScene decoration characters,
 * enemies, or props. It switches SpriteRenderer.sprite over time without using
 * Unity Animator.
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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

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
    public void Play()
    {
        if (!HasFrames() || spriteRenderer == null)
        {
            return;
        }

        isPlaying = true;
    }

    // Pauses frame animation on the current frame.
    public void Stop()
    {
        isPlaying = false;
    }

    // Moves animation back to frame 0 and displays it immediately.
    public void ResetToFirstFrame()
    {
        currentFrameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

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

    private void ApplyCurrentFrame()
    {
        if (!HasFrames() || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = frames[currentFrameIndex];
    }

    private bool HasFrames()
    {
        return frames != null && frames.Length > 0;
    }
}
