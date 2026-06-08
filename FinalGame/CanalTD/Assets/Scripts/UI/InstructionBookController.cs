/*
 * File: InstructionBookController.cs
 *
 * Purpose:
 * Implements InstructionBookController for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for InstructionBookController within the ui system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify InstructionBookController in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class InstructionPage
{
    public string title;
    [TextArea(3, 8)]
    public string body;
    public Sprite image;
}

public class InstructionBookController : MonoBehaviour
{
    [Header("Book Panel")]
    public GameObject instructionPanel;
    public Image pageImage;

    [Header("Texts")]
    public TMP_Text pageTitleText;
    public TMP_Text pageBodyText;
    public TMP_Text pageNumberText;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    [Header("Pages")]
    public InstructionPage[] pages;

    private int currentPageIndex = 0;
    private bool listenersBound;

    private void Awake()
    {
        BindListenersOnce();
    }
    private void Start()
    {
        // Keep the book hidden until the Instruction button opens it.
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    // Button listeners are registered once so UI buttons can work without extra wrapper scripts.
    private void BindListenersOnce()
    {
        if (listenersBound)
        {
            return;
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PreviousPage);
            prevButton.onClick.AddListener(PreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.onClick.AddListener(NextPage);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseBook);
            closeButton.onClick.AddListener(CloseBook);
        }

        listenersBound = true;
    }

    // Remove listeners to avoid duplicate calls if the object is recreated.
    private void UnbindListeners()
    {
        if (!listenersBound)
        {
            return;
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseBook);
        }

        listenersBound = false;
    }

    // Returns whether the instruction book panel is currently open.
    public bool IsOpen()
    {
        return instructionPanel != null && instructionPanel.activeSelf;
    }

    // Opens the instruction book and always starts from the first page.
    public void OpenBook()
    {
        if (instructionPanel == null)
        {
            Debug.LogWarning("Instruction panel is not assigned.");
            return;
        }

        instructionPanel.SetActive(true);
        ShowPage(0);
    }

    // Closes the instruction book popup.
    public void CloseBook()
    {
        if (instructionPanel == null)
        {
            Debug.LogWarning("Instruction panel is not assigned.");
            return;
        }

        instructionPanel.SetActive(false);
    }

    // Moves to the next instruction page when one exists.
    public void NextPage()
    {
        ShowPage(currentPageIndex + 1);
    }

    // Moves to the previous instruction page when one exists.
    public void PreviousPage()
    {
        ShowPage(currentPageIndex - 1);
    }

    // Displays the requested page and updates image, text, page count, and button states.
    public void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            ShowEmptyBookWarning();
            return;
        }

        currentPageIndex = Mathf.Clamp(index, 0, pages.Length - 1);
        InstructionPage page = pages[currentPageIndex];

        if (pageTitleText != null)
        {
            pageTitleText.text = page != null ? page.title : "Instruction";
        }

        if (pageBodyText != null)
        {
            pageBodyText.text = page != null ? page.body : "";
        }

        if (pageImage != null)
        {
            pageImage.sprite = page != null ? page.image : null;
            pageImage.enabled = page != null && page.image != null;
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = (currentPageIndex + 1) + " / " + pages.Length;
        }

        UpdateButtonStates();
    }

    // Shows readable fallback text instead of throwing errors when no pages are configured.
    private void ShowEmptyBookWarning()
    {
        currentPageIndex = 0;

        if (pageTitleText != null)
        {
            pageTitleText.text = "Instructions";
        }

        if (pageBodyText != null)
        {
            pageBodyText.text = "No instruction pages are assigned.";
        }

        if (pageImage != null)
        {
            pageImage.sprite = null;
            pageImage.enabled = false;
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = "0 / 0";
        }

        UpdateButtonStates();
        Debug.LogWarning("InstructionBookController has no pages assigned.");
    }

    // Enables or disables navigation buttons based on the current page.
    private void UpdateButtonStates()
    {
        bool hasPages = pages != null && pages.Length > 0;

        if (prevButton != null)
        {
            prevButton.interactable = hasPages && currentPageIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = hasPages && currentPageIndex < pages.Length - 1;
        }
    }
}
