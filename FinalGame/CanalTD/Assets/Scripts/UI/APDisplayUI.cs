/*
 * File: APDisplayUI.cs
 *
 * Purpose:
 * Legacy compatibility component for old AP UI in the ui layer of CanalTD.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Keep old scene references valid without showing AP as a final gameplay rule.
 * - Hide the attached AP text if it remains assigned in a scene.
 * - Avoid changing Unity scene references during final cleanup.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Disables or clears legacy AP text so the final UI does not present AP as a rule.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify any object using APDisplayUI no longer shows AP in the final scene flow.
 * - Confirm turn/card-action feedback remains readable through the normal HUD and toasts.
 */
using TMPro;
using UnityEngine;

public class APDisplayUI : MonoBehaviour
{
    public TurnManager turnManager;
    public TextMeshProUGUI apText;

    /// <summary>
    /// Finds and stores legacy display references before scene gameplay begins.
    /// </summary>
    void Awake()
    {
        if (apText == null)
        {
            apText = GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Keeps the legacy AP display hidden if the component remains in a scene.
    /// </summary>
    void Update()
    {
        Refresh();
    }

    /// <summary>
    /// Clears or hides this legacy AP display.
    /// </summary>
    public void Refresh()
    {
        if (apText == null)
        {
            return;
        }

        apText.text = "";
        apText.gameObject.SetActive(false);
    }
}
