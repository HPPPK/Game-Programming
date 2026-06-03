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

    private void Awake()
    {
        ResolveButtonReferences();
        ResolveBackgroundReference();

        if (previousButton != null)
        {
            BindButton(previousButton, OnClickPrevious);
        }

        if (nextButton != null)
        {
            BindButton(nextButton, OnClickNext);
        }

        if (skipPartButton != null)
        {
            BindButton(skipPartButton, OnClickSkipPart);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    public void Initialize(TutorialManager manager)
    {
        tutorialManager = manager;
    }

    public void ShowStep(TutorialStep step)
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(true);
        }

        ApplyTutorialBackgroundColor();

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
            nextButton.interactable = step != null && !step.requiresPlayerAction;
        }

        if (skipPartButton != null)
        {
            skipPartButton.interactable = step != null;
        }
    }

    public void ShowCompletionPanel()
    {
        ApplyCompletionBackgroundColor();

        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
    }

    public void HideAll()
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    public void OnClickPrevious()
    {
        tutorialManager?.PreviousStep();
    }

    public void OnClickNext()
    {
        tutorialManager?.NextStep();
    }

    public void OnClickSkipPart()
    {
        tutorialManager?.SkipCurrentPart();
    }

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

    private Button FindChildButton(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

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

    private void ApplyTutorialBackgroundColor()
    {
        ResolveBackgroundReference();

        if (messagePanelBackground != null)
        {
            messagePanelBackground.color = tutorialBackgroundColor;
        }
    }

    private void ApplyCompletionBackgroundColor()
    {
        ResolveBackgroundReference();

        if (messagePanelBackground != null)
        {
            messagePanelBackground.color = completionBackgroundColor;
        }
    }
}
