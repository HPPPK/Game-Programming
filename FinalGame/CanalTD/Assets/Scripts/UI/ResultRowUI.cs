/*
 * File: ResultRowUI.cs
 *
 * Purpose:
 * Implements ResultRowUI for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ResultRowUI within the ui system.
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
 * - Verify ResultRowUI in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;

public class ResultRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI moneyText;

    /// <summary>
    /// Sets data and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetData(PlayerResultEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (rankText != null)
        {
            rankText.text = entry.rank.ToString();
        }

        if (playerText != null)
        {
            playerText.text = entry.isEliminated ? entry.displayName + " - 已淘汰" : entry.displayName;
        }

        if (scoreText != null)
        {
            scoreText.text = entry.score.ToString();
        }

        if (hpText != null)
        {
            hpText.text = entry.castleHp.ToString();
        }

        if (moneyText != null)
        {
            moneyText.text = entry.money.ToString();
        }
    }
}
