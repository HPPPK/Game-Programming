/*
 * File: GlobalUIManager.cs
 *
 * Purpose:
 * Keeps shared UI panels available across all CanalTD scenes. This is intended
 * for a GlobalUIRoot object in a bootstrap scene.
 */
using System.Collections;
using TMPro;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    [Header("Panels")]
    public InstructionBookController instructionBook;
    public SettingsPanelController settingsPanel;

    [Header("Toast")]
    public GameObject toastRoot;
    public TMP_Text toastText;
    public float toastDuration = 2f;

    private Coroutine toastCoroutine;

    private void Awake()
    {
        // Prevent duplicate GlobalUIRoot objects when returning to BootstrapScene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CloseInstruction();
        CloseSettings();

        if (toastRoot != null)
        {
            toastRoot.SetActive(false);
        }
    }

    // Opens the instruction panel if closed, otherwise closes it.
    public void ToggleInstruction()
    {
        if (instructionBook == null)
        {
            Debug.LogWarning("GlobalUIManager instructionBook is not assigned.");
            return;
        }

        if (instructionBook.IsOpen())
        {
            instructionBook.CloseBook();
        }
        else
        {
            instructionBook.OpenBook();
        }
    }

    // Closes the instruction panel without affecting other UI.
    public void CloseInstruction()
    {
        if (instructionBook != null)
        {
            instructionBook.CloseBook();
        }
    }

    // Opens the settings panel if closed, otherwise closes it.
    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("GlobalUIManager settingsPanel is not assigned.");
            return;
        }

        if (settingsPanel.IsOpen())
        {
            settingsPanel.CloseSettings();
        }
        else
        {
            settingsPanel.OpenSettings();
        }
    }

    // Closes the settings panel without affecting other UI.
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.CloseSettings();
        }
    }

    // Shows a temporary toast message and hides it after toastDuration seconds.
    public void ShowToast(string message)
    {
        Debug.Log(message);

        if (toastText == null && toastRoot != null)
        {
            toastText = toastRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (toastRoot == null && toastText == null)
        {
            Debug.LogWarning("GlobalUIManager toast UI is not assigned.");
            return;
        }

        if (toastRoot != null)
        {
            toastRoot.SetActive(true);
        }

        if (toastText != null)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = message;
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(HideToastAfterDelay());
    }

    // Coroutine used by ShowToast so repeated messages reset the hide timer.
    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSeconds(toastDuration);

        if (toastRoot != null)
        {
            toastRoot.SetActive(false);
        }
        else if (toastText != null)
        {
            toastText.gameObject.SetActive(false);
        }

        toastCoroutine = null;
    }
}
