/*
 * File: ResultSceneManager.cs
 *
 * Purpose:
 * Builds the ResultScene ranking UI from GameResultData and handles New Game /
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
    public string gameSceneName = "GameScene";
    public string homeSceneName = "HomeScene";

    private void Start()
    {
        BuildResultList();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(NewGame);
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

    // Starts a fresh game by clearing old result data and loading GameScene.
    public void NewGame()
    {
        GameResultData.Clear();
        SceneManager.LoadScene(gameSceneName);
    }

    // Returns from the result screen to HomeScene.
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Kept for older button references; it now behaves the same as NewGame.
    public void RestartGame()
    {
        NewGame();
    }

    // Kept for older button references; it now returns to HomeScene.
    public void QuitGame()
    {
        BackToHome();
    }
}
