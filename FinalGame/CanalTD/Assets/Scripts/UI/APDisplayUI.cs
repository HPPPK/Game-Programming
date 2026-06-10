/*
 * File: APDisplayUI.cs
 *
 * Purpose:
 * Implements APDisplayUI for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for APDisplayUI within the ui system.
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
 * - Verify APDisplayUI in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;

public class APDisplayUI : MonoBehaviour
{
    public TurnManager turnManager;
    public TextMeshProUGUI apText;

    /// <summary>
    /// Finds and stores AP display UI references before scene gameplay begins.
    /// </summary>
    void Awake()
    {
        if (apText == null)
        {
            apText = GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Checks AP display UI input, timing, animation, or UI state once per frame.
    /// </summary>
    void Update()
    {
        Refresh();
    }

    /// <summary>
    /// Refreshes this display from the latest gameplay data.
    /// </summary>
    public void Refresh()
    {
        TurnManager manager = turnManager != null ? turnManager : TurnManager.Instance;
        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(manager);

        if (manager == null || apText == null)
        {
            return;
        }

        string prefix = turnSource != null && !turnSource.CanHumanAct && TurnSourceResolver.IsAIPrototypeActive()
            ? "AI AP: "
            : "AP: ";

        apText.text = prefix + manager.currentAP + " / " + manager.maxAP;
    }
}
