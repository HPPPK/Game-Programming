using System.Collections;
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

    [Header("Default Part 2 Paths")]
    [SerializeField] private string claimableLandPath = "Map/BuildAreas/Claimable_BuildArea_Gate01";
    [SerializeField] private string publicBuildAreaPath = "Map/BuildAreas/Public_BuildArea_01";
    [SerializeField] private string towerBuildAreaPath = "Map/BuildAreas/Public_BuildArea_01";
    [SerializeField] private string radialConfirmButtonPath = "UI/Canvas/RadialTowerMenu/ConfirmButton";
    [SerializeField] private string radialUpgradeButtonPath = "UI/Canvas/RadialTowerMenu/UpgradeButton";
    [SerializeField] private int guideSceneStartingGold = 100;

    private int currentPartIndex;
    private int currentStepIndex;
    private bool tutorialStarted;
    private bool tutorialCompleted;
    private int lastCompletedPartIndex = -1;
    private Coroutine currentStepRoutine;
    private GameObject lastBuiltTowerObject;
    private GameObject lastTutorialBuildAreaObject;

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
        EnsureGuideSceneTutorialConfigured();

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
        lastBuiltTowerObject = null;
        lastTutorialBuildAreaObject = null;

        if (parts == null || parts.Count == 0)
        {
            Debug.LogWarning("TutorialManager has no parts configured.");
            return;
        }

        EnsureGuideSceneStartingResources();

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

        if (step != null && step.requiresPlayerAction && !IsStepAlreadySatisfied(step))
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

        StopCurrentStepRoutine();
        MarkCurrentPartCompleted();

        tutorialCompleted = true;
        tutorialStarted = false;
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        tutorialEnemyDemoSpawner?.ClearTutorialEnemies();
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

        if (!IsAllowedActionForStep(step, actionType))
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

    public void NotifyBuildAreaSelected(TutorialActionType actionType, GameObject target)
    {
        NotifyAction(actionType, target);
    }

    public void NotifyTutorialAction(TutorialActionType actionType, GameObject target)
    {
        NotifyAction(actionType, target);
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

    public void RegisterRuntimeBuiltTower(GameObject buildAreaObject, GameObject towerObject)
    {
        lastTutorialBuildAreaObject = buildAreaObject;
        lastBuiltTowerObject = ResolveTutorialTowerHighlightTarget(towerObject);

        if (buildAreaObject != null)
        {
            SetOrAddBinding("TowerBuildArea", buildAreaObject);
        }

        if (lastBuiltTowerObject != null)
        {
            SetOrAddBinding("BuiltTower", lastBuiltTowerObject);
        }
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

        if (!DoesActionAdvanceStep(step, actionType))
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
        StopCurrentStepRoutine();

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
        if (step.hideTutorialUIOnEnter)
        {
            messageController?.HideAll();
            highlightController?.HideHighlight();
        }
        else
        {
            messageController?.ShowStep(step);
            GameObject highlightTarget = ResolveHighlightTarget(step);
            Debug.Log("Tutorial Highlight target: " + (highlightTarget != null ? GetHierarchyPath(highlightTarget) : "None"));
            highlightController?.ShowHighlight(highlightTarget, step.highlightTargetId);
        }
        HandleStepEnterEffects(step);
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

        if (step.highlightTargetId == "BuiltTower" && lastBuiltTowerObject != null)
        {
            return lastBuiltTowerObject;
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

    private bool IsAllowedActionForStep(TutorialStep step, TutorialActionType actualAction)
    {
        if (step == null)
        {
            return false;
        }

        if (step.allowedActionTypes != null && step.allowedActionTypes.Count > 0)
        {
            for (int i = 0; i < step.allowedActionTypes.Count; i++)
            {
                if (IsActionCompatible(actualAction, step.allowedActionTypes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        return IsActionCompatible(actualAction, step.expectedActionType);
    }

    private bool DoesActionAdvanceStep(TutorialStep step, TutorialActionType actualAction)
    {
        if (step == null)
        {
            return false;
        }

        if (actualAction == step.expectedActionType)
        {
            return true;
        }

        if (step.expectedActionType == TutorialActionType.PlayCard && IsSpecificCardAction(actualAction))
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

    private bool IsStepAlreadySatisfied(TutorialStep step)
    {
        if (step == null || !step.requiresPlayerAction)
        {
            return true;
        }

        switch (step.expectedActionType)
        {
            case TutorialActionType.SelectLand:
            case TutorialActionType.BuyLand:
            {
                TowerBuildArea claimableArea = ResolveBoundBuildArea("ClaimableLand");
                return claimableArea != null && !claimableArea.IsUnowned();
            }

            case TutorialActionType.BuildTower:
            {
                TowerBuildArea buildArea = ResolveBoundBuildArea("TowerBuildArea");
                return buildArea != null && buildArea.currentTower != null && buildArea.isOccupied;
            }

            case TutorialActionType.UpgradeTower:
            {
                TowerBuildArea buildArea = ResolveBoundBuildArea("TowerBuildArea");
                TowerStats towerStats = buildArea != null && buildArea.currentTower != null
                    ? buildArea.currentTower.GetComponent<TowerStats>() ?? buildArea.currentTower.GetComponentInChildren<TowerStats>()
                    : null;
                return towerStats != null && towerStats.level > 1;
            }

            case TutorialActionType.InspectTower:
            {
                TowerBuildArea buildArea = ResolveBoundBuildArea("TowerBuildArea");
                return buildArea != null && buildArea.currentTower != null && buildArea.isOccupied;
            }

            default:
                return false;
        }
    }

    private TowerBuildArea ResolveBoundBuildArea(string targetId)
    {
        GameObject targetObject = ResolveBindingTarget(targetId);
        return targetObject != null ? targetObject.GetComponent<TowerBuildArea>() : null;
    }

    private GameObject ResolveBindingTarget(string targetId)
    {
        if (targetBindings == null || string.IsNullOrWhiteSpace(targetId))
        {
            return null;
        }

        for (int i = 0; i < targetBindings.Count; i++)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding != null && binding.targetId == targetId)
            {
                return binding.targetObject;
            }
        }

        return null;
    }

    private GameObject ResolveTutorialTowerHighlightTarget(GameObject towerObject)
    {
        if (towerObject == null)
        {
            return null;
        }

        Transform visualTransform = towerObject.transform.Find("visual");
        if (visualTransform != null)
        {
            return visualTransform.gameObject;
        }

        return towerObject;
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

    public bool CanAdvanceCurrentStepManually()
    {
        TutorialStep step = CurrentStep;

        if (step == null)
        {
            return false;
        }

        return !step.requiresPlayerAction || IsStepAlreadySatisfied(step);
    }

    private void EnsureGuideSceneTutorialConfigured()
    {
        if (!IsGuideScene())
        {
            return;
        }

        if (IsPart1ConfiguredCorrectly() && IsPart2ConfiguredCorrectly())
        {
            return;
        }

        Debug.Log("GuideScene tutorial data invalid. Regenerating default Part 1 and Part 2.");
        RemoveInvalidTutorialParts();
        RemoveInvalidTargetBindings();
        BuildDefaultPart1Bindings();
        BuildDefaultPart1Definition();
        BuildDefaultPart2Bindings();
        BuildDefaultPart2Definition();
    }

    private void EnsureGuideSceneStartingResources()
    {
        if (!IsGuideScene() || guideSceneStartingGold <= 0)
        {
            return;
        }

        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        if (playerManager == null || playerManager.players == null)
        {
            return;
        }

        for (int i = 0; i < playerManager.players.Count; i++)
        {
            PlayerResource player = playerManager.players[i];
            if (player == null || player.playerType == PlayerType.Empty)
            {
                continue;
            }

            if (player.money < guideSceneStartingGold)
            {
                player.AddMoney(guideSceneStartingGold - player.money);
            }
        }
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

    public bool IsPart2ConfiguredCorrectly()
    {
        TutorialPartDefinition part2 = FindPartDefinition(TutorialPartId.LandTowerGold);

        if (part2 == null || part2.steps == null || part2.steps.Count != 12)
        {
            return false;
        }

        string[] requiredTargetIds =
        {
            "GoldDisplay",
            "ClaimableLand",
            "PublicBuildArea",
            "TowerBuildArea",
            "ConfirmButton",
            "UpgradeButton",
            "BuiltTower"
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

    private void BuildDefaultPart2Bindings()
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        AddBinding("ClaimableLand", claimableLandPath);
        AddBinding("PublicBuildArea", publicBuildAreaPath);
        AddBinding("TowerBuildArea", towerBuildAreaPath);
        AddBinding("ConfirmButton", radialConfirmButtonPath);
        AddBinding("UpgradeButton", radialUpgradeButtonPath);
        AddBinding("WaveSpawnPoint", "Spawners/Spawner_Left");

        GameObject builtTowerFallback = FindSceneObjectByPath(towerBuildAreaPath);
        SetOrAddBinding("BuiltTower", builtTowerFallback);
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

    private void BuildDefaultPart2Definition()
    {
        if (parts == null)
        {
            parts = new List<TutorialPartDefinition>();
        }

        TutorialPartDefinition part2 = new TutorialPartDefinition
        {
            partId = TutorialPartId.LandTowerGold,
            displayTitle = "Gold, Land, Towers, and Upgrades",
            steps = new List<TutorialStep>
            {
                CreateInfoStep("part2_step_1", "Gold is used to buy land, build towers, and upgrade towers.", "GoldDisplay"),
                CreateInfoStep("part2_step_2", "Claimable land must be purchased before you can build on it.", "ClaimableLand"),
                CreateActionStep("part2_step_3", "Click the highlighted land tile.", "ClaimableLand", TutorialActionType.SelectLand, true, TutorialActionType.SelectLand),
                CreateActionStep("part2_step_4", "Purchase this land to claim it.", "ClaimableLand", TutorialActionType.BuyLand, false, TutorialActionType.BuyLand),
                CreateInfoStep("part2_step_5", "Public build areas can be used immediately without purchasing land.", "PublicBuildArea"),
                CreateActionStep("part2_step_6", "Build your first tower here.", "TowerBuildArea", TutorialActionType.BuildTower, true, TutorialActionType.SelectOwnedLand, TutorialActionType.BuildTower),
                CreateActionStep("part2_step_7", "Click your tower to open the upgrade menu.", "BuiltTower", TutorialActionType.InspectTower, false, TutorialActionType.InspectTower),
                CreateActionStep("part2_step_8", "Click Upgrade to prepare the tower upgrade.", "UpgradeButton", TutorialActionType.InspectTower, false, TutorialActionType.InspectTower),
                CreateActionStep("part2_step_9", "Confirm the upgrade to make your tower stronger.", "ConfirmButton", TutorialActionType.UpgradeTower, false, TutorialActionType.UpgradeTower),
                CreateInfoStep("part2_step_10", "Enemy waves will come from the spawn point. Click Next to start a small wave.", "WaveSpawnPoint"),
                CreateHiddenEnemyWaveStep("part2_step_11", 3, 10f),
                CreateInfoStep("part2_step_12", "Great! You have learned the basic build loop.", null)
            }
        };

        parts.Add(part2);
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

    private TutorialStep CreateActionStep(
        string stepId,
        string message,
        string highlightTargetId,
        TutorialActionType completionActionType,
        bool requireExactTarget,
        params TutorialActionType[] allowedActions)
    {
        TutorialStep step = CreateInfoStep(stepId, message, highlightTargetId);
        step.requiresPlayerAction = true;
        step.requireExactTarget = requireExactTarget;
        step.expectedActionType = completionActionType;

        if (allowedActions != null && allowedActions.Length > 0)
        {
            step.allowedActionTypes = new List<TutorialActionType>(allowedActions);
        }

        return step;
    }

    private TutorialStep CreateEnemyDemoStep(string stepId, string message, string highlightTargetId, int enemyCount)
    {
        TutorialStep step = CreateInfoStep(stepId, message, highlightTargetId);
        step.tutorialEnemySpawnCount = Mathf.Max(1, enemyCount);
        return step;
    }

    private TutorialStep CreateHiddenEnemyWaveStep(string stepId, int enemyCount, float fallbackAutoAdvanceSeconds)
    {
        TutorialStep step = CreateWaitStep(stepId, string.Empty, TutorialActionType.WaveCompleted, fallbackAutoAdvanceSeconds);
        step.tutorialEnemySpawnCount = Mathf.Max(1, enemyCount);
        step.hideTutorialUIOnEnter = true;
        return step;
    }

    private TutorialStep CreateWaitStep(string stepId, string message, TutorialActionType completionActionType, float fallbackAutoAdvanceSeconds)
    {
        TutorialStep step = CreateInfoStep(stepId, message, null);
        step.requiresPlayerAction = true;
        step.expectedActionType = completionActionType;
        step.allowedActionTypes = new List<TutorialActionType> { completionActionType };
        step.fallbackAutoAdvanceSeconds = fallbackAutoAdvanceSeconds;
        return step;
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

        SetOrAddBinding(targetId, targetObject);
    }

    private void SetOrAddBinding(string targetId, GameObject targetObject)
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        for (int i = 0; i < targetBindings.Count; i++)
        {
            TutorialTargetBinding binding = targetBindings[i];

            if (binding != null && binding.targetId == targetId)
            {
                binding.targetObject = targetObject;
                return;
            }
        }

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

    private void RemoveInvalidTutorialParts()
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

            if (part.partId == TutorialPartId.PlayerInfoAndScore ||
                part.partId == TutorialPartId.LandTowerGold ||
                isPlaceholder)
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

    private void HandleStepEnterEffects(TutorialStep step)
    {
        if (step == null)
        {
            return;
        }

        if (step.highlightTargetId == "BuiltTower" && lastBuiltTowerObject == null && lastTutorialBuildAreaObject != null)
        {
            SetOrAddBinding("BuiltTower", lastTutorialBuildAreaObject);
        }

        if (step.tutorialEnemySpawnCount > 0)
        {
            SpawnTutorialWeakEnemies(step.tutorialEnemySpawnCount);
        }

        if (step.requiresPlayerAction && step.fallbackAutoAdvanceSeconds > 0f)
        {
            currentStepRoutine = StartCoroutine(FallbackAdvanceRoutine(step, step.fallbackAutoAdvanceSeconds));
        }
    }

    private IEnumerator FallbackAdvanceRoutine(TutorialStep expectedStep, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (tutorialCompleted || !tutorialStarted || CurrentStep != expectedStep)
        {
            yield break;
        }

        Debug.LogWarning("Tutorial step timed out. Advancing: " + expectedStep.stepId);
        AdvanceToNextStep();
    }

    private void StopCurrentStepRoutine()
    {
        if (currentStepRoutine != null)
        {
            StopCoroutine(currentStepRoutine);
            currentStepRoutine = null;
        }
    }
}
