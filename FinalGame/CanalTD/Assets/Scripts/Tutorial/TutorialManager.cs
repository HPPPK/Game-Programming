/*
 * File: TutorialManager.cs
 *
 * Purpose:
 * Implements TutorialManager for the tutorial layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tutorial scene objects, guide overlays, or scripted onboarding helpers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TutorialManager within the tutorial system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Verify TutorialManager in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    private const string ForcedGuideSceneTurnMarkerPath = "Players/Castle_TopLeft/TurnMaker";
    private const string ForcedGuideSceneTurnMarkerTopRightPath = "Players/Castle_TopRight/TurnMaker";
    private const string ForcedGuideSceneTurnMarkerBottomLeftPath = "Players/Castle_BottomLeft/TurnMaker";
    private const string ForcedGuideSceneTurnMarkerBottomRightPath = "Players/Castle_BottomRight/TurnMaker";
    private const string ForcedGuideSceneTurnBannerPath = "UI/Canvas/TextMeshProUGUI";
    private const string GuideSceneEndTurnButtonPath = "UI/Canvas/NormalGameplayUI/EndTurnButton";
    private const string TutorialWaveBlockedMessage = "Wait until the wave is finished.";

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
    [SerializeField] private string modeSelectSceneName = "ModeSelectScene";
    [SerializeField] private string practiceVsAISceneName = "GameScene_AIPrototype";
    [SerializeField] private string homeSceneName = "HomeScene";
    [TextArea(2, 5)]
    [SerializeField] private string tutorialCompletionMessage = "You have completed the CanalTD tutorial.\n\nGood luck!";

    [Header("Default Part 1 Paths")]
    [SerializeField] private string localPlayerCastleVisualPath = "Players/Castle_BottomLeft";
    [SerializeField] private string localPlayerInfoPanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel";
    [SerializeField] private string playerNameDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/Player";
    [SerializeField] private string hpDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/BloodNum";
    [SerializeField] private string goldDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/MoneyText";
    [SerializeField] private string cardCountDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/cardNum";
    [SerializeField] private string scoreDisplayPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel/PointText";
    [Header("Default Part 2 Paths")]
    [SerializeField] private string claimableLandPath = "Map/BuildAreas/Claimable_BuildArea_Gate01";
    [SerializeField] private string publicBuildAreaPath = "Map/BuildAreas/Public_BuildArea_01";
    [SerializeField] private string towerBuildAreaPath = "Map/BuildAreas/Public_BuildArea_01";
    [SerializeField] private string radialConfirmButtonPath = "UI/Canvas/RadialTowerMenu/ConfirmButton";
    [SerializeField] private string radialUpgradeButtonPath = "UI/Canvas/RadialTowerMenu/UpgradeButton";
    [SerializeField] private int guideSceneStartingGold = 100;

    [Header("Merged Player / Turn Paths")]
    [SerializeField] private string player1PanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel";
    [SerializeField] private string player2PanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel (1)";
    [SerializeField] private string player3PanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel (2)";
    [SerializeField] private string player4PanelPath = "UI/Canvas/NormalGameplayUI/PlayerStatusPanel (3)";

    [Header("Default Part 4 Paths")]
    [SerializeField] private string cardHandPath = "UI/Canvas/NormalGameplayUI/CardSlot";
    [SerializeField] private string drawButtonPath = "UI/Canvas/NormalGameplayUI/DrawCardButton";
    [SerializeField] private string discardButtonPath = "UI/Canvas/NormalGameplayUI/DiscardButton";
    [SerializeField] private string playButtonPath = "UI/Canvas/NormalGameplayUI/PlayCardButton";
    [SerializeField] private string targetingConfirmButtonPath = "UI/Canvas/TargetingUI/ConfirmButton";
    [SerializeField] private string targetingCancelButtonPath = "UI/Canvas/TargetingUI/CancelButton";
    [SerializeField] private string tutorialGatePath = "Gates/Gate_Stone_01";
    [SerializeField] private string tutorialShockTrapNodePath = "Map/PathGraph/RouteNodes/R_Left_01";
    [SerializeField] private string tutorialOpponentCastlePath = "Players/Castle_TopRight";
    [SerializeField] private string tutorialOwnedTowerAreaPath = "Map/BuildAreas/Public_BuildArea_01";
    [SerializeField] private string tutorialOpponentLandAreaPath = "Map/BuildAreas/Claimable_BuildArea_Gate02";
    [SerializeField] private string tutorialOpponentTowerAreaPath = "Map/BuildAreas/Claimable_BuildArea_Gate03";
    [Header("Default Part 5 Paths")]
    [SerializeField] private string endTurnButtonPath = GuideSceneEndTurnButtonPath;
    [SerializeField] private string roundTextPath = ForcedGuideSceneTurnBannerPath;

    private int currentPartIndex;
    private int currentStepIndex;
    private bool tutorialStarted;
    private bool tutorialCompleted;
    private int lastCompletedPartIndex = -1;
    private Coroutine currentStepRoutine;
    private GameObject lastBuiltTowerObject;
    private GameObject lastTutorialBuildAreaObject;
    private HashSet<string> completedStepIds = new HashSet<string>();
    private bool guideSceneWaveTutorialStarted;
    private bool guideSceneWaveTutorialCompleted;
    private bool guideSceneWaveTutorialRemainingTurnsSimulated;
    private bool guideSceneWaveTutorialEnemiesSpawned;

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
        completedStepIds.Clear();
        guideSceneWaveTutorialStarted = false;
        guideSceneWaveTutorialCompleted = false;
        guideSceneWaveTutorialRemainingTurnsSimulated = false;
        guideSceneWaveTutorialEnemiesSpawned = false;

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
        messageController?.ShowCompletionPanel(tutorialCompletionMessage);
        guideSceneWaveTutorialStarted = false;
        guideSceneWaveTutorialCompleted = false;
        guideSceneWaveTutorialRemainingTurnsSimulated = false;
        guideSceneWaveTutorialEnemiesSpawned = false;
    }

    public void OnClickPracticeVsAI()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(practiceVsAISceneName);
    }

    public void OnClickOpenModeSelect()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(modeSelectSceneName);
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
            if (step.allowedActionTypes == null || step.allowedActionTypes.Count == 0)
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

            GameObject passiveStepTarget = ResolveHighlightTarget(step);
            return passiveStepTarget == null || passiveStepTarget == target;
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

    public string GetWaveInteractionBlockedMessageOrDefault(string fallbackMessage)
    {
        return IsGuideSceneTutorialWaveInteractionBlocked()
            ? TutorialWaveBlockedMessage
            : fallbackMessage;
    }

    public GameObject GetBoundTargetObject(string targetId)
    {
        return ResolveBindingTarget(targetId);
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
        if (actionType == TutorialActionType.SelectCard && target != null)
        {
            SetOrAddBinding("CurrentCard", target);
            Transform cardSlot = target.transform.parent;
            if (cardSlot != null)
            {
                SetOrAddBinding("CurrentCardSlot", cardSlot.gameObject);
            }
        }

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
        if (IsGuideScenePart5WaveStepActive())
        {
            CompleteGuideSceneTutorialWaveState(false);
        }

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

    public bool ForceSingleTutorialCard(string cardId)
    {
        return cardDrawManager != null && cardDrawManager.ForceSingleTutorialCard(cardId);
    }

    public bool ForceTutorialHand(IEnumerable<string> cardIds)
    {
        return cardDrawManager != null && cardDrawManager.ForceTutorialHand(cardIds);
    }

    public void ResetTutorialCardAction()
    {
        cardDrawManager?.ResetTutorialCardAction();
    }

    public bool PrepareTutorialCardDemo(string cardId)
    {
        return cardDrawManager != null && cardDrawManager.PrepareTutorialCardDemo(cardId);
    }

    public bool PrepareTutorialCardDemo(string cardId, IEnumerable<string> handCardIds)
    {
        return cardDrawManager != null && cardDrawManager.PrepareTutorialCardDemo(cardId, handCardIds);
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

        MarkStepCompleted(step);
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
        PrepareStepBindings(step);
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

    private void PrepareStepBindings(TutorialStep step)
    {
        if (step == null)
        {
            return;
        }

        if (IsGuideScene() && step.partId == TutorialPartId.EndTurn)
        {
            UpdateGuideScenePart5TurnIndicatorBinding(step.stepId);
        }
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

        if (actual == TutorialActionType.SelectTarget)
        {
            return expected == TutorialActionType.SelectTarget;
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

        if (!string.IsNullOrWhiteSpace(step.stepId) && completedStepIds.Contains(step.stepId))
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

            case TutorialActionType.WaveCompleted:
                return guideSceneWaveTutorialCompleted;

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

        if (IsPart1ConfiguredCorrectly() &&
            IsPart2ConfiguredCorrectly() &&
            IsPart4ConfiguredCorrectly() &&
            IsPart5ConfiguredCorrectly() &&
            FindPartDefinition(TutorialPartId.EnemyWave) == null &&
            FindPartDefinition(TutorialPartId.PlayersTurnsScoring) == null)
        {
            return;
        }

        Debug.Log("GuideScene tutorial data invalid. Regenerating default merged Parts 1, 2, 4, and 5.");
        RemoveInvalidTutorialParts();
        RemoveInvalidTargetBindings();
        BuildDefaultPart1Bindings();
        BuildDefaultPart1Definition();
        BuildDefaultPart2Bindings();
        BuildDefaultPart2Definition();
        BuildDefaultPart4Bindings();
        BuildDefaultPart4Definition();
        BuildDefaultPart5Bindings();
        BuildDefaultPart5Definition();
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

        if (part1 == null || part1.steps == null || part1.steps.Count != 17)
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
            "TurnBanner",
            "TurnIndicator",
            "Player1Panel",
            "Player2Panel",
            "Player3Panel",
            "Player4Panel"
        };

        for (int i = 0; i < requiredTargetIds.Length; i++)
        {
            if (!HasValidTargetBinding(requiredTargetIds[i]))
            {
                return false;
            }
        }

        if (!DoesBindingMatchPath("TurnBanner", ForcedGuideSceneTurnBannerPath) ||
            !DoesBindingMatchPath("TurnIndicator", ForcedGuideSceneTurnMarkerPath))
        {
            return false;
        }

        return true;
    }

    public bool IsPart2ConfiguredCorrectly()
    {
        TutorialPartDefinition part2 = FindPartDefinition(TutorialPartId.LandTowerGold);

        if (part2 == null || part2.steps == null || part2.steps.Count != 15)
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
            "SellButton",
            "BuiltTower"
        };

        for (int i = 0; i < requiredTargetIds.Length; i++)
        {
            if (!HasValidTargetBinding(requiredTargetIds[i]))
            {
                return false;
            }
        }

        if (!DoesBindingMatchPath("ConfirmButton", radialConfirmButtonPath) ||
            !DoesBindingMatchPath("UpgradeButton", radialUpgradeButtonPath) ||
            !DoesBindingMatchPath("SellButton", "UI/Canvas/RadialTowerMenu/SellButton/Image"))
        {
            return false;
        }

        return true;
    }

    public bool IsPart4ConfiguredCorrectly()
    {
        TutorialPartDefinition part4 = FindPartDefinition(TutorialPartId.Cards);

        if (part4 == null || part4.steps == null || part4.steps.Count != 66)
        {
            return false;
        }

        string[] requiredTargetIds =
        {
            "CardHand",
            "DrawButton",
            "DiscardButton",
            "PlayButton",
            "Gate",
            "TargetingConfirmButton",
            "TargetingCancelButton",
            "ClaimableLand",
            "PublicBuildArea",
            "BuiltTower",
            "ShockTrapNode",
            "OpponentCastle"
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

    public bool IsPart5ConfiguredCorrectly()
    {
        TutorialPartDefinition part5 = FindPartDefinition(TutorialPartId.EndTurn);

        if (part5 == null || part5.steps == null || part5.steps.Count != 11)
        {
            return false;
        }

        string[] requiredTargetIds =
        {
            "EndTurnButton",
            "TurnIndicator",
            "WaveIndicator",
            "RoundText",
            "BuiltTower"
        };

        for (int i = 0; i < requiredTargetIds.Length; i++)
        {
            if (!HasValidTargetBinding(requiredTargetIds[i]))
            {
                return false;
            }
        }

        if (!DoesBindingMatchPath("EndTurnButton", endTurnButtonPath) ||
            !DoesBindingMatchPath("WaveIndicator", roundTextPath) ||
            !DoesBindingMatchPath("RoundText", roundTextPath))
        {
            return false;
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
        AddBinding("TurnBanner", ForcedGuideSceneTurnBannerPath);
        AddBinding("TurnIndicator", ForcedGuideSceneTurnMarkerPath);
        AddBinding("Player1Panel", player1PanelPath);
        AddBinding("Player2Panel", player2PanelPath);
        AddBinding("Player3Panel", player3PanelPath);
        AddBinding("Player4Panel", player4PanelPath);
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
        AddBinding("SellButton", "UI/Canvas/RadialTowerMenu/SellButton/Image");
        AddBinding("WaveSpawnPoint", "Spawners/Spawner_Left");

        GameObject builtTowerFallback = FindSceneObjectByPath(towerBuildAreaPath);
        SetOrAddBinding("BuiltTower", builtTowerFallback);
    }

    private void BuildDefaultPart4Bindings()
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        AddBinding("CardHand", cardHandPath);
        AddBinding("DrawButton", drawButtonPath);
        AddBinding("DiscardButton", discardButtonPath);
        AddBinding("PlayButton", playButtonPath);
        AddBinding("Gate", tutorialGatePath);
        AddBinding("TargetingConfirmButton", targetingConfirmButtonPath);
        AddBinding("TargetingCancelButton", targetingCancelButtonPath);
        AddBinding("ShockTrapNode", tutorialShockTrapNodePath);
        AddBinding("OpponentCastle", tutorialOpponentCastlePath);
    }

    private void BuildDefaultPart5Bindings()
    {
        if (targetBindings == null)
        {
            targetBindings = new List<TutorialTargetBinding>();
        }

        AddBinding("EndTurnButton", endTurnButtonPath);
        AddBinding("WaveIndicator", roundTextPath);
        AddBinding("RoundText", roundTextPath);
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
                CreateInfoStep("part1_step_9", "Each player is represented by a unique color.", "Player1Panel"),
                CreateInfoStep("part1_step_10", "Player colors help identify ownership of land, towers, and actions.", "Player2Panel"),
                CreateInfoStep("part1_step_11", "Throughout the match, every player's assets follow their color.", "Player3Panel"),
                CreateInfoStep("part1_step_12", "Knowing who owns a structure is important for strategy.", "Player4Panel"),
                CreateInfoStep("part1_step_13", "This banner displays the current round and whose turn it is.", "TurnBanner"),
                CreateInfoStep("part1_step_14", "This marker also indicates the player whose turn is active.", "TurnIndicator"),
                CreateInfoStep("part1_step_15", "Only the active player may perform turn actions.", "TurnIndicator"),
                CreateInfoStep("part1_step_16", "Always monitor both your status panel and your opponents' progress.", "PlayerInfoPanel"),
                CreateInfoStep("part1_step_17", "Great! You now understand the player information system.", null)
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
                CreateInfoStep(TutorialPartId.LandTowerGold, "part2_step_1", "Gold is used to buy land, build towers, and upgrade towers.", "GoldDisplay"),
                CreateInfoStep(TutorialPartId.LandTowerGold, "part2_step_2", "Claimable land must be purchased before you can build on it.", "ClaimableLand"),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_3", "Click the highlighted land tile.", "ClaimableLand", TutorialActionType.SelectLand, true, TutorialActionType.SelectLand),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_4", "Purchase this land to claim it.", "ClaimableLand", TutorialActionType.BuyLand, false, TutorialActionType.BuyLand),
                CreateInfoStep(TutorialPartId.LandTowerGold, "part2_step_5", "Public build areas can be used immediately without purchasing land.", "PublicBuildArea"),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_6", "Build your first tower here.", "TowerBuildArea", TutorialActionType.BuildTower, true, TutorialActionType.SelectOwnedLand, TutorialActionType.BuildTower),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_7", "Click your tower to open the upgrade menu.", "BuiltTower", TutorialActionType.InspectTower, false, TutorialActionType.InspectTower),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_8", "Click Upgrade to prepare the tower upgrade.", "UpgradeButton", TutorialActionType.PrepareUpgradeTower, false, TutorialActionType.PrepareUpgradeTower),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_9", "Confirm the upgrade to make your tower stronger.", "ConfirmButton", TutorialActionType.UpgradeTower, false, TutorialActionType.UpgradeTower),
                CreateInfoStep(TutorialPartId.LandTowerGold, "part2_step_10", "Enemy waves will come from the spawn point. Click Next to start a small wave.", "WaveSpawnPoint"),
                CreateHiddenEnemyWaveStep(TutorialPartId.LandTowerGold, "part2_step_11", 1, 10f),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_12", "Click your upgraded tower to open the tower menu.", "BuiltTower", TutorialActionType.InspectTower, false, TutorialActionType.InspectTower),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_13", "Click Sell to prepare selling this tower.", "SellButton", TutorialActionType.PrepareSellTower, false, TutorialActionType.PrepareSellTower),
                CreateActionStep(TutorialPartId.LandTowerGold, "part2_step_14", "Confirm the sale to recover some gold.", "ConfirmButton", TutorialActionType.SellTower, false, TutorialActionType.SellTower),
                CreateInfoStep(TutorialPartId.LandTowerGold, "part2_step_15", "Great! You have learned the basic build loop.", null)
            }
        };

        parts.Add(part2);
    }

    private void BuildDefaultPart4Definition()
    {
        if (parts == null)
        {
            parts = new List<TutorialPartDefinition>();
        }

        TutorialPartDefinition part4 = new TutorialPartDefinition
        {
            partId = TutorialPartId.Cards,
            displayTitle = "Cards and Special Actions",
            steps = new List<TutorialStep>()
        };

        List<TutorialStep> steps = part4.steps;
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_1", "Cards provide powerful strategic actions.", "CardHand"));
        steps.Add(CreateCardActionStep(TutorialPartId.Cards, "part4_step_2", "Instead of playing a card, you may draw one card.", "DrawButton", TutorialActionType.DrawCard, null, false, TutorialActionType.DrawCard));
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_3", "Discard returns one selected card to the deck.", "DiscardButton"));
        TutorialStep discardSelectStep = CreateCardActionStep(TutorialPartId.Cards, "part4_step_4", "Click a card in your hand to select it for discarding.", "CardHand", TutorialActionType.SelectCard, "StealCard", false, TutorialActionType.SelectCard);
        SetTutorialHand(discardSelectStep, "StealCard", "ShockTrap", "LockGate");
        steps.Add(discardSelectStep);
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_5", "The selected card rises slightly so you can see it is active.", "CardHand"));
        steps.Add(CreateCardActionStep(TutorialPartId.Cards, "part4_step_6", "Now click Discard to discard the selected card.", "DiscardButton", TutorialActionType.DiscardCard, null, false, TutorialActionType.DiscardCard));
        TutorialStep playIntroStep = CreateInfoStep(TutorialPartId.Cards, "part4_step_7", "Playing a card uses its special effect.", "PlayButton");
        SetTutorialHand(playIntroStep, "LockGate");
        steps.Add(playIntroStep);
        TutorialStep playSelectStep = CreateCardActionStep(TutorialPartId.Cards, "part4_step_8", "Click a card in your hand to select it for play.", "CardHand", TutorialActionType.SelectCard, "LockGate", false, TutorialActionType.SelectCard);
        steps.Add(playSelectStep);
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_9", "This selected card is ready to play.", "CardHand"));
        steps.Add(CreateActionStep(TutorialPartId.Cards, "part4_step_10", "Now click Play to enter card targeting mode.", "PlayButton", TutorialActionType.PlayCard, false, TutorialActionType.PlayCard));
        steps.Add(CreateActionStep(TutorialPartId.Cards, "part4_step_11", "Lock Gate closes a route and can force enemies to take a different path. Select a valid gate target first.", "Gate", TutorialActionType.SelectTarget, false, TutorialActionType.SelectTarget));
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_12", "Confirm applies Lock Gate to the selected target. Cancel returns the card to your hand without using it.", "TargetingConfirmButton"));
        steps.Add(CreateActionStep(TutorialPartId.Cards, "part4_step_13", "Now click Confirm to lock the selected gate.", "TargetingConfirmButton", TutorialActionType.LockGate, false, TutorialActionType.LockGate));
        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_14", "Next, you will practice the remaining card effects. Use Skip if you want to skip the rest of the card demonstrations.", null));

        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_15", "Open Gate reopens a route after a gate has been locked.", "Gate"));
        AddDetailedCardDemoSteps(steps, 16, "Open Gate", "OpenGate", "Gate", TutorialActionType.OpenGate, "Open Gate reopens a closed route so enemies can use that path again.", "Now click Confirm to open the selected gate.");

        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_22", "Tile control cards influence expansion and territory.", "ClaimableLand"));
        AddDetailedCardDemoSteps(steps, 23, "Freeze Claim", "FreezeClaim", "ClaimableLand", TutorialActionType.FreezeClaim, "Freeze Claim seals a land tile so normal land actions cannot use it until the effect clears.", "Now click Confirm to freeze the selected tile.");
        AddDetailedCardDemoSteps(steps, 29, "Take Over", "TakeOver", "PublicBuildArea", TutorialActionType.TakeOver, "Take Over captures an opponent's owned tile and transfers control to you.", "Now click Confirm to take control of the selected tile.");

        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_35", "Combat cards help defend difficult waves.", "BuiltTower"));
        AddDetailedCardDemoSteps(steps, 36, "Power Boost", "PowerBoost", "BuiltTower", TutorialActionType.PowerBoost, "Power Boost strengthens one of your towers for the next wave.", "Now click Confirm to apply Power Boost to the selected tower.");
        AddDetailedCardDemoSteps(steps, 42, "Shock Trap", "ShockTrap", "ShockTrapNode", TutorialActionType.PlaceShockTrap, "Shock Trap places a trap on the path that damages enemies when they move onto it.", "Now click Confirm to place Shock Trap at the selected position.");

        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_48", "Interaction cards directly affect opponents.", "OpponentCastle"));
        AddDetailedCardDemoSteps(steps, 49, "Disrupt", "Disrupt", "OpponentCastle", TutorialActionType.Disrupt, "Disrupt blocks the chosen opponent from using card actions on their next turn.", "Now click Confirm to apply Disrupt to the selected opponent.");
        AddDetailedCardDemoSteps(steps, 55, "Steal Card", "StealCard", "OpponentCastle", TutorialActionType.StealCard, "Steal Card takes one random card from the chosen opponent and adds it to your hand.", "Now click Confirm to steal a card from the selected opponent.");
        AddDetailedCardDemoSteps(steps, 61, "Trade Hands", "TradeHands", "OpponentCastle", TutorialActionType.TradeHands, "Trade Hands swaps your current hand with the chosen opponent's hand.", "Now click Confirm to swap hands with the selected opponent.");

        steps.Add(CreateInfoStep(TutorialPartId.Cards, "part4_step_66", "Cards create powerful strategic opportunities. Experiment with different combinations to control the battlefield.", null));

        parts.Add(part4);
    }

    private void BuildDefaultPart5Definition()
    {
        if (parts == null)
        {
            parts = new List<TutorialPartDefinition>();
        }

        TutorialPartDefinition part5 = new TutorialPartDefinition
        {
            partId = TutorialPartId.EndTurn,
            displayTitle = "End Turn and Enemy Waves",
            steps = new List<TutorialStep>
            {
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_1", "When you finish your turn, press End Turn.", "EndTurnButton"),
                CreateActionStep(TutorialPartId.EndTurn, "part5_step_2", "Click End Turn now.", "EndTurnButton", TutorialActionType.EndTurnClicked, false, TutorialActionType.EndTurnClicked),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_3", "The turn moves to the next player.", "TurnIndicator"),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_4", "When every player has completed their turn, an Enemy Wave begins.", "WaveIndicator"),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_5", "Enemy Waves occur after all players finish their turns.", "WaveIndicator"),
                CreateEnemyDemoStep(TutorialPartId.EndTurn, "part5_step_6", "Enemy Waves send enemies toward player castles.", null, 4),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_7", "Towers automatically attack enemies.", "BuiltTower"),
                CreateWaveLockStep(),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_9", "The wave has ended.", null),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_10", "A new round begins.", "RoundText"),
                CreateInfoStep(TutorialPartId.EndTurn, "part5_step_11", "Players take turns again after the wave.", "TurnIndicator")
            }
        };

        parts.Add(part5);
    }

    private void AddDetailedCardDemoSteps(
        List<TutorialStep> steps,
        int startIndex,
        string displayName,
        string tutorialCardId,
        string targetHighlightId,
        TutorialActionType resolutionActionType,
        string effectDescription,
        string resolutionMessage)
    {
        steps.Add(CreateCardActionStep(
            TutorialPartId.Cards,
            "part4_step_" + startIndex,
            "Click " + displayName + " in your hand to select it.",
            "CardHand",
            TutorialActionType.SelectCard,
            tutorialCardId,
            false,
            TutorialActionType.SelectCard));

        steps.Add(CreateInfoStep(
            TutorialPartId.Cards,
            "part4_step_" + (startIndex + 1),
            displayName + " is now selected and raised above its slot.",
            "CardHand"));

        steps.Add(CreateActionStep(
            TutorialPartId.Cards,
            "part4_step_" + (startIndex + 2),
            "Now click Play to begin using " + displayName + ".",
            "PlayButton",
            TutorialActionType.PlayCard,
            false,
            TutorialActionType.PlayCard));

        steps.Add(CreateActionStep(
            TutorialPartId.Cards,
            "part4_step_" + (startIndex + 3),
            effectDescription + " Select a valid target first.",
            targetHighlightId,
            TutorialActionType.SelectTarget,
            false,
            TutorialActionType.SelectTarget));

        steps.Add(CreateInfoStep(
            TutorialPartId.Cards,
            "part4_step_" + (startIndex + 4),
            "Confirm will apply " + displayName + " to the selected target, and Cancel will return it to your hand.",
            "TargetingConfirmButton"));

        steps.Add(CreateActionStep(
            TutorialPartId.Cards,
            "part4_step_" + (startIndex + 5),
            resolutionMessage,
            "TargetingConfirmButton",
            resolutionActionType,
            false,
            resolutionActionType));
    }

    private void SetTutorialHand(TutorialStep step, params string[] cardIds)
    {
        if (step == null)
        {
            return;
        }

        step.tutorialHandCardIds = cardIds != null
            ? new List<string>(cardIds)
            : new List<string>();
    }

    private TutorialStep CreateInfoStep(TutorialPartId partId, string stepId, string message, string highlightTargetId)
    {
        return new TutorialStep
        {
            partId = partId,
            stepId = stepId,
            messageText = message,
            highlightTargetId = highlightTargetId,
            requiresPlayerAction = false,
            expectedActionType = TutorialActionType.None
        };
    }

    private TutorialStep CreateInfoStep(string stepId, string message, string highlightTargetId)
    {
        return CreateInfoStep(TutorialPartId.PlayerInfoAndScore, stepId, message, highlightTargetId);
    }

    private TutorialStep CreateActionStep(
        TutorialPartId partId,
        string stepId,
        string message,
        string highlightTargetId,
        TutorialActionType completionActionType,
        bool requireExactTarget,
        params TutorialActionType[] allowedActions)
    {
        TutorialStep step = CreateInfoStep(partId, stepId, message, highlightTargetId);
        step.requiresPlayerAction = true;
        step.requireExactTarget = requireExactTarget;
        step.expectedActionType = completionActionType;

        if (allowedActions != null && allowedActions.Length > 0)
        {
            step.allowedActionTypes = new List<TutorialActionType>(allowedActions);
        }

        return step;
    }

    private TutorialStep CreateActionStep(
        string stepId,
        string message,
        string highlightTargetId,
        TutorialActionType completionActionType,
        bool requireExactTarget,
        params TutorialActionType[] allowedActions)
    {
        return CreateActionStep(
            TutorialPartId.PlayerInfoAndScore,
            stepId,
            message,
            highlightTargetId,
            completionActionType,
            requireExactTarget,
            allowedActions);
    }

    private TutorialStep CreateCardActionStep(
        TutorialPartId partId,
        string stepId,
        string message,
        string highlightTargetId,
        TutorialActionType completionActionType,
        string tutorialCardId,
        bool prepareSelectedCard,
        params TutorialActionType[] allowedActions)
    {
        TutorialStep step = CreateActionStep(
            partId,
            stepId,
            message,
            highlightTargetId,
            completionActionType,
            false,
            allowedActions);

        step.tutorialCardId = tutorialCardId;
        step.prepareTutorialCardOnEnter = prepareSelectedCard;
        step.resetTurnActionsOnEnter = true;
        step.fallbackAutoAdvanceSeconds = IsSpecificCardAction(completionActionType) ? 12f : 0f;
        return step;
    }

    private TutorialStep CreateCardActionStep(
        string stepId,
        string message,
        string highlightTargetId,
        TutorialActionType completionActionType,
        string tutorialCardId,
        bool prepareSelectedCard,
        params TutorialActionType[] allowedActions)
    {
        return CreateCardActionStep(
            TutorialPartId.PlayerInfoAndScore,
            stepId,
            message,
            highlightTargetId,
            completionActionType,
            tutorialCardId,
            prepareSelectedCard,
            allowedActions);
    }

    private TutorialStep CreateEnemyDemoStep(TutorialPartId partId, string stepId, string message, string highlightTargetId, int enemyCount)
    {
        TutorialStep step = CreateInfoStep(partId, stepId, message, highlightTargetId);
        step.tutorialEnemySpawnCount = Mathf.Max(1, enemyCount);
        return step;
    }

    private TutorialStep CreateWaveLockStep()
    {
        TutorialStep step = CreateActionStep(
            TutorialPartId.EndTurn,
            "part5_step_8",
            "Players cannot perform actions during Enemy Waves.",
            null,
            TutorialActionType.WaveCompleted,
            false,
            TutorialActionType.SelectLand,
            TutorialActionType.SelectOwnedLand,
            TutorialActionType.InspectTower,
            TutorialActionType.PrepareUpgradeTower,
            TutorialActionType.PrepareSellTower,
            TutorialActionType.BuyLand,
            TutorialActionType.BuildTower,
            TutorialActionType.UpgradeTower,
            TutorialActionType.SellTower,
            TutorialActionType.DrawCard,
            TutorialActionType.DiscardCard,
            TutorialActionType.PlayCard,
            TutorialActionType.SelectCard,
            TutorialActionType.EndTurnClicked);

        step.blockedMessageOverride = TutorialWaveBlockedMessage;
        step.fallbackAutoAdvanceSeconds = 10f;
        return step;
    }

    private TutorialStep CreateHiddenEnemyWaveStep(TutorialPartId partId, string stepId, int enemyCount, float fallbackAutoAdvanceSeconds)
    {
        TutorialStep step = CreateWaitStep(partId, stepId, string.Empty, TutorialActionType.WaveCompleted, fallbackAutoAdvanceSeconds);
        step.tutorialEnemySpawnCount = Mathf.Max(1, enemyCount);
        step.hideTutorialUIOnEnter = true;
        return step;
    }

    private TutorialStep CreateWaitStep(TutorialPartId partId, string stepId, string message, TutorialActionType completionActionType, float fallbackAutoAdvanceSeconds)
    {
        TutorialStep step = CreateInfoStep(partId, stepId, message, null);
        step.requiresPlayerAction = true;
        step.expectedActionType = completionActionType;
        step.allowedActionTypes = new List<TutorialActionType> { completionActionType };
        step.fallbackAutoAdvanceSeconds = fallbackAutoAdvanceSeconds;
        return step;
    }

    private TutorialStep CreateWaitStep(string stepId, string message, TutorialActionType completionActionType, float fallbackAutoAdvanceSeconds)
    {
        return CreateWaitStep(
            TutorialPartId.PlayerInfoAndScore,
            stepId,
            message,
            completionActionType,
            fallbackAutoAdvanceSeconds);
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

    private bool DoesBindingMatchPath(string targetId, string hierarchyPath)
    {
        GameObject expectedObject = FindSceneObjectByPath(hierarchyPath);
        GameObject actualObject = ResolveBindingTarget(targetId);
        return expectedObject != null && actualObject == expectedObject;
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

            bool isEmptyStepGroup = part.steps == null || part.steps.Count == 0;

            if (part.partId == TutorialPartId.PlayerInfoAndScore ||
                part.partId == TutorialPartId.LandTowerGold ||
                part.partId == TutorialPartId.Cards ||
                part.partId == TutorialPartId.EndTurn ||
                part.partId == TutorialPartId.EnemyWave ||
                part.partId == TutorialPartId.PlayersTurnsScoring ||
                isEmptyStepGroup)
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

        EnsureGuideSceneCardDemoBoardState(step);
        EnsureGuideScenePart5State(step);

        if (step.resetTurnActionsOnEnter)
        {
            TurnManager turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindObjectOfType<TurnManager>();
            turnManager?.StartTurn(false);
        }

        if ((step.tutorialHandCardIds != null && step.tutorialHandCardIds.Count > 0) ||
            !string.IsNullOrWhiteSpace(step.tutorialCardId))
        {
            ResetTutorialCardAction();

            bool hasTutorialHand = step.tutorialHandCardIds != null && step.tutorialHandCardIds.Count > 0;
            bool prepared = step.prepareTutorialCardOnEnter
                ? PrepareTutorialCardDemo(step.tutorialCardId, hasTutorialHand ? step.tutorialHandCardIds : null)
                : hasTutorialHand
                    ? ForceTutorialHand(step.tutorialHandCardIds)
                    : ForceSingleTutorialCard(step.tutorialCardId);

            if (!prepared)
            {
                Debug.LogWarning("Could not prepare tutorial card: " + step.tutorialCardId);
            }
        }

        if (step.tutorialEnemySpawnCount > 0)
        {
            bool shouldSpawnTutorialEnemies = !(step.partId == TutorialPartId.EndTurn &&
                                                step.stepId == "part5_step_6" &&
                                                guideSceneWaveTutorialEnemiesSpawned);

            if (shouldSpawnTutorialEnemies)
            {
                SpawnTutorialWeakEnemies(step.tutorialEnemySpawnCount);

                if (step.partId == TutorialPartId.EndTurn && step.stepId == "part5_step_6")
                {
                    guideSceneWaveTutorialEnemiesSpawned = true;
                }
            }
        }

        if (step.requiresPlayerAction && step.fallbackAutoAdvanceSeconds > 0f)
        {
            currentStepRoutine = StartCoroutine(FallbackAdvanceRoutine(step, step.fallbackAutoAdvanceSeconds));
        }
    }

    private void EnsureGuideSceneCardDemoBoardState(TutorialStep step)
    {
        if (!IsGuideScene() || step == null || step.partId != TutorialPartId.Cards)
        {
            return;
        }

        string cardId = !string.IsNullOrWhiteSpace(step.tutorialCardId)
            ? step.tutorialCardId
            : InferTutorialCardIdFromStep(step.stepId);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        BuildTowerManager buildTowerManager = BuildTowerManager.Instance != null
            ? BuildTowerManager.Instance
            : FindObjectOfType<BuildTowerManager>();

        if (playerManager == null || buildTowerManager == null)
        {
            return;
        }

        int currentPlayerId = playerManager.GetCurrentPlayerId();
        PlayerResource opponent = GetFirstOtherActivePlayer(playerManager, currentPlayerId);

        if (opponent == null)
        {
            return;
        }

        TowerBuildArea ownedTowerArea = ResolveBuildAreaByPath(tutorialOwnedTowerAreaPath);
        TowerBuildArea opponentLandArea = ResolveBuildAreaByPath(tutorialOpponentLandAreaPath);
        TowerBuildArea opponentTowerArea = ResolveBuildAreaByPath(tutorialOpponentTowerAreaPath);

        EnsureBuildAreaOwnedByPlayer(ownedTowerArea, currentPlayerId, playerManager, false);
        EnsureBuildAreaOwnedByPlayer(opponentLandArea, opponent.playerId, playerManager, false);
        EnsureBuildAreaOwnedByPlayer(opponentTowerArea, opponent.playerId, playerManager, true);
        EnsureTowerForPlayer(ownedTowerArea, currentPlayerId, buildTowerManager);
        EnsureTowerForPlayer(opponentTowerArea, opponent.playerId, buildTowerManager);

        if (ownedTowerArea != null)
        {
            SetOrAddBinding("TowerBuildArea", ownedTowerArea.gameObject);

            GameObject ownedTowerHighlight = ownedTowerArea.currentTower != null
                ? ResolveTutorialTowerHighlightTarget(ownedTowerArea.currentTower)
                : ownedTowerArea.gameObject;

            if (ownedTowerHighlight != null)
            {
                lastBuiltTowerObject = ownedTowerHighlight;
                lastTutorialBuildAreaObject = ownedTowerArea.gameObject;
                SetOrAddBinding("BuiltTower", ownedTowerHighlight);
            }
        }

        if (opponentLandArea != null)
        {
            SetOrAddBinding("ClaimableLand", opponentLandArea.gameObject);
        }

        if (opponentTowerArea != null)
        {
            SetOrAddBinding("PublicBuildArea", opponentTowerArea.gameObject);
        }
    }

    private void EnsureGuideScenePart5State(TutorialStep step)
    {
        if (!IsGuideScene() || step == null || step.partId != TutorialPartId.EndTurn)
        {
            return;
        }

        EnsureGuideSceneWaveTutorialBoardState();
        UpdateGuideScenePart5TurnIndicatorBinding(step.stepId);

        switch (step.stepId)
        {
            case "part5_step_1":
                if (!guideSceneWaveTutorialStarted && !guideSceneWaveTutorialCompleted)
                {
                    ResetGuideScenePart5StateToPlayerTurn();
                }
                break;

            case "part5_step_4":
                StartGuideSceneTutorialWaveState();
                break;

            case "part5_step_5":
                if (!guideSceneWaveTutorialStarted)
                {
                    StartGuideSceneTutorialWaveState();
                }
                break;

            case "part5_step_9":
            case "part5_step_10":
            case "part5_step_11":
                if (guideSceneWaveTutorialStarted && !guideSceneWaveTutorialCompleted)
                {
                    CompleteGuideSceneTutorialWaveState(true);
                }
                break;
        }
    }

    private void EnsureGuideSceneWaveTutorialBoardState()
    {
        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        BuildTowerManager buildTowerManager = BuildTowerManager.Instance != null
            ? BuildTowerManager.Instance
            : FindObjectOfType<BuildTowerManager>();

        if (playerManager == null || buildTowerManager == null)
        {
            return;
        }

        PlayerResource currentPlayer = GetFirstActivePlayer(playerManager);
        PlayerResource opponent = currentPlayer != null ? GetFirstOtherActivePlayer(playerManager, currentPlayer.playerId) : null;

        if (currentPlayer == null || opponent == null)
        {
            return;
        }

        TowerBuildArea ownedTowerArea = ResolveBuildAreaByPath(tutorialOwnedTowerAreaPath);
        TowerBuildArea opponentLandArea = ResolveBuildAreaByPath(tutorialOpponentLandAreaPath);
        TowerBuildArea opponentTowerArea = ResolveBuildAreaByPath(tutorialOpponentTowerAreaPath);

        EnsureBuildAreaOwnedByPlayer(ownedTowerArea, currentPlayer.playerId, playerManager, false);
        EnsureBuildAreaOwnedByPlayer(opponentLandArea, opponent.playerId, playerManager, false);
        EnsureBuildAreaOwnedByPlayer(opponentTowerArea, opponent.playerId, playerManager, true);
        EnsureTowerForPlayer(ownedTowerArea, currentPlayer.playerId, buildTowerManager);
        EnsureTowerForPlayer(opponentTowerArea, opponent.playerId, buildTowerManager);

        if (ownedTowerArea != null)
        {
            SetOrAddBinding("TowerBuildArea", ownedTowerArea.gameObject);

            GameObject builtTowerTarget = ownedTowerArea.currentTower != null
                ? ResolveTutorialTowerHighlightTarget(ownedTowerArea.currentTower)
                : ownedTowerArea.gameObject;

            if (builtTowerTarget != null)
            {
                lastBuiltTowerObject = builtTowerTarget;
                lastTutorialBuildAreaObject = ownedTowerArea.gameObject;
                SetOrAddBinding("BuiltTower", builtTowerTarget);
            }
        }
    }

    private void ResetGuideScenePart5StateToPlayerTurn()
    {
        guideSceneWaveTutorialStarted = false;
        guideSceneWaveTutorialCompleted = false;
        guideSceneWaveTutorialRemainingTurnsSimulated = false;
        guideSceneWaveTutorialEnemiesSpawned = false;
        tutorialEnemyDemoSpawner?.ClearTutorialEnemies();
        ResetTutorialCardAction();
        cardDrawManager?.CancelPendingCard();

        GamePhaseManager gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        TurnManager turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindObjectOfType<TurnManager>();
        PlayerResource firstPlayer = GetFirstActivePlayer(playerManager);

        if (gamePhaseManager != null)
        {
            gamePhaseManager.currentPhase = GamePhase.PlayerPhase;
        }

        if (firstPlayer != null)
        {
            SetGuideSceneCurrentPlayer(firstPlayer.playerId, playerManager, turnManager, true);
        }
    }

    private void SimulateGuideSceneRemainingTurnsForWaveTutorial()
    {
        if (guideSceneWaveTutorialRemainingTurnsSimulated)
        {
            return;
        }

        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        TurnManager turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindObjectOfType<TurnManager>();
        PlayerResource lastPlayer = GetLastActivePlayer(playerManager);

        if (lastPlayer == null)
        {
            return;
        }

        SetGuideSceneCurrentPlayer(lastPlayer.playerId, playerManager, turnManager, true);
        guideSceneWaveTutorialRemainingTurnsSimulated = true;
    }

    private void StartGuideSceneTutorialWaveState()
    {
        if (guideSceneWaveTutorialStarted)
        {
            return;
        }

        guideSceneWaveTutorialStarted = true;
        guideSceneWaveTutorialCompleted = false;

        GamePhaseManager gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        BuildTowerManager buildTowerManager = BuildTowerManager.Instance != null
            ? BuildTowerManager.Instance
            : FindObjectOfType<BuildTowerManager>();

        if (gamePhaseManager != null)
        {
            gamePhaseManager.currentPhase = GamePhase.WavePhase;
        }

        buildTowerManager?.HideBuildInteractionUI();
        cardDrawManager?.CancelPendingCard();
        CurrentTurnIndicatorManager.Instance?.HideAllMarkers();

        if (waveManager != null)
        {
            int currentWave = gamePhaseManager != null ? gamePhaseManager.currentWaveIndex : 1;
            waveManager.ShowWaveIncoming(currentWave);
        }
    }

    private void CompleteGuideSceneTutorialWaveState(bool forceClearEnemies)
    {
        if (!guideSceneWaveTutorialStarted || guideSceneWaveTutorialCompleted)
        {
            return;
        }

        guideSceneWaveTutorialCompleted = true;

        if (forceClearEnemies)
        {
            tutorialEnemyDemoSpawner?.ClearTutorialEnemies();
        }

        GamePhaseManager gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        PlayerManager playerManager = FindObjectOfType<PlayerManager>();
        TurnManager turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindObjectOfType<TurnManager>();
        PlayerResource firstPlayer = GetFirstActivePlayer(playerManager);

        if (gamePhaseManager != null)
        {
            gamePhaseManager.currentPhase = GamePhase.PlayerPhase;

            if (gamePhaseManager.currentWaveIndex < gamePhaseManager.maxWaves)
            {
                gamePhaseManager.currentRound += 1;
                gamePhaseManager.currentWaveIndex += 1;
            }
        }

        waveManager?.RefreshWaveCounterUI();

        if (firstPlayer != null)
        {
            SetGuideSceneCurrentPlayer(firstPlayer.playerId, playerManager, turnManager, true);
        }
    }

    private void SetGuideSceneCurrentPlayer(int playerId, PlayerManager playerManager, TurnManager turnManager, bool restartTurn)
    {
        if (playerManager == null)
        {
            return;
        }

        playerManager.currentPlayerId = playerId;

        if (turnManager != null)
        {
            turnManager.currentPlayerId = playerId;

            if (restartTurn)
            {
                turnManager.StartTurn(false);
            }
        }

        playerManager.RefreshCurrentPlayerUI();
        CurrentTurnIndicatorManager.Instance?.UpdateCurrentTurnIndicator(playerId);
    }

    private void UpdateGuideScenePart5TurnIndicatorBinding(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
        {
            return;
        }

        string markerPath = null;

        switch (stepId)
        {
            case "part5_step_1":
            case "part5_step_2":
                markerPath = ForcedGuideSceneTurnMarkerPath;
                break;
            case "part5_step_3":
                markerPath = ForcedGuideSceneTurnMarkerTopRightPath;
                break;
            case "part5_step_10":
            case "part5_step_11":
                markerPath = ForcedGuideSceneTurnMarkerPath;
                break;
        }

        if (string.IsNullOrWhiteSpace(markerPath))
        {
            return;
        }

        GameObject markerObject = FindSceneObjectByPath(markerPath);

        if (markerObject != null)
        {
            SetOrAddBinding("TurnIndicator", markerObject);
        }
    }

    private bool IsGuideScenePart5WaveStepActive()
    {
        TutorialStep step = CurrentStep;
        return IsGuideScene() &&
               step != null &&
               step.partId == TutorialPartId.EndTurn &&
               !string.IsNullOrWhiteSpace(step.stepId) &&
               step.stepId.StartsWith("part5_step_");
    }

    private bool IsGuideSceneTutorialWaveInteractionBlocked()
    {
        return IsGuideScenePart5WaveStepActive() &&
               guideSceneWaveTutorialStarted &&
               !guideSceneWaveTutorialCompleted;
    }

    private string InferTutorialCardIdFromStep(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
        {
            return null;
        }

        switch (stepId)
        {
            case "part4_step_7":
            case "part4_step_8":
            case "part4_step_9":
            case "part4_step_10":
            case "part4_step_11":
            case "part4_step_12":
            case "part4_step_13":
                return "LockGate";
            default:
                return null;
        }
    }

    private PlayerResource GetFirstOtherActivePlayer(PlayerManager playerManager, int currentPlayerId)
    {
        if (playerManager == null || playerManager.players == null)
        {
            return null;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null && !player.isEliminated && player.playerId != currentPlayerId)
            {
                return player;
            }
        }

        return null;
    }

    private PlayerResource GetFirstActivePlayer(PlayerManager playerManager)
    {
        if (playerManager == null || playerManager.players == null)
        {
            return null;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null && !player.isEliminated && player.playerType != PlayerType.Empty)
            {
                return player;
            }
        }

        return null;
    }

    private PlayerResource GetLastActivePlayer(PlayerManager playerManager)
    {
        if (playerManager == null || playerManager.players == null)
        {
            return null;
        }

        for (int i = playerManager.players.Count - 1; i >= 0; i--)
        {
            PlayerResource player = playerManager.players[i];

            if (player != null && !player.isEliminated && player.playerType != PlayerType.Empty)
            {
                return player;
            }
        }

        return null;
    }

    private TowerBuildArea ResolveBuildAreaByPath(string hierarchyPath)
    {
        GameObject areaObject = FindSceneObjectByPath(hierarchyPath);
        return areaObject != null ? areaObject.GetComponent<TowerBuildArea>() : null;
    }

    private void EnsureBuildAreaOwnedByPlayer(TowerBuildArea buildArea, int playerId, PlayerManager playerManager, bool preserveTower)
    {
        if (buildArea == null)
        {
            return;
        }

        buildArea.activatesNextTurn = false;
        buildArea.inactiveForPlayerId = -1;
        buildArea.ClearFreeze();

        if (buildArea.ownerPlayerId != playerId || !buildArea.isOwned)
        {
            if (!preserveTower)
            {
                buildArea.RemoveCurrentTower();
            }

            buildArea.SetOwner(playerId, playerManager);
        }
    }

    private void EnsureTowerForPlayer(TowerBuildArea buildArea, int playerId, BuildTowerManager buildTowerManager)
    {
        if (buildArea == null || buildTowerManager == null)
        {
            return;
        }

        buildArea.activatesNextTurn = false;
        buildArea.inactiveForPlayerId = -1;

        if (buildArea.currentTower != null && buildArea.towerOwnerPlayerId == playerId)
        {
            return;
        }

        buildArea.RemoveCurrentTower();
        buildTowerManager.TryBuildTowerForPlayer(buildArea, TowerType.Cannon, playerId, false);
    }

    private IEnumerator FallbackAdvanceRoutine(TutorialStep expectedStep, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (tutorialCompleted || !tutorialStarted || CurrentStep != expectedStep)
        {
            yield break;
        }

        if (expectedStep.partId == TutorialPartId.EndTurn &&
            expectedStep.expectedActionType == TutorialActionType.WaveCompleted)
        {
            CompleteGuideSceneTutorialWaveState(true);
        }

        Debug.LogWarning("Tutorial step timed out. Advancing: " + expectedStep.stepId);
        MarkStepCompleted(expectedStep);
        AdvanceToNextStep();
    }

    private void MarkStepCompleted(TutorialStep step)
    {
        if (step == null || string.IsNullOrWhiteSpace(step.stepId))
        {
            return;
        }

        completedStepIds.Add(step.stepId);
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
