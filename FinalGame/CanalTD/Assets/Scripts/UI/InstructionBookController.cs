/*
 * File: InstructionBookController.cs
 *
 * Purpose:
 * Controls a book-style instruction popup with multiple pages. Each page can
 * show a title, body text, and optional image.
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
