/*
 * File: PlayerTargetingManager.cs
 *
 * Purpose:
 * Implements PlayerTargetingManager for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PlayerTargetingManager within the card system.
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
 * - Verify PlayerTargetingManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerTargetingManager : MonoBehaviour
{
    private enum PlayerTargetingMode
    {
        None,
        StealCard,
        TradeHands,
        Disrupt
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
    public Color normalMapColor = Color.white;
    public Color dimMapColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Player Target Visuals")]
    public Color validHighlightColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.1f, 1f);
    public int validHighlightSortingOrder = 900;
    public int selectedSortingOrder = 1000;

    [Header("Player Targets")]
    public List<CastleBase> playerTargets = new List<CastleBase>();

    [Header("Action Marker Color")]
    public Color actionMarkerColor = new Color(1f, 0.5f, 0.2f, 1f);

    private readonly List<PlayerResource> validTargets = new List<PlayerResource>();
    private readonly Dictionary<PlayerResource, SpriteRenderer> targetRenderers = new Dictionary<PlayerResource, SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<SpriteRenderer, int> originalSortingOrders = new Dictionary<SpriteRenderer, int>();
    private PlayerResource selectedPlayer;
    private PlayerTargetingMode currentMode = PlayerTargetingMode.None;
    private bool isTargeting = false;

    /// <summary>
    /// Finds and stores player targeting manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    /// <summary>
    /// Subscribes player targeting manager to the events it needs while enabled.
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
    /// Unsubscribes player targeting manager from events so disabled objects stop receiving callbacks.
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
    /// Sets up player targeting manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        ExitTargetingMode();
    }

    /// <summary>
    /// Checks player targeting manager input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (!isTargeting)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectPlayerAtMouse();
        }
    }

    /// <summary>
    /// Handles begin steal card targeting for card state, hand state, or targeting.
    /// </summary>
    public bool BeginStealCardTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.StealCard, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        /// <summary>
        /// Handles begin targeting for player targeting manager.
        /// </summary>
        return BeginTargeting(PlayerTargetingMode.StealCard, "No player has cards to steal.");
    }

    /// <summary>
    /// Handles begin trade hands targeting for card state, hand state, or targeting.
    /// </summary>
    public bool BeginTradeHandsTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.TradeHands, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        /// <summary>
        /// Handles begin targeting for player targeting manager.
        /// </summary>
        return BeginTargeting(PlayerTargetingMode.TradeHands, "No player available to trade hands.");
    }

    /// <summary>
    /// Handles begin disrupt targeting for card state, hand state, or targeting.
    /// </summary>
    public bool BeginDisruptTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.Disrupt, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        /// <summary>
        /// Handles begin targeting for player targeting manager.
        /// </summary>
        return BeginTargeting(PlayerTargetingMode.Disrupt, "No player available to disrupt.");
    }

    /// <summary>
    /// Handles begin targeting for card state, hand state, or targeting.
    /// </summary>
    private bool BeginTargeting(PlayerTargetingMode mode, string noTargetMessage)
    {
        if (playerManager == null)
        {
            ShowToast("Player manager is missing.");
            return false;
        }

        if (mode == PlayerTargetingMode.StealCard)
        {
            PlayerHand currentHand = playerManager.GetCurrentPlayerHand();

            if (currentHand == null)
            {
                ShowToast("Player hand is missing.");
                return false;
            }

            if (!currentHand.CanAddCard())
            {
                ShowToast("Your hand is full.");
                return false;
            }
        }

        selectedPlayer = null;
        currentMode = mode;
        validTargets.Clear();
        targetRenderers.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();

        int currentPlayerId = playerManager.GetCurrentPlayerId();
        List<PlayerResource> candidates = GetTargetCandidates(currentPlayerId);

        foreach (PlayerResource candidate in candidates)
        {
            if (IsValidTargetForMode(candidate, mode))
            {
                validTargets.Add(candidate);
            }
        }

        if (validTargets.Count == 0)
        {
            ShowToast(noTargetMessage);
            currentMode = PlayerTargetingMode.None;
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);
        UpdateConfirmButtonState();

        // Show action markers on valid target castles
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            int[] targetPlayerIds = new int[validTargets.Count];
            for (int i = 0; i < validTargets.Count; i++)
            {
                targetPlayerIds[i] = validTargets[i].playerId;
            }
            CurrentTurnIndicatorManager.Instance.ShowActionMarkers(targetPlayerIds, actionMarkerColor);
        }

        foreach (PlayerResource target in validTargets)
        {
            SpriteRenderer renderer = GetTargetRenderer(target);

            if (renderer == null)
            {
                Debug.LogWarning("No target renderer found for " + target.GetDisplayName() + ".");
                continue;
            }

            targetRenderers[target] = renderer;
            StoreOriginalVisual(renderer);
            ApplyTargetVisual(renderer, validHighlightColor, validHighlightSortingOrder);
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

        if (selectedPlayer == null)
        {
            ShowToast("Choose a player first.");
            return;
        }

        if (currentMode == PlayerTargetingMode.StealCard)
        {
            ConfirmStealCard();
        }
        else if (currentMode == PlayerTargetingMode.TradeHands)
        {
            ConfirmTradeHands();
        }
        else if (currentMode == PlayerTargetingMode.Disrupt)
        {
            ConfirmDisrupt();
        }
    }

    /// <summary>
    /// Confirms steal card and applies the selected action if it is valid.
    /// </summary>
    private void ConfirmStealCard()
    {
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

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                string.Empty,
                selectedPlayer != null ? selectedPlayer.playerId : -1
            );

            if (requested)
            {
                ExitWithoutConsumingCard();
            }

            return;
        }

        ResolveStealCard(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    /// <summary>
    /// Confirms trade hands and applies the selected action if it is valid.
    /// </summary>
    private void ConfirmTradeHands()
    {
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

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                string.Empty,
                selectedPlayer != null ? selectedPlayer.playerId : -1
            );

            if (requested)
            {
                ExitWithoutConsumingCard();
            }

            return;
        }

        ResolveTradeHands(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    /// <summary>
    /// Confirms disrupt and applies the selected action if it is valid.
    /// </summary>
    private void ConfirmDisrupt()
    {
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

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                string.Empty,
                selectedPlayer != null ? selectedPlayer.playerId : -1
            );

            if (requested)
            {
                ExitWithoutConsumingCard();
            }

            return;
        }

        ResolveDisrupt(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    /// <summary>
    /// Looks up the target for steal card and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveStealCard(int playerId, int targetPlayerId)
    {
        /// <summary>
        /// Handles resolve steal card for player targeting manager.
        /// </summary>
        return ResolveStealCard(playerId, targetPlayerId, false);
    }

    /// <summary>
    /// Looks up the target for steal card and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveStealCard(int playerId, int targetPlayerId, bool consumePendingCard)
    {
        PlayerResource tutorialTarget = GetPlayerResource(targetPlayerId);

        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.StealCard, tutorialTarget != null ? tutorialTarget.gameObject : null))
        {
            return false;
        }

        PlayerResource currentPlayer = GetPlayerResource(playerId);
        PlayerResource targetPlayer = GetPlayerResource(targetPlayerId);

        if (currentPlayer == null)
        {
            ShowToast("Player resource is missing.");
            return false;
        }

        PlayerHand currentHand = currentPlayer.GetPlayerHand();
        PlayerHand targetHand = targetPlayer != null ? targetPlayer.GetPlayerHand() : null;

        if (currentHand == null || targetHand == null)
        {
            ShowToast("Player hand is missing.");
            return false;
        }

        if (!currentHand.CanAddCard())
        {
            ShowToast("Your hand is full.");
            return false;
        }

        if (targetHand.GetCardCount() <= 0)
        {
            ShowToast("Target has no cards.");
            return false;
        }

        if (consumePendingCard && !CanConfirmTargetingCard())
        {
            return false;
        }

        GameObject stolenCard = targetHand.RemoveRandomCard();

        if (stolenCard == null)
        {
            ShowToast("Target has no cards.");
            return false;
        }

        if (!currentHand.AddCard(stolenCard))
        {
            targetHand.AddCard(stolenCard);
            ShowToast("Your hand is full.");
            return false;
        }

        SyncAndRefreshPlayers(currentPlayer, targetPlayer);

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            currentHand.RemoveCard(stolenCard);
            targetHand.AddCard(stolenCard);
            SyncAndRefreshPlayers(currentPlayer, targetPlayer);
            ShowToast("Could not play this card.");
            return false;
        }

        cardDrawManager?.RenderCurrentPlayerHand();
        ShowPublicCardToast(currentPlayer, "Steal Card", targetPlayer);

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }
        RefreshVisibleHandAfterPlayerInteraction();

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.StealCard, tutorialTarget != null ? tutorialTarget.gameObject : null);

        return true;
    }

    /// <summary>
    /// Looks up the target for trade hands and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveTradeHands(int playerId, int targetPlayerId)
    {
        /// <summary>
        /// Handles resolve trade hands for player targeting manager.
        /// </summary>
        return ResolveTradeHands(playerId, targetPlayerId, false);
    }

    /// <summary>
    /// Looks up the target for trade hands and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveTradeHands(int playerId, int targetPlayerId, bool consumePendingCard)
    {
        PlayerResource tutorialTarget = GetPlayerResource(targetPlayerId);

        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.TradeHands, tutorialTarget != null ? tutorialTarget.gameObject : null))
        {
            return false;
        }

        PlayerResource currentPlayer = GetPlayerResource(playerId);
        PlayerResource targetPlayer = GetPlayerResource(targetPlayerId);

        if (currentPlayer == null)
        {
            ShowToast("Player resource is missing.");
            return false;
        }

        if (consumePendingCard && !CanConfirmTargetingCard())
        {
            return false;
        }

        PlayerHand currentHand = currentPlayer.GetPlayerHand();
        PlayerHand targetHand = targetPlayer != null ? targetPlayer.GetPlayerHand() : null;

        if (currentHand == null || targetHand == null)
        {
            ShowToast("Player hand is missing.");
            return false;
        }

        List<GameObject> currentCards = new List<GameObject>(currentHand.GetCards());
        List<GameObject> targetCards = new List<GameObject>(targetHand.GetCards());
        GameObject playedCardPrefab = cardDrawManager != null ? cardDrawManager.GetPendingCardPrefabForTargeting() : null;

        if (consumePendingCard && playedCardPrefab != null)
        {
            if (!currentCards.Remove(playedCardPrefab))
            {
                ShowToast("Could not play this card.");
                return false;
            }
        }

        if (consumePendingCard)
        {
            if (cardDrawManager == null ||
                !cardDrawManager.ConsumePendingTargetingCardFromPlayerHand(playerId, true))
            {
                ShowToast("Could not play this card.");
                return false;
            }
        }

        currentHand.CopyFrom(targetCards);
        targetHand.CopyFrom(currentCards);
        SyncAndRefreshPlayers(currentPlayer, targetPlayer);

        ShowPublicCardToast(currentPlayer, "Trade Hands", targetPlayer);

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }
        RefreshVisibleHandAfterPlayerInteraction();

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.TradeHands, tutorialTarget != null ? tutorialTarget.gameObject : null);

        return true;
    }

    /// <summary>
    /// Looks up the target for disrupt and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveDisrupt(int playerId, int targetPlayerId)
    {
        /// <summary>
        /// Handles resolve disrupt for player targeting manager.
        /// </summary>
        return ResolveDisrupt(playerId, targetPlayerId, false);
    }

    /// <summary>
    /// Looks up the target for disrupt and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveDisrupt(int playerId, int targetPlayerId, bool consumePendingCard)
    {
        PlayerResource tutorialTarget = GetPlayerResource(targetPlayerId);

        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.Disrupt, tutorialTarget != null ? tutorialTarget.gameObject : null))
        {
            return false;
        }

        PlayerResource currentPlayer = GetPlayerResource(playerId);
        PlayerResource targetPlayer = GetPlayerResource(targetPlayerId);

        if (targetPlayer == null)
        {
            ShowToast("Choose a player first.");
            return false;
        }

        if (targetPlayer.HasPendingDisrupt())
        {
            ShowToast("This player is already disrupted.");
            return false;
        }

        if (consumePendingCard && !CanConfirmTargetingCard())
        {
            return false;
        }

        targetPlayer.ApplyDisruptNextTurn();

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            targetPlayer.ClearPendingDisrupt();
            ShowToast("Could not play this card.");
            return false;
        }

        SyncAndRefreshPlayers(currentPlayer, targetPlayer);
        ShowPublicCardToast(currentPlayer, "Disrupt", targetPlayer);

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }
        RefreshVisibleHandAfterPlayerInteraction();

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.Disrupt, tutorialTarget != null ? tutorialTarget.gameObject : null);

        return true;
    }

    // Shows a public card announcement so every player can see who used a player-targeting card.
    /// <summary>
    /// Shows public card toast with the correct current context.
    /// </summary>
    private void ShowPublicCardToast(PlayerResource actor, string cardName, PlayerResource target)
    {
        string actorName = actor != null ? actor.GetDisplayName() : "Current player";
        string targetName = target != null ? target.GetDisplayName() : "target player";
        ShowToast(actorName + " used " + cardName + " on " + targetName + ".");
    }

    /// <summary>
    /// Checks whether confirm targeting card is allowed before enabling that action.
    /// </summary>
    private bool CanConfirmTargetingCard()
    {
        if (cardDrawManager == null)
        {
            ShowToast("Card manager is missing.");
            return false;
        }

        if (!cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
        {
            ShowToast("Could not play this card.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetPlayerResource(int playerId)
    {
        return playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
    }

    /// <summary>
    /// Returns current player ID used by card handling or target selection.
    /// </summary>
    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : -1;
    }

    /// <summary>
    /// Decides whether should block online action should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    /// <summary>
    /// Handles sync and refresh players for card state, hand state, or targeting.
    /// </summary>
    private void SyncAndRefreshPlayers(params PlayerResource[] players)
    {
        if (players != null)
        {
            foreach (PlayerResource player in players)
            {
                if (player != null)
                {
                    player.SyncCardCountFromHand();
                }
            }
        }

        if (playerManager != null)
        {
            foreach (PlayerResource player in players)
            {
                if (player != null)
                {
                    playerManager.RefreshPlayerUI(player.playerId);
                }
            }

            playerManager.RefreshCurrentPlayerUI();
        }
    }

    /// <summary>
    /// Refreshes visible hand after player interaction from the latest gameplay data.
    /// </summary>
    private void RefreshVisibleHandAfterPlayerInteraction()
    {
        if (cardDrawManager != null)
        {
            cardDrawManager.RenderCurrentPlayerHand();
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
    /// Attempts to select player at mouse and returns false if rules, resources, or references block it.
    /// </summary>
    private void TrySelectPlayerAtMouse()
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (IsPointerOverTargetingControls())
        {
            return;
        }

        PlayerResource clickedPlayer = GetPlayerUnderMouse();

        if (clickedPlayer == null)
        {
            ShowToast("Choose a player.");
            return;
        }

        if (playerManager != null && clickedPlayer.playerId == playerManager.GetCurrentPlayerId())
        {
            ShowToast("Choose another player.");
            return;
        }

        if (!IsValidTargetForMode(clickedPlayer, currentMode))
        {
            ShowToast(GetInvalidTargetMessage(clickedPlayer));
            return;
        }

        if (!validTargets.Contains(clickedPlayer))
        {
            ShowToast("Choose another player.");
            return;
        }

        if (selectedPlayer != null && targetRenderers.ContainsKey(selectedPlayer))
        {
            ApplyTargetVisual(targetRenderers[selectedPlayer], validHighlightColor, validHighlightSortingOrder);
        }

        selectedPlayer = clickedPlayer;

        if (targetRenderers.TryGetValue(selectedPlayer, out SpriteRenderer selectedRenderer))
        {
            ApplyTargetVisual(selectedRenderer, selectedColor, selectedSortingOrder);
        }

        UpdateConfirmButtonState();
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.SelectTarget, clickedPlayer != null ? clickedPlayer.gameObject : null);

        Debug.Log("Selected player: " + selectedPlayer.GetDisplayName());
    }

    /// <summary>
    /// Returns player under mouse used by card handling or target selection.
    /// </summary>
    private PlayerResource GetPlayerUnderMouse()
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
            /// Handles vector3 for player targeting manager.
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

            CastleBase castle = hit.GetComponent<CastleBase>();

            if (castle == null)
            {
                castle = hit.GetComponentInParent<CastleBase>();
            }

            if (castle != null && IsConfiguredCastleTarget(castle) && castle.ownerResource != null)
            {
                return castle.ownerResource;
            }

            if (playerTargets == null || playerTargets.Count == 0)
            {
                PlayerResource resource = hit.GetComponent<PlayerResource>();

                if (resource == null)
                {
                    resource = hit.GetComponentInParent<PlayerResource>();
                }

                if (resource != null)
                {
                    return resource;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns target candidates used by card handling or target selection.
    /// </summary>
    private List<PlayerResource> GetTargetCandidates(int currentPlayerId)
    {
        List<PlayerResource> candidates = new List<PlayerResource>();

        if (playerTargets != null && playerTargets.Count > 0)
        {
            foreach (CastleBase castle in playerTargets)
            {
                if (castle == null || castle.ownerResource == null)
                {
                    continue;
                }

                if (castle.ownerResource.playerId != currentPlayerId &&
                    !castle.ownerResource.isEliminated &&
                    !candidates.Contains(castle.ownerResource))
                {
                    candidates.Add(castle.ownerResource);
                }
            }

            return candidates;
        }

        return playerManager != null ? playerManager.GetOtherPlayers(currentPlayerId) : candidates;
    }

    /// <summary>
    /// Checks the current state to decide whether configured castle target is true.
    /// </summary>
    private bool IsConfiguredCastleTarget(CastleBase castle)
    {
        if (castle == null)
        {
            return false;
        }

        if (playerTargets == null || playerTargets.Count == 0)
        {
            return true;
        }

        return playerTargets.Contains(castle);
    }

    /// <summary>
    /// Checks the current state to decide whether valid steal target is true.
    /// </summary>
    private bool IsValidStealTarget(PlayerResource player)
    {
        if (player == null || playerManager == null)
        {
            return false;
        }

        if (player.playerId == playerManager.GetCurrentPlayerId() || player.isEliminated)
        {
            return false;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return player.GetDisplayedCardCount() > 0;
        }

        PlayerHand hand = player.GetPlayerHand();
        return hand != null && hand.GetCardCount() > 0;
    }

    /// <summary>
    /// Checks the current state to decide whether valid other player target is true.
    /// </summary>
    private bool IsValidOtherPlayerTarget(PlayerResource player)
    {
        return player != null &&
            playerManager != null &&
            player.playerId != playerManager.GetCurrentPlayerId() &&
            !player.isEliminated;
    }

    /// <summary>
    /// Checks the current state to decide whether valid target for mode is true.
    /// </summary>
    private bool IsValidTargetForMode(PlayerResource player, PlayerTargetingMode mode)
    {
        if (mode == PlayerTargetingMode.StealCard)
        {
            /// <summary>
            /// Handles is valid steal target for player targeting manager.
            /// </summary>
            return IsValidStealTarget(player);
        }

        if (mode == PlayerTargetingMode.TradeHands)
        {
            /// <summary>
            /// Handles is valid other player target for player targeting manager.
            /// </summary>
            return IsValidOtherPlayerTarget(player);
        }

        if (mode == PlayerTargetingMode.Disrupt)
        {
            /// <summary>
            /// Handles is valid other player target for player targeting manager.
            /// </summary>
            return IsValidOtherPlayerTarget(player) && !player.HasPendingDisrupt();
        }

        return false;
    }

    /// <summary>
    /// Returns invalid target message used by card handling or target selection.
    /// </summary>
    private string GetInvalidTargetMessage(PlayerResource player)
    {
        if (player == null)
        {
            return "Choose a player.";
        }

        if (playerManager != null && player.playerId == playerManager.GetCurrentPlayerId())
        {
            return "Choose another player.";
        }

        if (currentMode == PlayerTargetingMode.StealCard)
        {
            return "Target has no cards.";
        }

        if (currentMode == PlayerTargetingMode.Disrupt && player.HasPendingDisrupt())
        {
            return "This player is already disrupted.";
        }

        return "Choose another player.";
    }

    /// <summary>
    /// Returns target renderer used by card handling or target selection.
    /// </summary>
    private SpriteRenderer GetTargetRenderer(PlayerResource player)
    {
        if (player == null)
        {
            return null;
        }

        GameObject targetObject = null;
        CastleBase configuredCastle = GetConfiguredCastleForPlayer(player.playerId);

        if (configuredCastle != null)
        {
            targetObject = configuredCastle.gameObject;
        }

        PlayerVisualConfig config = playerManager != null ? playerManager.GetVisualConfig(player.playerId) : null;

        if (targetObject == null && config != null && config.castleReference != null)
        {
            targetObject = config.castleReference;
        }

        if (targetObject == null)
        {
            targetObject = player.gameObject;
        }

        return targetObject.GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Returns configured castle for player used by card handling or target selection.
    /// </summary>
    private CastleBase GetConfiguredCastleForPlayer(int playerId)
    {
        if (playerTargets == null)
        {
            return null;
        }

        foreach (CastleBase castle in playerTargets)
        {
            if (castle != null && castle.ownerResource != null && castle.ownerResource.playerId == playerId)
            {
                return castle;
            }
        }

        return null;
    }

    /// <summary>
    /// Handles store original visual for card state, hand state, or targeting.
    /// </summary>
    private void StoreOriginalVisual(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (!originalColors.ContainsKey(renderer))
        {
            originalColors[renderer] = renderer.color;
        }

        if (!originalSortingOrders.ContainsKey(renderer))
        {
            originalSortingOrders[renderer] = renderer.sortingOrder;
        }
    }

    /// <summary>
    /// Applies target visual to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyTargetVisual(SpriteRenderer renderer, Color color, int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        Color visibleColor = color;
        visibleColor.a = 1f;
        renderer.color = visibleColor;
        renderer.sortingOrder = sortingOrder;
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
                tilemap.color = active ? dimMapColor : normalMapColor;
            }
        }

        foreach (SpriteRenderer spriteRenderer in spritesToDim)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = active ? dimMapColor : normalMapColor;
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
        /// Handles is pointer over button for player targeting manager.
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
        RestoreTargetVisuals();
        selectedPlayer = null;
        currentMode = PlayerTargetingMode.None;
        isTargeting = false;
        SetTargetingVisuals(false);
        UpdateConfirmButtonState();

        // Restore turn markers when exiting targeting mode
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            CurrentTurnIndicatorManager.Instance.ResetMarkersToTurnIndicator();
        }
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

        confirmButton.interactable = isTargeting && selectedPlayer != null;
    }

    /// <summary>
    /// Handles restore target visuals for card state, hand state, or targeting.
    /// </summary>
    private void RestoreTargetVisuals()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in originalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }

        foreach (KeyValuePair<SpriteRenderer, int> entry in originalSortingOrders)
        {
            if (entry.Key != null)
            {
                entry.Key.sortingOrder = entry.Value;
            }
        }

        validTargets.Clear();
        targetRenderers.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();
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
