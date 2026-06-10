/*
 * File: TutorialMessageController.cs
 *
 * Purpose:
 * Implements TutorialMessageController for the tutorial layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tutorial scene objects, guide overlays, or scripted onboarding helpers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TutorialMessageController within the tutorial system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify TutorialMessageController in the scene or prefab where it is used and confirm the main happy path still works.
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMessageController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text partTitleText;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TMP_Text completionTitleText;
    [SerializeField] private TMP_Text completionMessageText;
    [SerializeField] private Image messagePanelBackground;
    [SerializeField] private Color tutorialBackgroundColor = new Color(0.17f, 0.14f, 0.10f, 0.92f);
    [SerializeField] private Color completionBackgroundColor = Color.white;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipPartButton;
    [System.Obsolete("SkipTutorialButton is no longer used by the tutorial message panel.")]
    [SerializeField] private Button skipTutorialButton;

    private TutorialManager tutorialManager;
    private bool cachedDefaultBackgroundColor;
    private Color defaultBackgroundColor;
    private RectTransform messageRootRect;
    private Vector2 defaultMessageRootAnchoredPosition;
    private Vector2 defaultMessageRootSizeDelta;
    private bool cachedDefaultMessageRootLayout;
    private float defaultMessageFontSize;
    private float defaultTitleFontSize;
    private bool cachedDefaultFontSizes;
    private RectTransform messageTextRect;
    private RectTransform partTitleTextRect;
    private Vector2 defaultMessageTextAnchoredPosition;
    private Vector2 defaultMessageTextSizeDelta;
    private Vector2 defaultPartTitleAnchoredPosition;
    private Vector2 defaultPartTitleSizeDelta;
    private bool cachedDefaultTextRects;

    /// <summary>
    /// Finds and stores tutorial message controller references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        ResolveButtonReferences();
        ResolveCompletionPanelReference();
        ResolveCompletionTextReferences();
        ResolveBackgroundReference();
        ResolveMessageRootLayoutDefaults();
        ResolveFontDefaults();
        ResolveTextRectDefaults();

        if (previousButton != null)
        {
            NormalizeTutorialButtonColors(previousButton);
            BindButton(previousButton, OnClickPrevious);
        }

        if (nextButton != null)
        {
            NormalizeTutorialButtonColors(nextButton);
            BindButton(nextButton, OnClickNext);
        }

        if (skipPartButton != null)
        {
            NormalizeTutorialButtonColors(skipPartButton);
            BindButton(skipPartButton, OnClickSkipPart);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Initializes initialize and prepares the references needed before use.
    /// </summary>
    public void Initialize(TutorialManager manager)
    {
        tutorialManager = manager;
    }

    /// <summary>
    /// Shows step with the correct current context.
    /// </summary>
    public void ShowStep(TutorialStep step)
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(true);
        }

        ApplyTutorialBackgroundColor();
        ApplyStepLayout(step);

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        if (partTitleText != null)
        {
            partTitleText.text = tutorialManager != null
                ? tutorialManager.GetCurrentPartTitleForUI()
                : (step != null ? step.partId.ToString() : string.Empty);
        }

        if (messageText != null)
        {
            messageText.text = step != null ? step.messageText : string.Empty;
        }

        if (previousButton != null)
        {
            previousButton.interactable = tutorialManager != null && !tutorialManager.IsAtFirstStep();
        }

        if (nextButton != null)
        {
            nextButton.interactable = tutorialManager != null
                ? tutorialManager.CanAdvanceCurrentStepManually()
                : (step != null && !step.requiresPlayerAction);
        }

        if (skipPartButton != null)
        {
            skipPartButton.interactable = step != null;
        }
    }

    /// <summary>
    /// Shows completion panel with the correct current context.
    /// </summary>
    public void ShowCompletionPanel(string completionMessage = null)
    {
        ResolveCompletionPanelReference();
        ResolveCompletionTextReferences();
        ApplyCompletionBackgroundColor();
        RestoreDefaultStepLayout();

        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }

        if (completionTitleText != null)
        {
            completionTitleText.text = "Congratulations!";
        }

        if (completionMessageText != null)
        {
            completionMessageText.text = string.IsNullOrWhiteSpace(completionMessage)
                ? "You have completed the CanalTD tutorial.\n\nGood luck!"
                : completionMessage;
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides all and clears temporary visual state.
    /// </summary>
    public void HideAll()
    {
        RestoreDefaultStepLayout();

        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Responds to on click previous and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickPrevious()
    {
        tutorialManager?.PreviousStep();
    }

    /// <summary>
    /// Responds to on click next and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickNext()
    {
        tutorialManager?.NextStep();
    }

    /// <summary>
    /// Responds to on click skip part and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickSkipPart()
    {
        tutorialManager?.SkipCurrentPart();
    }

    /// <summary>
    /// Looks up the target for button references and applies the resolved gameplay result.
    /// </summary>
    private void ResolveButtonReferences()
    {
        if (previousButton == null)
        {
            previousButton = FindChildButton("PreviousButton") ?? FindChildButton("Previous");
        }

        if (nextButton == null)
        {
            nextButton = FindChildButton("NextButton") ?? FindChildButton("Next");
        }

        if (skipPartButton == null ||
            skipPartButton.gameObject.name == "Previous" ||
            skipPartButton.gameObject.name == "PreviousButton")
        {
            skipPartButton = FindChildButton("SkipPartButton") ?? FindChildButton("SkipPart");
        }
    }

    /// <summary>
    /// Looks up the target for background reference and applies the resolved gameplay result.
    /// </summary>
    private void ResolveBackgroundReference()
    {
        if (messagePanelBackground == null)
        {
            messagePanelBackground = GetComponent<Image>();
        }

        if (!cachedDefaultBackgroundColor && messagePanelBackground != null)
        {
            defaultBackgroundColor = messagePanelBackground.color;
            cachedDefaultBackgroundColor = true;
        }
    }

    /// <summary>
    /// Looks up the target for completion panel reference and applies the resolved gameplay result.
    /// </summary>
    private void ResolveCompletionPanelReference()
    {
        if (completionPanel != null)
        {
            return;
        }

        Transform parent = transform.parent;

        if (parent != null)
        {
            Transform siblingPanel = parent.Find("TutorialCompletionPanel");

            if (siblingPanel != null)
            {
                completionPanel = siblingPanel.gameObject;
                return;
            }
        }

        GameObject foundPanel = GameObject.Find("TutorialCompletionPanel");

        if (foundPanel != null)
        {
            completionPanel = foundPanel;
        }
    }

    /// <summary>
    /// Looks up the target for completion text references and applies the resolved gameplay result.
    /// </summary>
    private void ResolveCompletionTextReferences()
    {
        if (completionPanel == null)
        {
            return;
        }

        if (completionTitleText == null)
        {
            Transform titleTransform = completionPanel.transform.Find("TitleText");

            if (titleTransform != null)
            {
                completionTitleText = titleTransform.GetComponent<TMP_Text>();
            }
        }

        if (completionMessageText == null)
        {
            Transform messageTransform = completionPanel.transform.Find("MessageText");

            if (messageTransform != null)
            {
                completionMessageText = messageTransform.GetComponent<TMP_Text>();
            }
        }
    }

    /// <summary>
    /// Looks up the target for message root layout defaults and applies the resolved gameplay result.
    /// </summary>
    private void ResolveMessageRootLayoutDefaults()
    {
        if (messageRootRect == null)
        {
            messageRootRect = transform as RectTransform;
        }

        if (!cachedDefaultMessageRootLayout && messageRootRect != null)
        {
            defaultMessageRootAnchoredPosition = messageRootRect.anchoredPosition;
            defaultMessageRootSizeDelta = messageRootRect.sizeDelta;
            cachedDefaultMessageRootLayout = true;
        }
    }

    /// <summary>
    /// Looks up the target for font defaults and applies the resolved gameplay result.
    /// </summary>
    private void ResolveFontDefaults()
    {
        if (!cachedDefaultFontSizes && messageText != null && partTitleText != null)
        {
            defaultMessageFontSize = messageText.fontSize;
            defaultTitleFontSize = partTitleText.fontSize;
            cachedDefaultFontSizes = true;
        }
    }

    /// <summary>
    /// Looks up the target for text rect defaults and applies the resolved gameplay result.
    /// </summary>
    private void ResolveTextRectDefaults()
    {
        if (messageTextRect == null && messageText != null)
        {
            messageTextRect = messageText.transform as RectTransform;
        }

        if (partTitleTextRect == null && partTitleText != null)
        {
            partTitleTextRect = partTitleText.transform as RectTransform;
        }

        if (!cachedDefaultTextRects && messageTextRect != null && partTitleTextRect != null)
        {
            defaultMessageTextAnchoredPosition = messageTextRect.anchoredPosition;
            defaultMessageTextSizeDelta = messageTextRect.sizeDelta;
            defaultPartTitleAnchoredPosition = partTitleTextRect.anchoredPosition;
            defaultPartTitleSizeDelta = partTitleTextRect.sizeDelta;
            cachedDefaultTextRects = true;
        }
    }

    /// <summary>
    /// Searches scene objects or cached lists to find child button.
    /// </summary>
    private Button FindChildButton(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    /// <summary>
    /// Handles bind button for tutorial guidance, highlights, or step progression.
    /// </summary>
    private void BindButton(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null || callback == null)
        {
            return;
        }

        if (button.onClick.GetPersistentEventCount() > 0)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    /// <summary>
    /// Handles normalize tutorial button colors for tutorial guidance, highlights, or step progression.
    /// </summary>
    private void NormalizeTutorialButtonColors(Button button)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.disabledColor = colors.normalColor;
        colors.colorMultiplier = Mathf.Max(1f, colors.colorMultiplier);
        button.colors = colors;
    }

    /// <summary>
    /// Applies tutorial background color to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyTutorialBackgroundColor()
    {
        ResolveBackgroundReference();

        if (messagePanelBackground != null)
        {
            messagePanelBackground.color = tutorialBackgroundColor;
        }
    }

    /// <summary>
    /// Applies step layout to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyStepLayout(TutorialStep step)
    {
        ResolveMessageRootLayoutDefaults();
        ResolveFontDefaults();

        if (IsFinalSummaryStep(step))
        {
            ApplyFinalSummaryLayout();
            return;
        }

        RestoreDefaultStepLayout();
    }

    /// <summary>
    /// Applies final summary layout to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyFinalSummaryLayout()
    {
        ResolveTextRectDefaults();

        if (messageRootRect != null)
        {
            messageRootRect.anchoredPosition = defaultMessageRootAnchoredPosition;
            messageRootRect.sizeDelta = new Vector2(
                defaultMessageRootSizeDelta.x + 4f,
                defaultMessageRootSizeDelta.y + 4.5f);
        }

        if (partTitleTextRect != null && cachedDefaultTextRects)
        {
            partTitleTextRect.anchoredPosition = defaultPartTitleAnchoredPosition;
            partTitleTextRect.sizeDelta = defaultPartTitleSizeDelta;
        }

        if (messageTextRect != null && cachedDefaultTextRects)
        {
            messageTextRect.anchoredPosition = new Vector2(
                defaultMessageTextAnchoredPosition.x,
                defaultMessageTextAnchoredPosition.y - 0.2f);
            messageTextRect.sizeDelta = new Vector2(
                defaultMessageTextSizeDelta.x,
                defaultMessageTextSizeDelta.y + 4f);
        }

        if (partTitleText != null)
        {
            partTitleText.fontSize = defaultTitleFontSize;
        }

        if (messageText != null)
        {
            messageText.fontSize = defaultMessageFontSize - 0.1f;
        }
    }

    /// <summary>
    /// Handles restore default step layout for tutorial guidance, highlights, or step progression.
    /// </summary>
    private void RestoreDefaultStepLayout()
    {
        ResolveMessageRootLayoutDefaults();
        ResolveFontDefaults();
        ResolveTextRectDefaults();

        if (messageRootRect != null && cachedDefaultMessageRootLayout)
        {
            messageRootRect.anchoredPosition = defaultMessageRootAnchoredPosition;
            messageRootRect.sizeDelta = defaultMessageRootSizeDelta;
        }

        if (partTitleTextRect != null && cachedDefaultTextRects)
        {
            partTitleTextRect.anchoredPosition = defaultPartTitleAnchoredPosition;
            partTitleTextRect.sizeDelta = defaultPartTitleSizeDelta;
        }

        if (messageTextRect != null && cachedDefaultTextRects)
        {
            messageTextRect.anchoredPosition = defaultMessageTextAnchoredPosition;
            messageTextRect.sizeDelta = defaultMessageTextSizeDelta;
        }

        if (partTitleText != null && cachedDefaultFontSizes)
        {
            partTitleText.fontSize = defaultTitleFontSize;
        }

        if (messageText != null && cachedDefaultFontSizes)
        {
            messageText.fontSize = defaultMessageFontSize;
        }
    }

    /// <summary>
    /// Checks the current state to decide whether final summary step is true.
    /// </summary>
    private bool IsFinalSummaryStep(TutorialStep step)
    {
        return step != null &&
               step.partId == TutorialPartId.EndTurn &&
               step.stepId == "part5_step_12";
    }

    /// <summary>
    /// Applies completion background color to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyCompletionBackgroundColor()
    {
        ResolveBackgroundReference();

        if (messagePanelBackground != null)
        {
            messagePanelBackground.color = completionBackgroundColor;
        }
    }
}
