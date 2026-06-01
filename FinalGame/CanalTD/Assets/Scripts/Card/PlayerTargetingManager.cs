/*
 * File: PlayerTargetingManager.cs
 *
 * Purpose:
 * Handles player-targeting tactical cards such as Steal Card, Trade Hands, and
 * Disrupt. It shows the shared targeting overlay, highlights valid player
 * targets, stores the selected player, and applies the card only after Confirm.
 *
 * Notes:
 * Cancel never consumes the pending card. CardDrawManager remains responsible
 * for removing the card from the hand after a successful targeted effect.
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

    private void Start()
    {
        ExitTargetingMode();
    }

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

    public bool BeginStealCardTargeting()
    {
        return BeginTargeting(PlayerTargetingMode.StealCard, "No player has cards to steal.");
    }

    public bool BeginTradeHandsTargeting()
    {
        return BeginTargeting(PlayerTargetingMode.TradeHands, "No player available to trade hands.");
    }

    public bool BeginDisruptTargeting()
    {
        return BeginTargeting(PlayerTargetingMode.Disrupt, "No player available to disrupt.");
    }

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

    public void ConfirmSelection()
    {
        if (!isTargeting)
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

    private void ConfirmStealCard()
    {
        ResolveStealCard(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    private void ConfirmTradeHands()
    {
        ResolveTradeHands(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    private void ConfirmDisrupt()
    {
        ResolveDisrupt(GetCurrentPlayerId(), selectedPlayer != null ? selectedPlayer.playerId : -1, true);
    }

    public bool ResolveStealCard(int playerId, int targetPlayerId)
    {
        return ResolveStealCard(playerId, targetPlayerId, false);
    }

    public bool ResolveStealCard(int playerId, int targetPlayerId, bool consumePendingCard)
    {
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

        return true;
    }

    public bool ResolveTradeHands(int playerId, int targetPlayerId)
    {
        return ResolveTradeHands(playerId, targetPlayerId, false);
    }

    public bool ResolveTradeHands(int playerId, int targetPlayerId, bool consumePendingCard)
    {
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
            currentCards.Remove(playedCardPrefab);
        }

        currentHand.CopyFrom(targetCards);
        targetHand.CopyFrom(currentCards);
        SyncAndRefreshPlayers(currentPlayer, targetPlayer);

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            currentHand.CopyFrom(currentCards);
            targetHand.CopyFrom(targetCards);
            SyncAndRefreshPlayers(currentPlayer, targetPlayer);
            ShowToast("Could not play this card.");
            return false;
        }

        cardDrawManager?.RenderCurrentPlayerHand();
        ShowPublicCardToast(currentPlayer, "Trade Hands", targetPlayer);

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        return true;
    }

    public bool ResolveDisrupt(int playerId, int targetPlayerId)
    {
        return ResolveDisrupt(playerId, targetPlayerId, false);
    }

    public bool ResolveDisrupt(int playerId, int targetPlayerId, bool consumePendingCard)
    {
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

        return true;
    }

    // Shows a public card announcement so every player can see who used a player-targeting card.
    private void ShowPublicCardToast(PlayerResource actor, string cardName, PlayerResource target)
    {
        string actorName = actor != null ? actor.GetDisplayName() : "Current player";
        string targetName = target != null ? target.GetDisplayName() : "target player";
        ShowToast(actorName + " used " + cardName + " on " + targetName + ".");
    }

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

    private PlayerResource GetPlayerResource(int playerId)
    {
        return playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
    }

    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : -1;
    }

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

    public bool IsTargeting()
    {
        return isTargeting;
    }

    public void ExitWithoutConsumingCard()
    {
        ExitTargetingMode();
    }

    private void TrySelectPlayerAtMouse()
    {
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

        Debug.Log("Selected player: " + selectedPlayer.GetDisplayName());
    }

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

        PlayerHand hand = player.GetPlayerHand();
        return hand != null && hand.GetCardCount() > 0;
    }

    private bool IsValidOtherPlayerTarget(PlayerResource player)
    {
        return player != null &&
            playerManager != null &&
            player.playerId != playerManager.GetCurrentPlayerId() &&
            !player.isEliminated;
    }

    private bool IsValidTargetForMode(PlayerResource player, PlayerTargetingMode mode)
    {
        if (mode == PlayerTargetingMode.StealCard)
        {
            return IsValidStealTarget(player);
        }

        if (mode == PlayerTargetingMode.TradeHands)
        {
            return IsValidOtherPlayerTarget(player);
        }

        if (mode == PlayerTargetingMode.Disrupt)
        {
            return IsValidOtherPlayerTarget(player) && !player.HasPendingDisrupt();
        }

        return false;
    }

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

    private bool IsPointerOverTargetingControls()
    {
        return IsPointerOverButton(confirmButton) || IsPointerOverButton(cancelButton);
    }

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

    private void ExitTargetingMode()
    {
        RestoreTargetVisuals();
        selectedPlayer = null;
        currentMode = PlayerTargetingMode.None;
        isTargeting = false;
        SetTargetingVisuals(false);

        // Restore turn markers when exiting targeting mode
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            CurrentTurnIndicatorManager.Instance.ResetMarkersToTurnIndicator();
        }
    }

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
