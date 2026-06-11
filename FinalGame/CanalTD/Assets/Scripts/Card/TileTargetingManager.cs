/*
 * File: TileTargetingManager.cs
 *
 * Purpose:
 * Implements TileTargetingManager for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TileTargetingManager within the card system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Handle card usage, targeting, hand state, or card-driven map interactions.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Card selections, targeting choices, turn permissions, and player hand data.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Applies card outcomes, targeting results, hand count changes, or card-related restrictions.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify TileTargetingManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileTargetingManager : MonoBehaviour
{
    public enum TileTargetingMode
    {
        None,
        TakeOver,
        FreezeClaim
    }

    [Header("Managers")]
    public PlayerManager playerManager;
    public CardDrawManager cardDrawManager;
    public Camera targetCamera;
    public MonoBehaviour toastMessage;

    [Header("UI")]
    public GameObject darkOverlay;
    public GameObject targetingUI;
    public CanvasGroup normalGameplayUI;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Map Dimming")]
    public Tilemap[] tilemapsToDim;
    public SpriteRenderer[] spritesToDim;
    public Color normalColor = Color.white;
    public Color dimColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Build Areas")]
    public Transform buildAreasParent;
    public Color validHighlightColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.1f, 1f);
    public int validHighlightSortingOrder = 900;
    public int selectedSortingOrder = 1000;

    private readonly List<TowerBuildArea> highlightedAreas = new List<TowerBuildArea>();
    private readonly List<TowerBuildArea> validTargets = new List<TowerBuildArea>();
    private readonly Dictionary<TowerBuildArea, Color> originalColors = new Dictionary<TowerBuildArea, Color>();
    private readonly Dictionary<TowerBuildArea, int> originalSortingOrders = new Dictionary<TowerBuildArea, int>();
    private TowerBuildArea selectedTile;
    private TileTargetingMode currentMode = TileTargetingMode.None;
    private bool isTargeting = false;

    /// <summary>
    /// Finds and stores tile targeting manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    /// <summary>
    /// Subscribes tile targeting manager to the events it needs while enabled.
    /// </summary>
    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelSelection);
        }
    }

    /// <summary>
    /// Unsubscribes tile targeting manager from events so disabled objects stop receiving callbacks.
    /// </summary>
    private void OnDisable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmSelection);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelSelection);
        }
    }

    /// <summary>
    /// Sets up tile targeting manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        ExitTargetingMode();
    }

    /// <summary>
    /// Checks tile targeting manager input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (!isTargeting)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTileAtMouse();
        }
    }

    /// <summary>
    /// Starts Take Over targeting and highlights opponent-owned tiles that can be captured.
    /// </summary>
    public bool BeginTakeOverTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.TakeOver, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        /// <summary>
        /// Handles begin targeting for tile targeting manager.
        /// </summary>
        return BeginTargeting(TileTargetingMode.TakeOver, "No land available to take over.");
    }

    /// <summary>
    /// Starts Freeze Claim targeting and highlights tiles that can be frozen.
    /// </summary>
    public bool BeginFreezeClaimTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.FreezeClaim, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        /// <summary>
        /// Handles begin targeting for tile targeting manager.
        /// </summary>
        return BeginTargeting(TileTargetingMode.FreezeClaim, "No land available to freeze.");
    }

    /// <summary>
    /// Handles begin targeting for card state, hand state, or targeting.
    /// </summary>
    private bool BeginTargeting(TileTargetingMode mode, string noTargetMessage)
    {
        currentMode = mode;
        selectedTile = null;
        highlightedAreas.Clear();
        validTargets.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();

        List<TowerBuildArea> allAreas = GetBuildAreas();
        int currentPlayerId = GetCurrentPlayerId();

        foreach (TowerBuildArea buildArea in allAreas)
        {
            if (IsValidTargetForMode(buildArea, currentPlayerId, mode))
            {
                validTargets.Add(buildArea);
            }
        }

        if (validTargets.Count == 0)
        {
            ShowToast(noTargetMessage);
            currentMode = TileTargetingMode.None;
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);
        UpdateConfirmButtonState();

        foreach (TowerBuildArea buildArea in validTargets)
        {
            if (buildArea == null)
            {
                continue;
            }

            highlightedAreas.Add(buildArea);
            StoreOriginalVisual(buildArea);
            ApplyTileVisual(buildArea, validHighlightColor, validHighlightSortingOrder, false);
        }

        return true;
    }

    /// <summary>
    /// Confirms selection and applies the selected action if it is valid.
    /// </summary>
    public void ConfirmSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (currentMode == TileTargetingMode.TakeOver)
        {
            ConfirmTakeOver();
        }
        else if (currentMode == TileTargetingMode.FreezeClaim)
        {
            ConfirmFreezeClaim();
        }
    }

    /// <summary>
    /// Checks whether selection is allowed before enabling that action.
    /// </summary>
    public void CancelSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        ExitTargetingMode();

        if (cardDrawManager != null)
        {
            cardDrawManager.CancelPendingCard();
        }
    }

    /// <summary>
    /// Checks the current state to decide whether targeting is true.
    /// </summary>
    public bool IsTargeting()
    {
        return isTargeting;
    }

    /// <summary>
    /// Handles exit without consuming card for card state, hand state, or targeting.
    /// </summary>
    public void ExitWithoutConsumingCard()
    {
        ExitTargetingMode();
    }

    /// <summary>
    /// Confirms take over and applies the selected action if it is valid.
    /// </summary>
    private void ConfirmTakeOver()
    {
        if (selectedTile == null)
        {
            ShowToast("Choose a land first.");
            return;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            cardDrawManager != null)
        {
            GameObject pendingCardPrefab = cardDrawManager.GetPendingCardPrefabForTargeting();

            if (pendingCardPrefab == null)
            {
                ShowToast("Could not play this card.");
                return;
            }

            string cardId = CardDrawManager.NormalizeCardId(pendingCardPrefab.name);
            string cardName = pendingCardPrefab.name;
            string targetTileId = selectedTile.name;

            ExitWithoutConsumingCard();

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                cardId,
                cardName,
                targetTileId,
                -1
            );

            if (!requested)
            {
                cardDrawManager.CancelPendingCard();
            }

            return;
        }

        ResolveTakeOver(GetCurrentPlayerId(), selectedTile, true);
    }

    /// <summary>
    /// Confirms freeze claim and applies the selected action if it is valid.
    /// </summary>
    private void ConfirmFreezeClaim()
    {
        if (selectedTile == null)
        {
            ShowToast("Choose a land first.");
            return;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            cardDrawManager != null)
        {
            GameObject pendingCardPrefab = cardDrawManager.GetPendingCardPrefabForTargeting();

            if (pendingCardPrefab == null)
            {
                ShowToast("Could not play this card.");
                return;
            }

            string cardId = CardDrawManager.NormalizeCardId(pendingCardPrefab.name);
            string cardName = pendingCardPrefab.name;
            string targetTileId = selectedTile.name;

            ExitWithoutConsumingCard();

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                cardId,
                cardName,
                targetTileId,
                -1
            );

            if (!requested)
            {
                cardDrawManager.CancelPendingCard();
            }

            return;
        }

        ResolveFreezeClaim(GetCurrentPlayerId(), selectedTile, true);
    }

    /// <summary>
    /// Captures the selected tile for the acting player and updates ownership, cost, and visuals.
    /// </summary>
    public bool ResolveTakeOver(int playerId, TowerBuildArea buildArea)
    {
        /// <summary>
        /// Handles resolve take over for tile targeting manager.
        /// </summary>
        return ResolveTakeOver(playerId, buildArea, false);
    }

    /// <summary>
    /// Captures the selected tile for the acting player and updates ownership, cost, and visuals.
    /// </summary>
    public bool ResolveTakeOver(int playerId, TowerBuildArea buildArea, bool consumePendingCard)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.TakeOver, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (buildArea == null)
        {
            ShowToast("Choose a land first.");
            return false;
        }

        if (buildArea.isFrozenOrSealed)
        {
            ShowToast("This land is frozen.");
            return false;
        }

        if (!IsValidTakeOverTarget(buildArea, playerId))
        {
            ShowToast("Choose an opponent-owned land.");
            return false;
        }

        PlayerResource currentPlayer = GetPlayerResource(playerId);

        if (currentPlayer == null)
        {
            ShowToast("Player resource is missing.");
            return false;
        }

        int takeOverCost = GetTakeOverCost(buildArea);

        if (!currentPlayer.CanAfford(takeOverCost) || !currentPlayer.SpendMoney(takeOverCost))
        {
            ShowToast("Not enough gold to take over this land.");
            return false;
        }

        if (consumePendingCard)
        {
            if (cardDrawManager == null || !cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
            {
                currentPlayer.AddMoney(takeOverCost);
                ShowToast("Could not play this card.");
                return false;
            }
        }

        int previousOwnerId = buildArea.ownerPlayerId;
        string previousOwnerName = playerManager != null ? playerManager.GetPlayerDisplayName(previousOwnerId) : "opponent";

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            currentPlayer.AddMoney(takeOverCost);
            ShowToast("Could not play this card.");
            return false;
        }

        buildArea.RemoveCurrentTower();
        buildArea.ownerPlayerId = playerId;
        buildArea.isOwned = true;
        buildArea.inactiveForPlayerId = playerId;
        buildArea.activatesNextTurn = true;
        buildArea.RefreshOwnershipVisual(playerManager);

        RefreshPlayerUI(playerId);
        ShowToast(currentPlayer.GetDisplayName() + " used Take Over on " + previousOwnerName + "'s land.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.TakeOver, buildArea != null ? buildArea.gameObject : null);

        return true;
    }

    /// <summary>
    /// Freezes the selected tile for the card duration and updates tile visuals and card state.
    /// </summary>
    public bool ResolveFreezeClaim(int playerId, TowerBuildArea buildArea)
    {
        /// <summary>
        /// Handles resolve freeze claim for tile targeting manager.
        /// </summary>
        return ResolveFreezeClaim(playerId, buildArea, false);
    }

    /// <summary>
    /// Freezes the selected tile for the card duration and updates tile visuals and card state.
    /// </summary>
    public bool ResolveFreezeClaim(int playerId, TowerBuildArea buildArea, bool consumePendingCard)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.FreezeClaim, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (!IsValidFreezeClaimTarget(buildArea))
        {
            if (buildArea != null && buildArea.isFrozenOrSealed)
            {
                ShowToast("This land is already frozen.");
            }
            else
            {
                ShowToast("Choose a valid land.");
            }

            return false;
        }

        buildArea.FreezeForPlayer(playerId);

        if (consumePendingCard)
        {
            if (cardDrawManager == null || !cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
            {
                buildArea.ClearFreeze();
                ShowToast("Could not play this card.");
                return false;
            }

            if (!cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
            {
                buildArea.ClearFreeze();
                ShowToast("Could not play this card.");
                return false;
            }
        }

        RefreshPlayerUI(playerId);
        PlayerResource currentPlayer = GetPlayerResource(playerId);
        string actorName = currentPlayer != null ? currentPlayer.GetDisplayName() : "Current player";
        ShowToast(actorName + " used Freeze Claim on a land tile.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.FreezeClaim, buildArea != null ? buildArea.gameObject : null);

        return true;
    }

    /// <summary>
    /// Attempts to select tile at mouse and returns false if rules, resources, or references block it.
    /// </summary>
    private void TrySelectTileAtMouse()
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (IsPointerOverTargetingControls())
        {
            return;
        }

        TowerBuildArea clickedTile = GetTileUnderMouse();

        if (clickedTile != null && clickedTile.isFrozenOrSealed)
        {
            ShowToast(currentMode == TileTargetingMode.FreezeClaim ? "This land is already frozen." : "This land is frozen.");
            return;
        }

        if (clickedTile == null || !validTargets.Contains(clickedTile))
        {
            ShowToast(currentMode == TileTargetingMode.FreezeClaim ? "Choose a valid land." : "Choose an opponent-owned land.");
            return;
        }

        if (selectedTile != null)
        {
            ApplyTileVisual(selectedTile, validHighlightColor, validHighlightSortingOrder, false);
        }

        selectedTile = clickedTile;
        ApplyTileVisual(selectedTile, selectedColor, selectedSortingOrder, true);
        UpdateConfirmButtonState();
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.SelectTarget, selectedTile != null ? selectedTile.gameObject : null);

        Debug.Log($"Selected tile: {selectedTile.name}");
    }

    /// <summary>
    /// Returns tile under mouse used by card handling or target selection.
    /// </summary>
    private TowerBuildArea GetTileUnderMouse()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return null;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            /// <summary>
            /// Handles vector3 for tile targeting manager.
            /// </summary>
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );

        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPosition.x, worldPosition.y));

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            TowerBuildArea buildArea = hit.GetComponent<TowerBuildArea>();

            if (buildArea == null)
            {
                buildArea = hit.GetComponentInParent<TowerBuildArea>();
            }

            if (buildArea != null)
            {
                return buildArea;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns build areas used by card handling or target selection.
    /// </summary>
    private List<TowerBuildArea> GetBuildAreas()
    {
        List<TowerBuildArea> results = new List<TowerBuildArea>();

        if (buildAreasParent != null)
        {
            results.AddRange(buildAreasParent.GetComponentsInChildren<TowerBuildArea>(true));
        }
        else
        {
            results.AddRange(FindObjectsOfType<TowerBuildArea>());
        }

        return results;
    }

    /// <summary>
    /// Checks the current state to decide whether valid take over target is true.
    /// </summary>
    private bool IsValidTakeOverTarget(TowerBuildArea buildArea, int currentPlayerId)
    {
        return buildArea != null && buildArea.CanBeTakenOverBy(currentPlayerId);
    }

    /// <summary>
    /// Checks the current state to decide whether valid freeze claim target is true.
    /// </summary>
    private bool IsValidFreezeClaimTarget(TowerBuildArea buildArea)
    {
        return buildArea != null && buildArea.CanBeFrozen();
    }

    /// <summary>
    /// Checks the current state to decide whether valid target for mode is true.
    /// </summary>
    private bool IsValidTargetForMode(TowerBuildArea buildArea, int currentPlayerId, TileTargetingMode mode)
    {
        if (mode == TileTargetingMode.TakeOver)
        {
            /// <summary>
            /// Handles is valid take over target for tile targeting manager.
            /// </summary>
            return IsValidTakeOverTarget(buildArea, currentPlayerId);
        }

        if (mode == TileTargetingMode.FreezeClaim)
        {
            /// <summary>
            /// Handles is valid freeze claim target for tile targeting manager.
            /// </summary>
            return IsValidFreezeClaimTarget(buildArea);
        }

        return false;
    }

    /// <summary>
    /// Returns take over cost used by card handling or target selection.
    /// </summary>
    private int GetTakeOverCost(TowerBuildArea buildArea)
    {
        if (buildArea == null)
        {
            return 0;
        }

        int normalTakeoverCost = Mathf.CeilToInt(buildArea.landPurchaseCost * 1.5f);
        return Mathf.CeilToInt(normalTakeoverCost / 2f);
    }

    /// <summary>
    /// Returns current player ID used by card handling or target selection.
    /// </summary>
    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : 0;
    }

    /// <summary>
    /// Returns current player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
    }

    /// <summary>
    /// Returns player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetPlayerResource(int playerId)
    {
        return playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
    }

    /// <summary>
    /// Decides whether should block online action should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    /// <summary>
    /// Refreshes player UI from the latest gameplay data.
    /// </summary>
    private void RefreshPlayerUI(int playerId)
    {
        if (playerManager == null)
        {
            return;
        }

        playerManager.RefreshPlayerUI(playerId);
        playerManager.RefreshCurrentPlayerUI();
    }

    /// <summary>
    /// Sets targeting visuals and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetTargetingVisuals(bool active)
    {
        if (active && BuildTowerManager.Instance != null)
        {
            BuildTowerManager.Instance.HideBuildInteractionUI();
        }

        SetNormalGameplayUIEnabled(!active);

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(active);
            SetOverlayRaycastBlocking(active);

            if (active)
            {
                darkOverlay.transform.SetAsLastSibling();
            }
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(active);

            if (active)
            {
                targetingUI.transform.SetAsLastSibling();
            }
        }

        foreach (Tilemap tilemap in tilemapsToDim)
        {
            if (tilemap != null)
            {
                tilemap.color = active ? dimColor : normalColor;
            }
        }

        foreach (SpriteRenderer spriteRenderer in spritesToDim)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = active ? dimColor : normalColor;
            }
        }
    }

    /// <summary>
    /// Sets normal gameplay UI enabled and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetNormalGameplayUIEnabled(bool enabled)
    {
        ResolveNormalGameplayUI();

        if (normalGameplayUI == null)
        {
            return;
        }

        normalGameplayUI.interactable = enabled;
        normalGameplayUI.blocksRaycasts = enabled;
    }

    /// <summary>
    /// Sets overlay raycast blocking and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetOverlayRaycastBlocking(bool blocking)
    {
        if (darkOverlay == null)
        {
            return;
        }

        Image overlayImage = darkOverlay.GetComponent<Image>();

        if (overlayImage != null)
        {
            overlayImage.raycastTarget = blocking;
        }
    }

    /// <summary>
    /// Checks the current state to decide whether pointer over targeting controls is true.
    /// </summary>
    private bool IsPointerOverTargetingControls()
    {
        /// <summary>
        /// Handles is pointer over button for tile targeting manager.
        /// </summary>
        return IsPointerOverButton(confirmButton) || IsPointerOverButton(cancelButton);
    }

    /// <summary>
    /// Checks the current state to decide whether pointer over button is true.
    /// </summary>
    private bool IsPointerOverButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return false;
        }

        Camera uiCamera = null;
        Canvas canvas = button.GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, uiCamera);
    }

    /// <summary>
    /// Looks up the target for normal gameplay UI and applies the resolved gameplay result.
    /// </summary>
    private void ResolveNormalGameplayUI()
    {
        if (normalGameplayUI != null)
        {
            return;
        }

        CanvasGroup[] canvasGroups = FindObjectsOfType<CanvasGroup>(true);

        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup != null && canvasGroup.name == "NormalGameplayUI")
            {
                normalGameplayUI = canvasGroup;
                return;
            }
        }
    }

    /// <summary>
    /// Handles exit targeting mode for card state, hand state, or targeting.
    /// </summary>
    private void ExitTargetingMode()
    {
        ClearHighlights();
        selectedTile = null;
        currentMode = TileTargetingMode.None;
        isTargeting = false;
        SetTargetingVisuals(false);
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// Updates confirm button state so the display or cached state matches current gameplay data.
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null)
        {
            return;
        }

        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialGameplayActive)
        {
            confirmButton.interactable = true;
            return;
        }

        confirmButton.interactable = isTargeting && selectedTile != null;
    }

    /// <summary>
    /// Clears highlights and removes its temporary gameplay or visual effect.
    /// </summary>
    private void ClearHighlights()
    {
        foreach (TowerBuildArea target in highlightedAreas)
        {
            if (target != null)
            {
                target.HideHighlight();
                RestoreTileVisual(target);
            }
        }

        highlightedAreas.Clear();
        validTargets.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();
    }

    /// <summary>
    /// Handles store original visual for card state, hand state, or targeting.
    /// </summary>
    private void StoreOriginalVisual(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.areaVisualRenderer == null)
        {
            return;
        }

        if (!originalColors.ContainsKey(buildArea))
        {
            originalColors.Add(buildArea, buildArea.areaVisualRenderer.color);
        }

        if (!originalSortingOrders.ContainsKey(buildArea))
        {
            originalSortingOrders.Add(buildArea, buildArea.areaVisualRenderer.sortingOrder);
        }
    }

    /// <summary>
    /// Applies tile visual to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyTileVisual(TowerBuildArea buildArea, Color color, int sortingOrder, bool isSelected)
    {
        if (buildArea == null)
        {
            return;
        }

        if (buildArea.areaVisualRenderer == null)
        {
            if (isSelected)
            {
                Debug.LogWarning("Selected tile has no areaVisualRenderer.");
            }

            buildArea.ShowHighlight(color);
            return;
        }

        Color visibleColor = color;
        visibleColor.a = 1f;
        buildArea.areaVisualRenderer.color = visibleColor;
        buildArea.areaVisualRenderer.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Handles restore tile visual for card state, hand state, or targeting.
    /// </summary>
    private void RestoreTileVisual(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.areaVisualRenderer == null)
        {
            return;
        }

        if (originalColors.TryGetValue(buildArea, out Color originalColor))
        {
            buildArea.areaVisualRenderer.color = originalColor;
        }
        else
        {
            buildArea.RefreshOwnershipVisual(playerManager);
        }

        buildArea.RefreshOwnershipVisual(playerManager);

        if (originalSortingOrders.TryGetValue(buildArea, out int originalSortingOrder))
        {
            buildArea.areaVisualRenderer.sortingOrder = originalSortingOrder;
        }
    }

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        if (cardDrawManager != null)
        {
            cardDrawManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryCallToastMethod(string message)
    {
        if (toastMessage == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "ShowMessage",
            "Show",
            "ShowToast",
            "Display",
            "ShowWarningMessage"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = toastMessage.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null
            );

            if (method == null)
            {
                continue;
            }

            method.Invoke(toastMessage, new object[] { message });
            return true;
        }

        return false;
    }
}
