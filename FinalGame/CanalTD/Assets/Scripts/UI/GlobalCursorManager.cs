/*
 * File: GlobalCursorManager.cs
 *
 * Purpose:
 * Implements GlobalCursorManager for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GlobalCursorManager within the ui system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Verify GlobalCursorManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;
using UnityEngine.UI;

public class GlobalCursorManager : MonoBehaviour
{
    public static GlobalCursorManager Instance { get; private set; }

    [Header("Cursor UI")]
    public RectTransform cursorImage;
    public Image cursorGraphic;

    /// <summary>
    /// Finds and stores global cursor manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        // Keep exactly one global cursor object across scene loads.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            Debug.LogWarning("GlobalCursorManager should be placed on a root GameObject before DontDestroyOnLoad.");
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Sets up global cursor manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        Cursor.visible = false;
        MakeCursorIgnoreRaycasts();
    }

    /// <summary>
    /// Checks global cursor manager input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        Cursor.visible = false;

        if (cursorImage != null)
        {
            cursorImage.position = Input.mousePosition;
        }
    }

    // Ensures the cursor image never blocks buttons, sliders, or other UI clicks.
    /// <summary>
    /// Handles make cursor ignore raycasts for UI display, input, or player feedback.
    /// </summary>
    private void MakeCursorIgnoreRaycasts()
    {
        if (cursorGraphic == null && cursorImage != null)
        {
            cursorGraphic = cursorImage.GetComponent<Image>();
        }

        if (cursorGraphic != null)
        {
            cursorGraphic.raycastTarget = false;
        }
    }
}
