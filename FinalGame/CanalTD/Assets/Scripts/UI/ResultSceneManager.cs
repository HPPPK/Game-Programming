/*
 * File: ResultSceneManager.cs
 *
 * Purpose:
 * Builds the ResultScene ranking UI from GameResultData and handles Play Again /
 * Back To Home button actions.
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
    public void PlayAgain()
    {
        GameResultData.Clear();
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // Kept for older button references; it now behaves the same as PlayAgain.
    public void NewGame()
    {
        PlayAgain();
    }

    // Returns from the result screen to HomeScene.
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Kept for older button references; it now behaves the same as PlayAgain.
    public void RestartGame()
    {
        PlayAgain();
    }

    // Kept for older button references; it now returns to HomeScene.
    public void QuitGame()
    {
        BackToHome();
    }
}
