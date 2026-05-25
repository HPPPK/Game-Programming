/*
 * File: BootstrapSceneLoader.cs
 *
 * Purpose:
 * Loads HomeScene after GlobalUIRoot has had a frame to initialize and persist.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapSceneLoader : MonoBehaviour
{
    [Header("Scene")]
    public string homeSceneName = "HomeScene";

    [Header("Loading")]
    public bool loadOnStart = true;

    private void Start()
    {
        if (loadOnStart)
        {
            StartCoroutine(LoadHomeAfterGlobalUIInit());
        }
    }

    // Waits one frame so Awake/Start on GlobalUIRoot can run before changing scenes.
    private IEnumerator LoadHomeAfterGlobalUIInit()
    {
        yield return null;
        SceneManager.LoadScene(homeSceneName);
    }
}
