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

    public void NewGame()
    {
        GameResultData.Clear();
        SceneManager.LoadScene(gameSceneName);
    }

    public void BackToHome()
    {
        Debug.Log("HomeScene is not implemented yet.");
    }

    public void RestartGame()
    {
        NewGame();
    }

    public void QuitGame()
    {
        BackToHome();
    }
}
