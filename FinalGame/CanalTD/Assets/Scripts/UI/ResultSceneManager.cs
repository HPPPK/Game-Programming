/*
 * File: ResultSceneManager.cs
 *
 * Purpose:
 * Implements ResultSceneManager for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ResultSceneManager within the ui system.
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
 * - Verify ResultSceneManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ResultSceneManager : MonoBehaviour
{
    [Header("Result List")]
    public Transform resultListParent;
    public GameObject resultRowPrefab;

    [Header("Texts")]
    public TextMeshProUGUI winnerText;

    [Header("Buttons")]
    public Button restartButton;
    public Button quitButton;

    [Header("Scenes")]
    public string modeSelectSceneName = "ModeSelectScene";
    public string homeSceneName = "HomeScene";

    /// <summary>
    /// Sets up result scene manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        BuildResultList();
        AudioManager.Instance?.PlayVictory();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(PlayAgain);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(BackToHome);
        }
    }

    // Rebuilds the final ranking rows from the static result data.
    /// <summary>
    /// Builds result list from configured scene objects and runtime state.
    /// </summary>
    private void BuildResultList()
    {
        List<PlayerResultEntry> results = GameResultData.Results;

        if (winnerText != null)
        {
            winnerText.text = results != null && results.Count > 0
                ? "Top Rank: " + results[0].displayName
                : "Top Rank: None";
        }

        if (resultListParent == null || resultRowPrefab == null || results == null)
        {
            return;
        }

        for (int i = resultListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(resultListParent.GetChild(i).gameObject);
        }

        foreach (PlayerResultEntry entry in results)
        {
            GameObject rowObject = Instantiate(resultRowPrefab, resultListParent);
            ResultRowUI rowUI = rowObject.GetComponent<ResultRowUI>();

            if (rowUI != null)
            {
                rowUI.SetData(entry);
            }
        }
    }

    // Returns to ModeSelectScene so the player can configure and start another match.
    /// <summary>
    /// Plays again in the current scene context.
    /// </summary>
    public void PlayAgain()
    {
        GameResultData.Clear();
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // Kept for older button references; it now behaves the same as PlayAgain.
    /// <summary>
    /// Handles new game for UI display, input, or player feedback.
    /// </summary>
    public void NewGame()
    {
        PlayAgain();
    }

    // Returns from the result screen to HomeScene.
    /// <summary>
    /// Handles back to home for UI display, input, or player feedback.
    /// </summary>
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Kept for older button references; it now behaves the same as PlayAgain.
    /// <summary>
    /// Handles restart game for UI display, input, or player feedback.
    /// </summary>
    public void RestartGame()
    {
        PlayAgain();
    }

    // Kept for older button references; it now returns to HomeScene.
    /// <summary>
    /// Handles quit game for UI display, input, or player feedback.
    /// </summary>
    public void QuitGame()
    {
        BackToHome();
    }
}
