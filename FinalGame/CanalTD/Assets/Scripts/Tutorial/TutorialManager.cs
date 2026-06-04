using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Flow")]
    [SerializeField] private bool autoStartOnEnable = true;
    [SerializeField] private List<TutorialPartDefinition> parts = new List<TutorialPartDefinition>();
    [SerializeField] private List<TutorialTargetBinding> targetBindings = new List<TutorialTargetBinding>();

    [Header("Controllers")]
    [SerializeField] private TutorialMessageController messageController;
    [SerializeField] private TutorialHighlightController highlightController;
    [SerializeField] private TutorialActionGate actionGate;
    [SerializeField] private TutorialEnemyDemoSpawner tutorialEnemyDemoSpawner;
    [SerializeField] private CardDrawManager cardDrawManager;

    [Header("Scene Names")]
    [SerializeField] private string guideSceneName = "GuideScene";
    [SerializeField] private string practiceVsAISceneName = "GameScene_AIPrototype";
    [SerializeField] private string homeSceneName = "HomeScene";

    [Header("Default Part 1 Paths")]
    [SerializeField] private string localPlayerCastleVisualPath = "Players/Castle_BottomLeft";
    [SerializeField] private string localPlayerInfoPanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel";
    [SerializeField] private string playerNameDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/Player";
    [SerializeField] private string hpDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/BloodNum";
    [SerializeField] private string goldDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/MoneyText";
    [SerializeField] private string cardCountDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/cardNum";
    [SerializeField] private string scoreDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/PointText";
    [SerializeField] private string turnIndicatorPath = "UI/Canvas/TextMeshProUGUI";

    private int currentPartIndex;
    private int currentStepIndex;
    private bool tutorialStarted;
    private bool tutorialCompleted;
    private int lastCompletedPartIndex = -1;

    public bool IsTutorialGameplayActive => tutorialStarted && !tutorialCompleted && CurrentStep != null && IsGuideScene();
    public TutorialStep CurrentStep => GetCurrentStep();

    private void Awake()
    {
        Instance = this;

        if (messageController == null)
        {
            messageController = FindObjectOfType<TutorialMessageController>();
        }

        if (highlightController == null)
        {
            highlightController = FindObjectOfType<TutorialHighlightController>();
        }

        if (actionGate == null)
        {
            actionGate = FindObjectOfType<TutorialActionGate>();
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        messageController?.Initialize(this);
    }

    private void Start()
    {
        EnsurePart1ConfiguredFromGuideScene();

        if (autoStartOnEnable)
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        tutorialCompleted = false;
        tutorialStarted = true;
        lastCompletedPartIndex = -1;
        currentPartIndex = 0;
        currentStepIndex = 0;

        if (parts == null || parts.Count == 0)
        {
            Debug.LogWarning("TutorialManager has no parts configured.");
            return;
        }

        Debug.Log("Tutorial Part started: " + GetCurrentPartTitle());
        ApplyCurrentStep();
    }

    public void NextStep()
    {
        if (!tutorialStarted || tutorialCompleted)
        {
            return;
        }

        TutorialStep step = CurrentStep;

        if (step != null && step.requiresPlayerAction)
        {
            return;
        }

        AdvanceToNextStep();
    }

    public void PreviousStep()
    {
        if (!tutorialStarted || tutorialCompleted)
        {
            return;
        }

        currentPartIndex = Mathf.Clamp(currentPartIndex, 0, Mathf.Max(0, parts != null ? parts.Count - 1 : 0));
        currentStepIndex = Mathf.Max(0, currentStepIndex);

        if (currentStepIndex > 0)
        {
            currentStepIndex -= 1;
            ApplyCurrentStep();
            return;
        }

        if (currentPartIndex > 0)
        {
            currentPartIndex -= 1;

            TutorialPartDefinition previousPart = parts != null &&
                                                  currentPartIndex >= 0 &&
                                                  currentPartIndex < parts.Count
                ? parts[currentPartIndex]
                : null;

            int previousPartStepCount = previousPart != null && previousPart.steps != null
                ? previousPart.steps.Count
                : 0;

            currentStepIndex = Mathf.Max(0, previousPartStepCount - 1);
            ApplyCurrentStep();
            return;
        }

        Debug.Log("Already at the beginning.");
    }

    public void SkipCurrentPart()
    {
        if (!tutorialStarted || tutorialCompleted)
        {
            return;
        }

        MarkCurrentPartCompleted();

        currentPartIndex += 1;
        currentStepIndex = 0;

        if (currentPartIndex >= parts.Count)
        {
            CompleteTutorial();
            return;
        }

        ApplyCurrentStep();
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    public void CompleteTutorial()
    {
        if (tutorialCompleted)
        {
            return;
        }

        MarkCurrentPartCompleted();

        tutorialCompleted = true;
        tutorialStarted = false;
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        highlightController?.HideHighlight();
        messageController?.ShowCompletionPanel();
    }

    public void OnClickPracticeVsAI()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(practiceVsAISceneName);
    }

    public void OnClickReturnToMenu()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(homeSceneName);
    }

    public bool IsActionAllowed(TutorialActionType actionType, GameObject target)
    {
        if (!IsTutorialGameplayActive)
        {
            return true;
        }

        TutorialStep step = CurrentStep;

        if (step == null)
        {
            return true;
        }

        if (!step.requiresPlayerAction)
        {
            return false;
        }

        if (!IsActionCompatible(actionType, step.expectedActionType))
        {
            return false;
        }

        if (!step.requireExactTarget)
        {
            return true;
        }

        GameObject expectedTarget = ResolveHighlightTarget(step);
        return expectedTarget == null || expectedTarget == target;
    }

    public string GetBlockedActionMessage(TutorialActionType actionType)
    {
        TutorialStep step = CurrentStep;

        if (step != null && !string.IsNullOrWhiteSpace(step.blockedMessageOverride))
        {
            return step.blockedMessageOverride;
        }

        if (step != null && step.requiresPlayerAction)
        {
            return "Follow the highlighted tutorial action first.";
        }

        return "Read the tutorial step and press Next.";
    }

    public void NotifyLandPurchased(GameObject target)
    {
        NotifyAction(TutorialActionType.BuyLand, target);
    }

    public void NotifyTowerBuilt(GameObject target)
    {
        NotifyAction(TutorialActionType.BuildTower, target);
    }

    public void NotifyTowerUpgraded(GameObject target)
    {
        NotifyAction(TutorialActionType.UpgradeTower, target);
    }

    public void NotifyTowerSold(GameObject target)
    {
        NotifyAction(TutorialActionType.SellTower, target);
    }

    public void NotifyCardDrawn(GameObject target)
    {
        NotifyAction(TutorialActionType.DrawCard, target);
    }

    public void NotifyCardDiscarded(GameObject target)
    {
        NotifyAction(TutorialActionType.DiscardCard, target);
    }

    public void NotifyCardPlayed(TutorialActionType actionType, GameObject target)
    {
        NotifyAction(actionType, target);
    }

    public void NotifyEndTurnClicked()
    {
        NotifyAction(TutorialActionType.EndTurnClicked, null);
    }

    public void NotifyTutorialWaveStarted()
    {
        NotifyAction(TutorialActionType.WaveStarted, null);
    }

    public void NotifyTutorialWaveCompleted()
    {
        NotifyAction(TutorialActionType.WaveCompleted, null);
    }

    public void SpawnTutorialWeakEnemies(int count)
    {
        if (tutorialEnemyDemoSpawner != null)
        {
            tutorialEnemyDemoSpawner.SpawnTutorialWeakEnemies(count);
            return;
        }

        Debug.LogWarning("TutorialEnemyDemoSpawner is not assigned.");
    }

    public bool ForceGiveTutorialCard(string cardId)
    {
        return cardDrawManager != null && cardDrawManager.ForceGiveTutorialCard(cardId);
    }

    public void ResetTutorialCardAction()
    {
        cardDrawManager?.ResetTutorialCardAction();
    }

    public bool PrepareTutorialCardDemo(string cardId)
    {
        return cardDrawManager != null && cardDrawManager.PrepareTutorialCardDemo(cardId);
    }

    private void NotifyAction(TutorialActionType actionType, GameObject target)
    {
        if (!IsTutorialGameplayActive)
        {
            return;
        }

        TutorialStep step = CurrentStep;

        if (step == null || !step.requiresPlayerAction)
        {
            return;
        }

        if (!IsActionAllowed(actionType, target))
        {
            return;
        }

        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        currentStepIndex += 1;

        while (currentPartIndex < parts.Count)
        {
            TutorialPartDefinition part = parts[currentPartIndex];
            int stepCount = part != null && part.steps != null ? part.steps.Count : 0;

            if (currentStepIndex < stepCount)
            {
                ApplyCurrentStep();
                return;
            }

            currentPartIndex += 1;
            currentStepIndex = 0;
        }

        CompleteTutorial();
    }

    private void ApplyCurrentStep()
    {
        TutorialStep step = CurrentStep;

        if (step == null)
        {
            Debug.LogWarning("TutorialManager could not resolve current step. Clamping back to a safe step.");

            if (parts != null && parts.Count > 0)
            {
                currentPartIndex = Mathf.Clamp(currentPartIndex, 0, parts.Count - 1);
                TutorialPartDefinition part = parts[currentPartIndex];
                int stepCount = part != null && part.steps != null ? part.steps.Count : 0;

                if (stepCount > 0)
                {
                    currentStepIndex = Mathf.Clamp(currentStepIndex, 0, stepCount - 1);
                    step = CurrentStep;
                }
            }

            if (step == null)
            {
                return;
            }
        }

        step.partId = parts[currentPartIndex].partId;
        Debug.Log("Tutorial Step changed: part=" + GetCurrentPartTitle() + ", stepIndex=" + currentStepIndex + ", stepId=" + step.stepId);
        messageController?.ShowStep(step);
        GameObject highlightTarget = ResolveHighlightTarget(step);
        Debug.Log("Tutorial Highlight target: " + (highlightTarget != null ? GetHierarchyPath(highlightTarget) : "None"));
        highlightController?.ShowHighlight(highlightTarget, step.highlightTargetId);
    }

    public bool IsAtFirstStep()
    {
        return currentPartIndex <= 0 && currentStepIndex <= 0;
    }

    private TutorialStep GetCurrentStep()
    {
        if (parts == null || currentPartIndex < 0 || currentPartIndex >= parts.Count)
        {
            return null;
        }

        TutorialPartDefinition part = parts[currentPartIndex];

        if (part == null || part.steps == null || currentStepIndex < 0 || currentStepIndex >= part.steps.Count)
        {
            return null;
        }

        return part.steps[currentStepIndex];
    }

    private GameObject ResolveHighlightTarget(TutorialStep step)
    {
        if (step == null)
        {
            return null;
        }

        if (step.highlightTargetObject != null)
        {
            return step.highlightTargetObject;
        }

        if (string.IsNullOrWhiteSpace(step.highlightTargetId) || targetBindings == null)
        {
            return null;
        }

        for (int i = 0; i < targetBindings.Count; i++)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding != null &&
                !string.IsNullOrWhiteSpace(binding.targetId) &&
                binding.targetId == step.highlightTargetId)
            {
                return binding.targetObject;
            }
        }

        return null;
    }

    private bool IsActionCompatible(TutorialActionType actual, TutorialActionType expected)
    {
        if (expected == TutorialActionType.None)
        {
            return true;
        }

        if (actual == expected)
        {
            return true;
        }

        if (actual == TutorialActionType.SelectLand)
        {
            return expected == TutorialActionType.BuyLand ||
                   expected == TutorialActionType.TakeOver ||
                   expected == TutorialActionType.FreezeClaim;
        }

        if (actual == TutorialActionType.SelectOwnedLand)
        {
            return expected == TutorialActionType.BuildTower ||
                   expected == TutorialActionType.UpgradeTower ||
                   expected == TutorialActionType.SellTower ||
                   expected == TutorialActionType.InspectTower ||
                   expected == TutorialActionType.PowerBoost;
        }

        if (actual == TutorialActionType.SelectCard)
        {
            return expected == TutorialActionType.PlayCard ||
                   expected == TutorialActionType.DiscardCard ||
                   IsSpecificCardAction(expected);
        }

        if (expected == TutorialActionType.PlayCard && IsSpecificCardAction(actual))
        {
            return true;
        }

        return false;
    }

    private bool IsSpecificCardAction(TutorialActionType actionType)
    {
        return actionType == TutorialActionType.OpenGate ||
               actionType == TutorialActionType.LockGate ||
               actionType == TutorialActionType.TakeOver ||
               actionType == TutorialActionType.FreezeClaim ||
               actionType == TutorialActionType.StealCard ||
               actionType == TutorialActionType.TradeHands ||
               actionType == TutorialActionType.Disrupt ||
               actionType == TutorialActionType.PowerBoost ||
               actionType == TutorialActionType.PlaceShockTrap;
    }

    private bool IsGuideScene()
    {
        return SceneManager.GetActiveScene().name == guideSceneName;
    }

    private string GetCurrentPartTitle()
    {
        if (parts == null || currentPartIndex < 0 || currentPartIndex >= parts.Count)
        {
            return string.Empty;
        }

        TutorialPartDefinition part = parts[currentPartIndex];

        if (part != null && !string.IsNullOrWhiteSpace(part.displayTitle))
        {
            return part.displayTitle;
        }

        return part != null ? part.partId.ToString() : string.Empty;
    }

    public string GetCurrentPartTitleForUI()
    {
        return GetCurrentPartTitle();
    }

    private void EnsurePart1ConfiguredFromGuideScene()
    {
        if (!IsGuideScene())
        {
            return;
        }

        if (IsPart1ConfiguredCorrectly())
        {
            return;
        }

        Debug.Log("GuideScene Part 1 data invalid. Regenerating default Part 1.");
        RemoveInvalidPart1AndPlaceholderParts();
        RemoveInvalidTargetBindings();
        BuildDefaultPart1Bindings();
        BuildDefaultPart1Definition();
    }

    public bool IsPart1ConfiguredCorrectly()
    {
        TutorialPartDefinition part1 = FindPartDefinition(TutorialPartId.PlayerInfoAndScore);

        if (part1 == null || part1.steps == null || part1.steps.Count != 10)
        {
            return false;
        }

        string[] requiredTargetIds =
        {
            "PlayerCastle",
            "PlayerInfoPanel",
            "PlayerNameDisplay",
            "GoldDisplay",
            "HPDisplay",
            "CardCountDisplay",
            "ScoreDisplay",
            "TurnIndicator"
        };

        for (int i = 0; i < requiredTargetIds.Length; i++)
        {
            if (!HasValidTargetBinding(requiredTargetIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void BuildDefaultPart1Bindings()
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        AddBinding("PlayerCastle", localPlayerCastleVisualPath);
        AddBinding("PlayerInfoPanel", localPlayerInfoPanelPath);
        AddBinding("PlayerNameDisplay", playerNameDisplayPath);
        AddBinding("GoldDisplay", goldDisplayPath);
        AddBinding("HPDisplay", hpDisplayPath);
        AddBinding("CardCountDisplay", cardCountDisplayPath);
        AddBinding("ScoreDisplay", scoreDisplayPath);
        AddBinding("TurnIndicator", turnIndicatorPath);
    }

    private void BuildDefaultPart1Definition()
    {
        if (parts == null)
        {
            parts = new List<TutorialPartDefinition>();
        }

        TutorialPartDefinition part1 = new TutorialPartDefinition
        {
            partId = TutorialPartId.PlayerInfoAndScore,
            displayTitle = "Player Information & Winning Condition",
            steps = new List<TutorialStep>
            {
                CreateInfoStep("part1_step_1", "This is your castle. Protect it from enemy attacks.", "PlayerCastle"),
                CreateInfoStep("part1_step_2", "This panel shows your current game information.", "PlayerInfoPanel"),
                CreateInfoStep("part1_step_3", "This is your player name. You can customize it before starting a match.", "PlayerNameDisplay"),
                CreateInfoStep("part1_step_4", "HP represents your remaining castle health.", "HPDisplay"),
                CreateInfoStep("part1_step_5", "Gold is used to buy land, build towers, and upgrade towers.", "GoldDisplay"),
                CreateInfoStep("part1_step_6", "Cards give you powerful actions and strategic options.", "CardCountDisplay"),
                CreateInfoStep("part1_step_7", "Score determines the final winner.", "ScoreDisplay"),
                CreateInfoStep("part1_step_8", "Defeating enemies and surviving waves will increase your score.", "ScoreDisplay"),
                CreateInfoStep("part1_step_9", "Always pay attention to whose turn it is.", "TurnIndicator"),
                CreateInfoStep("part1_step_10", "Great! You now understand the player information system.", null)
            }
        };

        parts.Insert(0, part1);
    }

    private TutorialStep CreateInfoStep(string stepId, string message, string highlightTargetId)
    {
        return new TutorialStep
        {
            partId = TutorialPartId.PlayerInfoAndScore,
            stepId = stepId,
            messageText = message,
            highlightTargetId = highlightTargetId,
            requiresPlayerAction = false,
            expectedActionType = TutorialActionType.None
        };
    }

    private void AddBinding(string targetId, string hierarchyPath)
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        for (int i = targetBindings.Count - 1; i >= 0; i--)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding != null &&
                !string.IsNullOrWhiteSpace(binding.targetId) &&
                binding.targetId == targetId)
            {
                targetBindings.RemoveAt(i);
            }
        }

        GameObject targetObject = FindSceneObjectByPath(hierarchyPath);

        targetBindings.Add(new TutorialTargetBinding
        {
            targetId = targetId,
            targetObject = targetObject
        });
    }

    private GameObject FindSceneObjectByPath(string hierarchyPath)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
        {
            return null;
        }

        string[] segments = hierarchyPath.Split('/');
        GameObject current = GameObject.Find(segments[0]);

        if (current == null)
        {
            Debug.LogWarning("Tutorial target root not found: " + hierarchyPath);
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            Transform child = current.transform.Find(segments[i]);

            if (child == null)
            {
                Debug.LogWarning("Tutorial target path not found: " + hierarchyPath);
                return null;
            }

            current = child.gameObject;
        }

        return current;
    }

    private string GetHierarchyPath(GameObject target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        Transform current = target.transform;
        List<string> segments = new List<string>();

        while (current != null)
        {
            segments.Add(current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments.ToArray());
    }

    private TutorialPartDefinition FindPartDefinition(TutorialPartId partId)
    {
        if (parts == null)
        {
            return null;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            TutorialPartDefinition part = parts[i];

            if (part != null && part.partId == partId)
            {
                return part;
            }
        }

        return null;
    }

    private bool HasValidTargetBinding(string targetId)
    {
        if (targetBindings == null || string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        for (int i = 0; i < targetBindings.Count; i++)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding != null &&
                binding.targetId == targetId &&
                binding.targetObject != null)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveInvalidPart1AndPlaceholderParts()
    {
        if (parts == null)
        {
            parts = new List<TutorialPartDefinition>();
            return;
        }

        for (int i = parts.Count - 1; i >= 0; i--)
        {
            TutorialPartDefinition part = parts[i];

            if (part == null)
            {
                parts.RemoveAt(i);
                continue;
            }

            bool isPlaceholder = part.steps == null || part.steps.Count == 0;

            if (part.partId == TutorialPartId.PlayerInfoAndScore || isPlaceholder)
            {
                parts.RemoveAt(i);
            }
        }
    }

    private void RemoveInvalidTargetBindings()
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
            return;
        }

        for (int i = targetBindings.Count - 1; i >= 0; i--)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding == null ||
                string.IsNullOrWhiteSpace(binding.targetId) ||
                binding.targetObject == null)
            {
                targetBindings.RemoveAt(i);
            }
        }
    }

    private void MarkCurrentPartCompleted()
    {
        if (currentPartIndex < 0 || currentPartIndex == lastCompletedPartIndex)
        {
            return;
        }

        Debug.Log("Tutorial Part completed: " + GetCurrentPartTitle());
        lastCompletedPartIndex = currentPartIndex;
    }
}
