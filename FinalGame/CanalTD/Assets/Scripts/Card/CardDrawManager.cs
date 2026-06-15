/*
 * CanalTD - CardDrawManager
 *
 * Purpose:
 * Manages player card drawing, hand state, card play/discard actions, and card targeting flow.
 *
 * Attached GameObject:
 * Configured in the Unity scene / Inspector with card UI, deck, hand, and targeting references.
 *
 * Main responsibilities:
 * - Maintain draw pile, discard pile, hand state, and selected-card state.
 * - Enforce draw/play/discard restrictions from the turn system.
 * - Coordinate targeting confirm/cancel flows for tile, gate, tower, player, and trap effects.
 * - Bridge card state to online synchronization when online mode is active.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Card selections, targeting choices, turn permissions, card action flags, and player hand data.
 *
 * Outputs / effects:
 * - Updates hand/deck/discard state, applies card outcomes, and drives targeting feedback.
 * - Shows warnings or feedback when card actions are invalid.
 *
 * Authorship / assistance:
 * Game design, Unity implementation, integration, and final documentation were developed by Jingyu Pan
 * for an individual coursework submission. AI assistance was used as disclosed in the project documentation.
 *
 * Testing notes:
 * - Manually verify per-turn draw/play/discard limits, hand updates, targeting confirm/cancel, Disrupt blocking, and turn restrictions.
 * - Online card synchronization requires separate manual validation before claiming full stability.
 */
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CardDeckEntry
{
    public GameObject cardPrefab;
    public int copies = 1;
}

public class CardDrawManager : MonoBehaviour
{
    [Header("9 Card Type Prefabs")]
    public GameObject[] cardTypePrefabs;

    [Header("Deck Settings")]
    public List<CardDeckEntry> deckEntries = new List<CardDeckEntry>();
    public int copiesPerCardType = 4;
    public int initialCardsPerPlayer = 2;
    public bool rebuildDeckWhenEmpty = true;

    [Header("Card Slots")]
    public Transform[] cardSlots;

    [Header("Draw / Play Animation")]
    public GameObject drawPileVisual;
    public float animationTime = 0.4f;
    public float playCenterHoldTime = 0.2f;

    [Header("Selection")]
    public float selectedMoveUpDistance = 40f;

    [Header("Warning")]
    public TextMeshProUGUI warningText;
    public float warningTime = 1.2f;

    [Header("Number UI")]
    public PresentTheNumberUI presentTheNumberUI;

    [Header("Turn")]
    public TurnManager turnManager;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Player Manager")]
    public PlayerManager playerManager;

    [Header("Tile Targeting")]
    public TileTargetingManager tileTargetingManager;

    [Header("Player Targeting")]
    public PlayerTargetingManager playerTargetingManager;

    [Header("Tower Targeting")]
    public TowerTargetingManager towerTargetingManager;

    [Header("Shock Trap Targeting")]
    public ShockTrapTargetingManager shockTrapTargetingManager;

    private List<GameObject> runtimeDeck = new List<GameObject>();
    private bool deckInitialized = false;
    private bool initialHandsDealt = false;
    private bool isBusy = false;
    private CardInstanceSelectable selectedCard;
    private CardInstanceSelectable pendingPlayedCard;

    /// <summary>
    /// Subscribes card draw manager to the events it needs while enabled.
    /// </summary>
    private void OnEnable()
    {
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;
        }
    }

    /// <summary>
    /// Unsubscribes card draw manager from events so disabled objects stop receiving callbacks.
    /// </summary>
    private void OnDisable()
    {
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
        }
    }

    /// <summary>
    /// Sets up card draw manager when this scene object starts running.
    /// </summary>
    void Start()
    {
        if (!deckInitialized)
        {
            InitializeDeck();
        }

        RenderCurrentPlayerHand();
    }

    /// <summary>
    /// Builds the runtime draw deck from configured card entries, then shuffles it for play.
    /// </summary>
    public void InitializeDeck()
    {
        runtimeDeck.Clear();

        if (HasConfiguredDeckEntries())
        {
            foreach (CardDeckEntry entry in deckEntries)
            {
                if (entry == null || entry.cardPrefab == null)
                {
                    continue;
                }

                int copyCount = Mathf.Max(0, entry.copies);

                for (int i = 0; i < copyCount; i++)
                {
                    runtimeDeck.Add(entry.cardPrefab);
                }
            }
        }
        else
        {
            BuildDeckFromLegacyPrefabs();
        }

        ShuffleRuntimeDeck();
        deckInitialized = true;
        Debug.Log("Deck ready. Total cards = " + runtimeDeck.Count);
    }

    /// <summary>
    /// Builds the draw deck from the older card prefab array when no deck entries are configured.
    /// </summary>
    private void BuildDeckFromLegacyPrefabs()
    {
        if (cardTypePrefabs == null)
        {
            return;
        }

        foreach (GameObject prefab in cardTypePrefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            for (int i = 0; i < copiesPerCardType; i++)
            {
                runtimeDeck.Add(prefab);
            }
        }
    }

    /// <summary>
    /// Checks whether the Inspector deck list contains at least one usable card prefab.
    /// </summary>
    private bool HasConfiguredDeckEntries()
    {
        if (deckEntries == null || deckEntries.Count == 0)
        {
            return false;
        }

        foreach (CardDeckEntry entry in deckEntries)
        {
            if (entry != null && entry.cardPrefab != null && entry.copies > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Randomizes the runtime deck order so future card draws are not predictable.
    /// </summary>
    private void ShuffleRuntimeDeck()
    {
        for (int i = 0; i < runtimeDeck.Count; i++)
        {
            int randomIndex = Random.Range(i, runtimeDeck.Count);
            GameObject temp = runtimeDeck[i];
            runtimeDeck[i] = runtimeDeck[randomIndex];
            runtimeDeck[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Handles draw card for card state, hand state, or targeting.
    /// </summary>
    public void DrawCard()
    {
        TryDrawCardForCurrentPlayer();
    }

    /// <summary>
    /// Attempts to draw card for current player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryDrawCardForCurrentPlayer()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.DrawCard, null))
        {
            return false;
        }

        if (!CanHumanUseCardsNow(true))
        {
            return false;
        }

        if (IsDisruptedThisTurn())
        {
            StartCoroutine(ShowWarning("You cannot use cards while disrupted."));
            return false;
        }

        if (ShouldUseLegacyPhaseCheck() && gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot use cards during enemy wave.")
                : "You cannot use cards during enemy wave."));
            return false;
        }

        if (isBusy) return false;

        if (IsCurrentHandFull())
        {
            StartCoroutine(ShowWarning("Hand limit reached."));
            return false;
        }

        if (!HasDrawableCard())
        {
            StartCoroutine(ShowWarning("Deck is empty."));
            return false;
        }

        if (ShouldUseAuthoritativeOnlineCardSync())
        {
            return PhotonOnlineCardSyncManager.Instance != null &&
                PhotonOnlineCardSyncManager.Instance.RequestDrawCard();
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumeDraw())
        {
            return false;
        }

        StartCoroutine(DrawCardRoutine());
        return true;
    }

    /// <summary>
    /// Finds the first free card slot where a newly drawn card can be placed.
    /// </summary>
    int FindEmptySlot()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i].Find("CardView") == null)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Animates drawing a card, adds it to the hand, and refreshes the hand UI.
    /// </summary>
    IEnumerator DrawCardRoutine()
    {
        isBusy = true;

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(true);
        }

        /// <summary>
        /// Handles wait for seconds for card draw manager.
        /// </summary>
        yield return new WaitForSeconds(animationTime);

        GameObject cardPrefab = DrawOneCardFromDeck();

        if (cardPrefab == null)
        {
            if (drawPileVisual != null)
            {
                drawPileVisual.SetActive(false);
            }

            StartCoroutine(ShowWarning("Deck is empty."));
            isBusy = false;
            yield break;
        }

        PlayerHand currentHand = GetCurrentPlayerHand();

        if (currentHand != null)
        {
            currentHand.AddCard(cardPrefab);
            SyncCurrentPlayerCardCount();
        }
        else
        {
            int emptySlotIndex = FindEmptySlot();

            if (emptySlotIndex >= 0)
            {
                CreateCardView(cardPrefab, cardSlots[emptySlotIndex]);
            }
        }

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(false);
        }

        Debug.Log("Drew card: " + cardPrefab.name + ". Cards left in deck = " + GetRemainingDeckCount());
        TutorialManager.Instance?.NotifyCardDrawn(cardPrefab);
        RenderCurrentPlayerHand();

        isBusy = false;
    }

    /// <summary>
    /// Deals initial hands to players and refreshes their hand state.
    /// </summary>
    public void DealInitialHands()
    {
        DealInitialHands(initialCardsPerPlayer);
    }

    /// <summary>
    /// Deals initial hands to players and refreshes their hand state.
    /// </summary>
    public void DealInitialHands(int cardsPerPlayer)
    {
        if (initialHandsDealt)
        {
            return;
        }

        if (playerManager == null || playerManager.players == null)
        {
            Debug.LogWarning("Cannot deal initial hands because PlayerManager is not assigned.");
            return;
        }

        if (!deckInitialized)
        {
            InitializeDeck();
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            PlayerHand hand = player.GetPlayerHand();

            if (hand == null)
            {
                continue;
            }

            hand.Clear();

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (!TryAddCardToHand(hand, false))
                {
                    break;
                }
            }

            player.SyncCardCountFromHand();
        }

        initialHandsDealt = true;
        Debug.Log("Initial hands dealt.");
        RenderCurrentPlayerHand();
    }

    /// <summary>
    /// Attempts to draw for current player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryDrawForCurrentPlayer(bool countsAsTurnDraw)
    {
        PlayerResource currentPlayer = GetCurrentPlayerResource();
        return currentPlayer != null && TryDrawForPlayer(currentPlayer.playerId, countsAsTurnDraw);
    }

    /// <summary>
    /// Attempts to draw for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryDrawForPlayer(int playerId, bool countsAsTurnDraw)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null)
        {
            return false;
        }

        if (countsAsTurnDraw)
        {
            TurnManager manager = GetTurnManager();

            if (manager != null && !manager.TryConsumeDraw())
            {
                return false;
            }
        }

        PlayerHand hand = player.GetPlayerHand();
        bool added = TryAddCardToHand(hand, true);

        if (added)
        {
            player.SyncCardCountFromHand();
            TutorialManager.Instance?.NotifyCardDrawn(null);

            if (playerManager != null && playerManager.GetCurrentPlayerId() == playerId)
            {
                RenderCurrentPlayerHand();
            }
            else if (playerManager != null)
            {
                playerManager.RefreshPlayerUI(playerId);
            }

            if (countsAsTurnDraw)
            {
                Debug.Log("Drew card for " + player.GetDisplayName() + ".");
            }
        }
        else if (playerManager != null && playerManager.GetCurrentPlayerId() == playerId)
        {
            RenderCurrentPlayerHand();
        }

        return added;
    }

    /// <summary>
    /// Attempts to add card to hand and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAddCardToHand(PlayerHand hand, bool showWarnings)
    {
        if (hand == null)
        {
            return false;
        }

        if (!hand.CanAddCard())
        {
            if (showWarnings)
            {
                Debug.Log("Hand limit reached.");
                StartCoroutine(ShowWarning("Hand limit reached."));
            }

            return false;
        }

        GameObject cardPrefab = DrawOneCardFromDeck();

        if (cardPrefab == null)
        {
            if (showWarnings)
            {
                StartCoroutine(ShowWarning("Deck is empty."));
            }

            return false;
        }

        return hand.AddCard(cardPrefab);
    }

    /// <summary>
    /// Checks whether select cards is allowed before enabling that action.
    /// </summary>
    public bool CanSelectCards()
    {
        return !isBusy && pendingPlayedCard == null && CanHumanUseCardsNow();
    }

    /// <summary>
    /// Returns hand card count used by card handling or target selection.
    /// </summary>
    public int GetHandCardCount()
    {
        PlayerHand currentHand = GetCurrentPlayerHand();

        if (currentHand != null)
        {
            return currentHand.GetCardCount();
        }

        if (cardSlots == null)
        {
            return 0;
        }

        int count = 0;

        foreach (Transform slot in cardSlots)
        {
            if (slot == null)
            {
                continue;
            }

            Transform cardView = slot.Find("CardView");

            if (cardView != null && cardView.gameObject.activeSelf)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Selects card and updates the related targeting or UI highlight.
    /// </summary>
    public void SelectCard(CardInstanceSelectable card)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.SelectCard, card != null ? card.gameObject : null))
        {
            return;
        }

        if (!CanHumanUseCardsNow(true))
        {
            return;
        }

        if (isBusy) return;
        if (pendingPlayedCard != null) return;

        if (selectedCard == card)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            return;
        }

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = card;
        selectedCard.SetSelected(true);

        Debug.Log("Selected card: " + selectedCard.sourcePrefab.name);
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.SelectCard, selectedCard.gameObject);
    }

    /// <summary>
    /// Handles discard selected card for card state, hand state, or targeting.
    /// </summary>
    public void DiscardSelectedCard()
    {
        TryDiscardSelectedCard();
    }

    /// <summary>
    /// Attempts to discard selected card and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryDiscardSelectedCard()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.DiscardCard, selectedCard != null ? selectedCard.gameObject : null))
        {
            return false;
        }

        if (!CanHumanUseCardsNow(true))
        {
            return false;
        }

        if (IsDisruptedThisTurn())
        {
            StartCoroutine(ShowWarning("You cannot use cards while disrupted."));
            return false;
        }

        if (ShouldUseLegacyPhaseCheck() && gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot use cards during enemy wave.")
                : "You cannot use cards during enemy wave."));
            return false;
        }

        if (isBusy) return false;
        if (pendingPlayedCard != null) return false;

        if (selectedCard == null)
        {
            StartCoroutine(ShowWarning("No card selected!"));
            return false;
        }

        GameObject returnedPrefab = selectedCard.sourcePrefab;

        if (ShouldUseAuthoritativeOnlineCardSync())
        {
            TurnManager authoritativeManager = GetTurnManager();

            if (authoritativeManager != null && !authoritativeManager.CanDiscardCard())
            {
                authoritativeManager.TryConsumeDiscard();
                return false;
            }

            return returnedPrefab != null &&
                PhotonOnlineCardSyncManager.Instance != null &&
                PhotonOnlineCardSyncManager.Instance.RequestDiscardCard(
                    NormalizeCardId(returnedPrefab.name),
                    returnedPrefab.name
                );
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumeDiscard())
        {
            return false;
        }

        if (returnedPrefab != null)
        {
            PlayerHand currentHand = GetCurrentPlayerHand();

            if (currentHand != null)
            {
                currentHand.RemoveCard(returnedPrefab);
                SyncCurrentPlayerCardCount();
            }

            AddCardToDeck(returnedPrefab);
        }

        Debug.Log("Discarded card and returned to deck: " + (returnedPrefab != null ? returnedPrefab.name : "Unknown") + ". Cards in deck = " + GetRemainingDeckCount());

        selectedCard = null;

        RenderCurrentPlayerHand();
        TutorialManager.Instance?.NotifyCardDiscarded(returnedPrefab);
        return true;
    }

    /// <summary>
    /// Plays the selected card audio feedback.
    /// </summary>
    public void PlaySelectedCard()
    {
        TryBeginPlaySelectedCard();
    }

    /// <summary>
    /// Attempts to begin play selected card and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryBeginPlaySelectedCard()
    {
        TutorialActionType tutorialActionType = GetTutorialActionTypeForCardName(
            selectedCard != null && selectedCard.sourcePrefab != null ? selectedCard.sourcePrefab.name : string.Empty
        );

        if (TutorialActionGate.BlockIfNotAllowed(
                tutorialActionType,
                selectedCard != null ? selectedCard.gameObject : null))
        {
            return false;
        }

        if (!CanHumanUseCardsNow(true))
        {
            return false;
        }

        if (IsDisruptedThisTurn())
        {
            StartCoroutine(ShowWarning("You cannot use cards while disrupted."));
            return false;
        }

        if (ShouldUseLegacyPhaseCheck() && gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot use cards during enemy wave.")
                : "You cannot use cards during enemy wave."));
            return false;
        }

        if (isBusy) return false;
        if (pendingPlayedCard != null) return false;

        if (selectedCard == null)
        {
            StartCoroutine(ShowWarning("No card selected!"));
            return false;
        }

        GateActionType gateActionType = GetGateActionType(selectedCard.sourcePrefab.name);

        if (gateActionType != GateActionType.None &&
            (GateTargetingManager.Instance == null ||
             !GateTargetingManager.Instance.HasValidGateTargets(gateActionType)))
        {
            StartCoroutine(ShowWarning("No valid gates."));
            return false;
        }

        if (IsTileTargetingCard(selectedCard.sourcePrefab.name) && tileTargetingManager == null)
        {
            StartCoroutine(ShowWarning("Tile targeting manager is missing."));
            return false;
        }

        if (IsPlayerTargetingCard(selectedCard.sourcePrefab.name) && playerTargetingManager == null)
        {
            StartCoroutine(ShowWarning("Player targeting manager is missing."));
            return false;
        }

        if (IsTowerTargetingCard(selectedCard.sourcePrefab.name) && towerTargetingManager == null)
        {
            StartCoroutine(ShowWarning("Tower targeting manager is missing."));
            return false;
        }

        if (IsShockTrapCard(selectedCard.sourcePrefab.name) && shockTrapTargetingManager == null)
        {
            StartCoroutine(ShowWarning("Shock Trap targeting manager is missing."));
            return false;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.CanPlayCard())
        {
            manager.TryConsumePlayCard();
            return false;
        }

        if (ShouldUseAuthoritativeOnlineCardSync() && selectedCard != null && selectedCard.sourcePrefab != null)
        {
            string selectedCardId = NormalizeCardId(selectedCard.sourcePrefab.name);

            if (!RequiresTargetSelection(selectedCardId))
            {
                return PhotonOnlineCardSyncManager.Instance != null &&
                    PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                        selectedCardId,
                        selectedCard.sourcePrefab.name,
                        string.Empty,
                        -1
                    );
            }
        }

        StartCoroutine(PlayCardRoutine(selectedCard));
        return true;
    }

    /// <summary>
    /// Animates the selected card play before resolving its effect or targeting flow.
    /// </summary>
    IEnumerator PlayCardRoutine(CardInstanceSelectable card)
    {
        isBusy = true;

        RectTransform cardRect = card.GetComponent<RectTransform>();

        if (cardRect == null)
        {
            Debug.LogWarning("Selected card has no RectTransform.");
            isBusy = false;
            yield break;
        }

        string playedCardName = card.sourcePrefab.name;

        Vector3 startPos = cardRect.position;
        Vector3 startScale = cardRect.localScale;
        Quaternion startRotation = cardRect.rotation;

        Vector3 targetPos = startPos;

        if (drawPileVisual != null)
        {
            targetPos = drawPileVisual.transform.position;
        }

        float t = 0f;

        while (t < animationTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / animationTime);

            cardRect.position = Vector3.Lerp(startPos, targetPos, progress);
            cardRect.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            cardRect.rotation = Quaternion.Lerp(
                startRotation,
                Quaternion.Euler(0, 0, 20),
                progress
            );

            yield return null;
        }

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(true);
        }

        /// <summary>
        /// Handles wait for seconds for card draw manager.
        /// </summary>
        yield return new WaitForSeconds(playCenterHoldTime);

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(false);
        }

        Debug.Log("Played card: " + playedCardName);

        GateActionType gateActionType = GetGateActionType(playedCardName);

        if (gateActionType != GateActionType.None)
        {
            if (GateTargetingManager.Instance == null ||
                !GateTargetingManager.Instance.EnterGateTargetMode(gateActionType))
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);

            if (CursorToolManager.Instance != null)
            {
                CursorToolManager.Instance.EnterHammerMode();
            }

            isBusy = false;
            yield break;
        }

        if (IsTakeOverCard(playedCardName))
        {
            if (tileTargetingManager == null ||
                !tileTargetingManager.BeginTakeOverTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsFreezeClaimCard(playedCardName))
        {
            if (tileTargetingManager == null ||
                !tileTargetingManager.BeginFreezeClaimTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsStealCard(playedCardName))
        {
            if (playerTargetingManager == null ||
                !playerTargetingManager.BeginStealCardTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsTradeHandsCard(playedCardName))
        {
            if (playerTargetingManager == null ||
                !playerTargetingManager.BeginTradeHandsTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsDisruptCard(playedCardName))
        {
            if (playerTargetingManager == null ||
                !playerTargetingManager.BeginDisruptTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsPowerBoostCard(playedCardName))
        {
            if (towerTargetingManager == null ||
                !towerTargetingManager.BeginPowerBoostTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        if (IsShockTrapCard(playedCardName))
        {
            Debug.Log("Starting Shock Trap targeting.");

            if (shockTrapTargetingManager == null ||
                !shockTrapTargetingManager.BeginShockTrapTargeting())
            {
                ResetCardRect(cardRect);
                card.SetSelected(true);
                isBusy = false;
                yield break;
            }

            EnterPendingTargetingCard(card);
            isBusy = false;
            yield break;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumePlayCard())
        {
            ResetCardRect(cardRect);
            card.SetSelected(true);
            isBusy = false;
            yield break;
        }

        AudioManager.Instance?.PlayCardPlay();

        selectedCard = null;
        RemoveCardFromCurrentHand(card.sourcePrefab);
        Destroy(card.gameObject);
        RenderCurrentPlayerHand();

        isBusy = false;
    }

    /// <summary>
    /// Handles enter pending targeting card for card state, hand state, or targeting.
    /// </summary>
    private void EnterPendingTargetingCard(CardInstanceSelectable card)
    {
        pendingPlayedCard = card;
        selectedCard = null;

        // Confirm consumes the hidden card; Cancel restores it to the hand slot.
        card.gameObject.SetActive(false);
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.PlayCard, card.gameObject);
    }

    /// <summary>
    /// Confirms pending card and applies the selected action if it is valid.
    /// </summary>
    public void ConfirmPendingCard()
    {
        ConfirmPendingCard(true);
    }

    /// <summary>
    /// Confirms card consume after successful resolution and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmCardConsumeAfterSuccessfulResolution()
    {
        /// <summary>
        /// Handles confirm pending card internal for card draw manager.
        /// </summary>
        return ConfirmPendingCardInternal(true);
    }

    /// <summary>
    /// Confirms card consume after successful resolution and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmCardConsumeAfterSuccessfulResolution(bool consumePlayAction)
    {
        /// <summary>
        /// Handles confirm pending card internal for card draw manager.
        /// </summary>
        return ConfirmPendingCardInternal(consumePlayAction);
    }

    /// <summary>
    /// Confirms pending card and applies the selected action if it is valid.
    /// </summary>
    public void ConfirmPendingCard(bool consumePlayAction)
    {
        ConfirmPendingCardInternal(consumePlayAction);
    }

    /// <summary>
    /// Confirms pending card internal and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmPendingCardInternal(bool consumePlayAction)
    {
        if (pendingPlayedCard == null) return false;

        TurnManager manager = GetTurnManager();

        if (consumePlayAction && manager != null && !manager.TryConsumePlayCard())
        {
            return false;
        }

        AudioManager.Instance?.PlayCardPlay();

        RemoveCardFromCurrentHand(pendingPlayedCard.sourcePrefab);
        NotifyTutorialCardPlayed(pendingPlayedCard.sourcePrefab);
        Destroy(pendingPlayedCard.gameObject);
        pendingPlayedCard = null;
        RenderCurrentPlayerHand();

        Debug.Log("Pending card confirmed and consumed.");
        return true;
    }

    /// <summary>
    /// Consumes selected card after successful targeting and records that the player has used that action.
    /// </summary>
    public bool ConsumeSelectedCardAfterSuccessfulTargeting()
    {
        /// <summary>
        /// Handles confirm pending card internal for card draw manager.
        /// </summary>
        return ConfirmPendingCardInternal(true);
    }

    /// <summary>
    /// Consumes pending targeting card from player hand and records that the player has used that action.
    /// </summary>
    public bool ConsumePendingTargetingCardFromPlayerHand(int playerId, bool consumePlayAction)
    {
        if (pendingPlayedCard == null || pendingPlayedCard.sourcePrefab == null)
        {
            return false;
        }

        GameObject pendingCardObject = pendingPlayedCard.gameObject;
        GameObject pendingCardPrefab = pendingPlayedCard.sourcePrefab;

        if (!TryConsumeDirectPlayedCard(playerId, pendingCardPrefab, consumePlayAction))
        {
            return false;
        }

        if (pendingCardObject != null)
        {
            Destroy(pendingCardObject);
        }

        pendingPlayedCard = null;
        selectedCard = null;
        return true;
    }

    /// <summary>
    /// Checks whether consume selected card after successful targeting is allowed before enabling that action.
    /// </summary>
    public bool CanConsumeSelectedCardAfterSuccessfulTargeting()
    {
        if (pendingPlayedCard == null)
        {
            return false;
        }

        TurnManager manager = GetTurnManager();
        return manager == null || manager.CanPlayCard();
    }

    /// <summary>
    /// Returns pending card prefab for targeting used by card handling or target selection.
    /// </summary>
    public GameObject GetPendingCardPrefabForTargeting()
    {
        return pendingPlayedCard != null ? pendingPlayedCard.sourcePrefab : null;
    }

    /// <summary>
    /// Checks whether pending card is allowed before enabling that action.
    /// </summary>
    public void CancelPendingCard()
    {
        if (pendingPlayedCard == null) return;

        // Gate targeting lives outside the other targeting managers, so it needs
        // its own explicit exit path before the pending card returns to hand.
        if (GateTargetingManager.Instance != null && GateTargetingManager.Instance.IsAnyTargetingActive())
        {
            GateTargetingManager.Instance.ExitGateTargetMode();
        }

        if (tileTargetingManager != null && tileTargetingManager.IsTargeting())
        {
            tileTargetingManager.ExitWithoutConsumingCard();
        }

        if (playerTargetingManager != null && playerTargetingManager.IsTargeting())
        {
            playerTargetingManager.ExitWithoutConsumingCard();
        }

        if (towerTargetingManager != null && towerTargetingManager.IsTargeting())
        {
            towerTargetingManager.ExitWithoutConsumingCard();
        }

        if (shockTrapTargetingManager != null && shockTrapTargetingManager.IsTargeting())
        {
            shockTrapTargetingManager.ExitWithoutConsumingCard();
        }

        GameObject cardObject = pendingPlayedCard.gameObject;
        cardObject.SetActive(true);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        ResetCardRect(cardRect);

        pendingPlayedCard = null;
        RenderCurrentPlayerHand();

        Debug.Log("Pending card cancelled and returned to hand.");
    }

    /// <summary>
    /// Attempts to consume direct played card and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumeDirectPlayedCard(int playerId, GameObject cardPrefab)
    {
        /// <summary>
        /// Attempts to consume direct played card and reports whether it succeeded.
        /// </summary>
        return TryConsumeDirectPlayedCard(playerId, cardPrefab, true);
    }

    /// <summary>
    /// Attempts to consume direct played card and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumeDirectPlayedCard(int playerId, GameObject cardPrefab, bool consumePlayAction)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null || cardPrefab == null)
        {
            return false;
        }

        TurnManager manager = GetTurnManager();

        if (consumePlayAction && manager != null && !manager.TryConsumePlayCard())
        {
            return false;
        }

        PlayerHand hand = player.GetPlayerHand();

        if (hand == null || !hand.RemoveCard(cardPrefab))
        {
            return false;
        }

        player.SyncCardCountFromHand();

        if (playerManager != null && playerManager.GetCurrentPlayerId() == playerId)
        {
            RenderCurrentPlayerHand();
        }
        else if (playerManager != null)
        {
            playerManager.RefreshPlayerUI(playerId);
        }

        AudioManager.Instance?.PlayCardPlay();
        NotifyTutorialCardPlayed(cardPrefab);
        Debug.Log("Directly consumed played card: " + cardPrefab.name + " for player " + playerId);
        return true;
    }

    // Returns a list of card prefabs to the runtime deck and reshuffles once.
    /// <summary>
    /// Handles return cards to deck for card state, hand state, or targeting.
    /// </summary>
    public void ReturnCardsToDeck(IEnumerable<GameObject> cardPrefabs)
    {
        if (cardPrefabs == null)
        {
            return;
        }

        if (!deckInitialized)
        {
            InitializeDeck();
        }

        bool addedAnyCard = false;

        foreach (GameObject cardPrefab in cardPrefabs)
        {
            if (cardPrefab == null)
            {
                continue;
            }

            runtimeDeck.Add(cardPrefab);
            addedAnyCard = true;
        }

        if (addedAnyCard)
        {
            ShuffleRuntimeDeck();
        }
    }

    // Returns every card in one player's hand to the runtime deck, then clears the hand.
    /// <summary>
    /// Handles return player hand to deck for card state, hand state, or targeting.
    /// </summary>
    public bool ReturnPlayerHandToDeck(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null)
        {
            Debug.LogWarning("ReturnPlayerHandToDeck failed because player " + playerId + " was not found.");
            return false;
        }

        PlayerHand hand = player.GetPlayerHand();

        if (hand == null)
        {
            Debug.LogWarning("ReturnPlayerHandToDeck failed because player " + playerId + " has no PlayerHand.");
            return false;
        }

        List<GameObject> cardsToReturn = hand.GetCards() != null
            ? new List<GameObject>(hand.GetCards().Where(card => card != null))
            : new List<GameObject>();

        ReturnCardsToDeck(cardsToReturn);
        hand.Clear();
        player.ForceSetCardCount(0);

        if (playerManager != null && playerManager.GetCurrentPlayerId() == playerId)
        {
            RenderCurrentPlayerHand();
        }
        else if (playerManager != null)
        {
            playerManager.RefreshPlayerUI(playerId);
        }

        Debug.Log("Returned " + cardsToReturn.Count + " cards from player " + playerId + " back to the deck.");
        return true;
    }

    /// <summary>
    /// Handles force give tutorial card for card state, hand state, or targeting.
    /// </summary>
    public bool ForceGiveTutorialCard(string cardId)
    {
        PlayerResource player = GetVisibleHandPlayerResource();

        if (player == null)
        {
            return false;
        }

        PlayerHand hand = player.GetPlayerHand();
        GameObject cardPrefab = FindCardPrefabById(cardId);

        if (hand == null || cardPrefab == null)
        {
            return false;
        }

        if (hand.GetCards().Contains(cardPrefab))
        {
            RenderCurrentPlayerHand();
            return true;
        }

        if (!hand.CanAddCard())
        {
            List<GameObject> cards = hand.GetCards();
            GameObject cardToReturn = cards != null && cards.Count > 0 ? cards[0] : null;

            if (cardToReturn != null)
            {
                hand.RemoveCard(cardToReturn);
                AddCardToDeck(cardToReturn);
            }
        }

        if (!hand.AddCard(cardPrefab))
        {
            return false;
        }

        player.SyncCardCountFromHand();
        RenderCurrentPlayerHand();
        return true;
    }

    /// <summary>
    /// Handles force single tutorial card for card state, hand state, or targeting.
    /// </summary>
    public bool ForceSingleTutorialCard(string cardId)
    {
        /// <summary>
        /// Handles force tutorial hand for card draw manager.
        /// </summary>
        return ForceTutorialHand(new[] { cardId });
    }

    /// <summary>
    /// Handles force tutorial hand for card state, hand state, or targeting.
    /// </summary>
    public bool ForceTutorialHand(IEnumerable<string> cardIds)
    {
        PlayerResource player = GetVisibleHandPlayerResource();

        if (player == null)
        {
            return false;
        }

        PlayerHand hand = player.GetPlayerHand();
        if (hand == null || cardIds == null)
        {
            return false;
        }

        List<GameObject> cardPrefabs = new List<GameObject>();

        foreach (string cardId in cardIds)
        {
            GameObject cardPrefab = FindCardPrefabById(cardId);

            if (cardPrefab != null)
            {
                cardPrefabs.Add(cardPrefab);
            }
        }

        if (cardPrefabs.Count == 0)
        {
            return false;
        }

        hand.Clear();

        foreach (GameObject cardPrefab in cardPrefabs)
        {
            if (!hand.AddCard(cardPrefab))
            {
                return false;
            }
        }

        player.SyncCardCountFromHand();
        RenderCurrentPlayerHand();
        return true;
    }

    /// <summary>
    /// Resets tutorial card action for a new turn, wave, player, scene, or match state.
    /// </summary>
    public void ResetTutorialCardAction()
    {
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
        }

        CancelPendingCard();
        RenderCurrentPlayerHand();
    }

    /// <summary>
    /// Handles prepare tutorial card demo for card state, hand state, or targeting.
    /// </summary>
    public bool PrepareTutorialCardDemo(string cardId)
    {
        /// <summary>
        /// Handles prepare tutorial card demo for card draw manager.
        /// </summary>
        return PrepareTutorialCardDemo(cardId, null);
    }

    /// <summary>
    /// Handles prepare tutorial card demo for card state, hand state, or targeting.
    /// </summary>
    public bool PrepareTutorialCardDemo(string cardId, IEnumerable<string> handCardIds)
    {
        ResetTutorialCardAction();

        if (handCardIds != null)
        {
            if (!ForceTutorialHand(handCardIds))
            {
                return false;
            }
        }
        else if (!ForceSingleTutorialCard(cardId))
        {
            return false;
        }

        /// <summary>
        /// Attempts to select visible card by ID and reports whether it succeeded.
        /// </summary>
        return TrySelectVisibleCardById(cardId);
    }

    /// <summary>
    /// Restores a card UI transform to its normal anchored position and scale.
    /// </summary>
    void ResetCardRect(RectTransform cardRect)
    {
        if (cardRect == null) return;

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
    }

    /// <summary>
    /// Returns gate action type needed by this gameplay system.
    /// </summary>
    GateActionType GetGateActionType(string cardName)
    {
        cardName = NormalizeCardName(cardName);

        if (cardName == "open gate" || cardName == "opengate")
        {
            return GateActionType.OpenGate;
        }

        if (cardName == "redirectflow" || cardName == "redirect flow")
        {
            return GateActionType.OpenGate;
        }

        if (cardName == "lock gate" || cardName == "lockgate")
        {
            return GateActionType.LockGate;
        }

        return GateActionType.None;
    }

    /// <summary>
    /// Checks the current state to decide whether take over card is true.
    /// </summary>
    private bool IsTakeOverCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "takeover" || cardName == "take over";
    }

    /// <summary>
    /// Checks the current state to decide whether freeze claim card is true.
    /// </summary>
    private bool IsFreezeClaimCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "freezeclaim" || cardName == "freeze claim";
    }

    /// <summary>
    /// Checks the current state to decide whether steal card is true.
    /// </summary>
    private bool IsStealCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "stealcard" || cardName == "steal card";
    }

    /// <summary>
    /// Checks the current state to decide whether trade hands card is true.
    /// </summary>
    private bool IsTradeHandsCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "tradehands" || cardName == "trade hands";
    }

    /// <summary>
    /// Checks the current state to decide whether disrupt card is true.
    /// </summary>
    private bool IsDisruptCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "disrupt";
    }

    /// <summary>
    /// Checks the current state to decide whether power boost card is true.
    /// </summary>
    private bool IsPowerBoostCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "powerboost" || cardName == "power boost";
    }

    /// <summary>
    /// Checks the current state to decide whether shock trap card is true.
    /// </summary>
    private bool IsShockTrapCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "shocktrap" || cardName == "shock trap";
    }

    /// <summary>
    /// Checks the current state to decide whether tile targeting card is true.
    /// </summary>
    private bool IsTileTargetingCard(string cardName)
    {
        /// <summary>
        /// Handles is take over card for card draw manager.
        /// </summary>
        return IsTakeOverCard(cardName) || IsFreezeClaimCard(cardName);
    }

    /// <summary>
    /// Checks the current state to decide whether player targeting card is true.
    /// </summary>
    private bool IsPlayerTargetingCard(string cardName)
    {
        /// <summary>
        /// Handles is steal card for card draw manager.
        /// </summary>
        return IsStealCard(cardName) || IsTradeHandsCard(cardName) || IsDisruptCard(cardName);
    }

    /// <summary>
    /// Checks the current state to decide whether tower targeting card is true.
    /// </summary>
    private bool IsTowerTargetingCard(string cardName)
    {
        /// <summary>
        /// Handles is power boost card for card draw manager.
        /// </summary>
        return IsPowerBoostCard(cardName);
    }

    /// <summary>
    /// Handles normalize card name for card state, hand state, or targeting.
    /// </summary>
    private string NormalizeCardName(string cardName)
    {
        /// <summary>
        /// Handles normalize card ID for card draw manager.
        /// </summary>
        return NormalizeCardId(cardName);
    }

    /// <summary>
    /// Displays a temporary card warning message, then hides it after a delay.
    /// </summary>
    IEnumerator ShowWarning(string message)
    {
        if (warningText != null)
        {
            Transform toastRoot = warningText.transform.parent;

            if (toastRoot != null)
            {
                toastRoot.gameObject.SetActive(true);
            }

            warningText.text = message;
            warningText.fontSize = GetWarningFontSize(message);
        }

        /// <summary>
        /// Handles wait for seconds for card draw manager.
        /// </summary>
        yield return new WaitForSeconds(warningTime);

        if (warningText != null)
        {
            Transform toastRoot = warningText.transform.parent;

            if (toastRoot != null)
            {
                toastRoot.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Shows warning message with the correct current context.
    /// </summary>
    public void ShowWarningMessage(string message)
    {
        StartCoroutine(ShowWarning(message));
    }

    /// <summary>
    /// Returns warning font size used by card handling or target selection.
    /// </summary>
    private float GetWarningFontSize(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return 4f;
        }

        if (message.Length > 33)
        {
            return 2f;
        }

        if (message.Length >= 20)
        {
            return 3f;
        }

        return 4f;
    }

    /// <summary>
    /// Refreshes present number UI from the latest gameplay data.
    /// </summary>
    private void RefreshPresentNumberUI()
    {
        if (playerManager != null)
        {
            PlayerResource currentPlayer = GetVisibleHandPlayerResource();

            if (currentPlayer != null)
            {
                currentPlayer.SyncCardCountFromHand();
            }

            if (TurnSourceResolver.IsAIPrototypeActive() && currentPlayer != null)
            {
                if (presentTheNumberUI != null)
                {
                    presentTheNumberUI.SetNumbers(currentPlayer.money, currentPlayer.GetHandCardCount());
                }

                playerManager.RefreshPlayerUI(currentPlayer.playerId);
            }
            else
            {
                playerManager.RefreshCurrentPlayerUI();
            }

            return;
        }

        if (presentTheNumberUI != null)
        {
            presentTheNumberUI.SetCardCount(GetHandCardCount());
        }
    }

    /// <summary>
    /// Refreshes present number UI next frame from the latest gameplay data.
    /// </summary>
    private IEnumerator RefreshPresentNumberUINextFrame()
    {
        yield return null;
        RefreshPresentNumberUI();
    }

    /// <summary>
    /// Returns turn manager used by card handling or target selection.
    /// </summary>
    private TurnManager GetTurnManager()
    {
        return turnManager != null ? turnManager : TurnManager.Instance;
    }

    /// <summary>
    /// Checks the current state to decide whether disrupted this turn is true.
    /// </summary>
    private bool IsDisruptedThisTurn()
    {
        TurnManager manager = GetTurnManager();
        return manager != null && manager.IsCardActionsBlockedThisTurn();
    }

    // In GameScene_AIPrototype, ITurnSource is AIPrototypeTurnManager; in old GameScene it is TurnManager.
    /// <summary>
    /// Checks whether human use cards now is allowed before enabling that action.
    /// </summary>
    private bool CanHumanUseCardsNow(bool showBlockedTurnToast = false)
    {
        if (OnlineTurnPermissionManager.ShouldBlockLocalGameplayAction(showBlockedTurnToast))
        {
            return false;
        }

        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(GetTurnManager());

        if (!TurnSourceResolver.IsAIPrototypeActive())
        {
            return true;
        }

        return turnSource != null && turnSource.CanHumanAct;
    }

    // AIPrototypeTurnManager owns phase flow in GameScene_AIPrototype, so old GamePhaseManager state is ignored there.
    /// <summary>
    /// Decides whether should use legacy phase check should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldUseLegacyPhaseCheck()
    {
        return !TurnSourceResolver.IsAIPrototypeActive();
    }

    /// <summary>
    /// Responds to current player changed and updates the affected gameplay or UI systems.
    /// </summary>
    private void HandleCurrentPlayerChanged(int playerId)
    {
        if (pendingPlayedCard != null)
        {
            CancelPendingCard();
        }

        RenderCurrentPlayerHand();
    }

    /// <summary>
    /// Rebuilds the visible card slots for whichever player currently has the turn.
    /// </summary>
    public void RenderCurrentPlayerHand()
    {
        selectedCard = null;
        ClearVisibleHand();

        PlayerResource currentPlayer = GetVisibleHandPlayerResource();

        if (currentPlayer == null)
        {
            RefreshPresentNumberUI();
            return;
        }

        PlayerHand currentHand = currentPlayer.GetPlayerHand();

        if (currentHand == null)
        {
            RefreshPresentNumberUI();
            return;
        }

        List<GameObject> handCards = currentHand.GetCards();
        int visibleCount = Mathf.Min(handCards.Count, cardSlots != null ? cardSlots.Length : 0);

        for (int i = 0; i < visibleCount; i++)
        {
            CreateCardView(handCards[i], cardSlots[i]);
        }

        currentPlayer.SetCardCount(currentHand.GetCardCount());
        RefreshPresentNumberUI();
    }

    // In AIPrototype mode, the visible hand belongs to the local human player, not the AI whose turn is running.
    /// <summary>
    /// Returns visible hand player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetVisibleHandPlayerResource()
    {
        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineGameSceneManager.Instance != null &&
            PhotonOnlineGameSceneManager.Instance.LocalPlayerId >= 0 &&
            playerManager != null)
        {
            return playerManager.GetPlayerResource(PhotonOnlineGameSceneManager.Instance.LocalPlayerId);
        }

        if (!TurnSourceResolver.IsAIPrototypeActive())
        {
            /// <summary>
            /// Returns current player resource needed by this gameplay system.
            /// </summary>
            return GetCurrentPlayerResource();
        }

        if (playerManager == null || playerManager.players == null)
        {
            /// <summary>
            /// Returns current player resource needed by this gameplay system.
            /// </summary>
            return GetCurrentPlayerResource();
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null &&
                player.playerType == PlayerType.Human &&
                !player.isEliminated)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// Clears visible hand and removes its temporary gameplay or visual effect.
    /// </summary>
    private void ClearVisibleHand()
    {
        if (cardSlots == null)
        {
            return;
        }

        foreach (Transform slot in cardSlots)
        {
            if (slot == null)
            {
                continue;
            }

            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                Transform child = slot.GetChild(i);

                if (child != null && child.name == "CardView")
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Creates card view and configures it for the current scene or interaction.
    /// </summary>
    private GameObject CreateCardView(GameObject cardPrefab, Transform slot)
    {
        if (cardPrefab == null || slot == null)
        {
            return null;
        }

        GameObject newCard = Instantiate(cardPrefab, slot);
        newCard.name = "CardView";

        ResetCardRect(newCard.GetComponent<RectTransform>());

        CardInstanceSelectable selectable = newCard.GetComponent<CardInstanceSelectable>();

        if (selectable == null)
        {
            selectable = newCard.AddComponent<CardInstanceSelectable>();
        }

        selectable.Init(this, cardPrefab, selectedMoveUpDistance);
        return newCard;
    }

    /// <summary>
    /// Returns current player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
    }

    /// <summary>
    /// Returns blocked turn message used by card handling or target selection.
    /// </summary>
    private string GetBlockedTurnMessage()
    {
        return PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext()
            ? "Wait for your turn."
            : "Wait for your turn.";
    }

    /// <summary>
    /// Returns current player ID used by card handling or target selection.
    /// </summary>
    private int GetCurrentPlayerId()
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerId();
        }

        TurnManager manager = GetTurnManager();
        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(manager);
        return turnSource != null ? turnSource.CurrentPlayerId : 0;
    }

    /// <summary>
    /// Returns current player hand used by card handling or target selection.
    /// </summary>
    private PlayerHand GetCurrentPlayerHand()
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerHand();
        }

        PlayerResource currentPlayer = GetCurrentPlayerResource();
        return currentPlayer != null ? currentPlayer.GetPlayerHand() : null;
    }

    /// <summary>
    /// Checks the current state to decide whether current hand full is true.
    /// </summary>
    private bool IsCurrentHandFull()
    {
        PlayerHand currentHand = GetCurrentPlayerHand();

        if (currentHand != null)
        {
            return !currentHand.CanAddCard();
        }

        int maxCards = cardSlots != null ? cardSlots.Length : 0;
        return maxCards > 0 && GetHandCardCount() >= maxCards;
    }

    /// <summary>
    /// Removes card from current hand from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
    private void RemoveCardFromCurrentHand(GameObject cardPrefab)
    {
        PlayerHand currentHand = GetCurrentPlayerHand();

        if (currentHand != null)
        {
            currentHand.RemoveCard(cardPrefab);
            SyncCurrentPlayerCardCount();
        }
    }

    /// <summary>
    /// Handles sync current player card count for card state, hand state, or targeting.
    /// </summary>
    private void SyncCurrentPlayerCardCount()
    {
        PlayerResource currentPlayer = GetCurrentPlayerResource();

        if (currentPlayer != null)
        {
            currentPlayer.SyncCardCountFromHand();
        }
    }

    /// <summary>
    /// Checks whether drawable card is present before the code depends on it.
    /// </summary>
    private bool HasDrawableCard()
    {
        return GetRemainingDeckCount() > 0;
    }

    /// <summary>
    /// Handles draw one card from deck for card state, hand state, or targeting.
    /// </summary>
    private GameObject DrawOneCardFromDeck()
    {
        if (!deckInitialized)
        {
            InitializeDeck();
        }

        if (runtimeDeck.Count <= 0 && rebuildDeckWhenEmpty)
        {
            InitializeDeck();
        }

        if (runtimeDeck.Count <= 0)
        {
            return null;
        }

        GameObject cardPrefab = runtimeDeck[0];
        runtimeDeck.RemoveAt(0);
        return cardPrefab;
    }

    /// <summary>
    /// Adds card to deck to the relevant list, UI, hand, deck, or gameplay state.
    /// </summary>
    private void AddCardToDeck(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            return;
        }

        runtimeDeck.Add(cardPrefab);
        ShuffleRuntimeDeck();
    }

    /// <summary>
    /// Returns remaining deck count used by card handling or target selection.
    /// </summary>
    private int GetRemainingDeckCount()
    {
        if (!deckInitialized)
        {
            InitializeDeck();
        }

        if (runtimeDeck.Count <= 0 && rebuildDeckWhenEmpty)
        {
            InitializeDeck();
        }

        return runtimeDeck.Count;
    }

    /// <summary>
    /// Notifies connected systems that tutorial card played occurred.
    /// </summary>
    private void NotifyTutorialCardPlayed(GameObject cardPrefab)
    {
        TutorialActionType actionType = cardPrefab != null
            ? GetTutorialActionTypeForCardName(cardPrefab.name)
            : TutorialActionType.PlayCard;

        TutorialManager.Instance?.NotifyCardPlayed(actionType, cardPrefab);
    }

    /// <summary>
    /// Returns tutorial action type for card name used by card handling or target selection.
    /// </summary>
    private TutorialActionType GetTutorialActionTypeForCardName(string cardName)
    {
        if (IsTakeOverCard(cardName)) return TutorialActionType.TakeOver;
        if (IsFreezeClaimCard(cardName)) return TutorialActionType.FreezeClaim;
        if (IsStealCard(cardName)) return TutorialActionType.StealCard;
        if (IsTradeHandsCard(cardName)) return TutorialActionType.TradeHands;
        if (IsDisruptCard(cardName)) return TutorialActionType.Disrupt;
        if (IsPowerBoostCard(cardName)) return TutorialActionType.PowerBoost;
        if (IsShockTrapCard(cardName)) return TutorialActionType.PlaceShockTrap;

        GateActionType gateActionType = GetGateActionType(cardName);

        if (gateActionType == GateActionType.OpenGate) return TutorialActionType.OpenGate;
        if (gateActionType == GateActionType.LockGate) return TutorialActionType.LockGate;

        return TutorialActionType.PlayCard;
    }

    /// <summary>
    /// Searches scene objects or cached lists to find card prefab by ID.
    /// </summary>
    private GameObject FindCardPrefabById(string cardId)
    {
        string normalizedId = NormalizeCardName(cardId);

        if (string.IsNullOrEmpty(normalizedId))
        {
            return null;
        }

        if (deckEntries != null)
        {
            foreach (CardDeckEntry entry in deckEntries)
            {
                if (entry != null &&
                    entry.cardPrefab != null &&
                    NormalizeCardName(entry.cardPrefab.name) == normalizedId)
                {
                    return entry.cardPrefab;
                }
            }
        }

        if (cardTypePrefabs != null)
        {
            foreach (GameObject prefab in cardTypePrefabs)
            {
                if (prefab != null && NormalizeCardName(prefab.name) == normalizedId)
                {
                    return prefab;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to select visible card by ID and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TrySelectVisibleCardById(string cardId)
    {
        string normalizedId = NormalizeCardName(cardId);

        if (string.IsNullOrEmpty(normalizedId) || cardSlots == null)
        {
            return false;
        }

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] == null)
            {
                continue;
            }

            CardInstanceSelectable selectable = cardSlots[i].GetComponentInChildren<CardInstanceSelectable>();

            if (selectable == null || selectable.sourcePrefab == null)
            {
                continue;
            }

            if (NormalizeCardName(selectable.sourcePrefab.name) == normalizedId)
            {
                if (selectedCard != null)
                {
                    selectedCard.SetSelected(false);
                }

                selectedCard = selectable;
                selectedCard.SetSelected(true);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns player resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetPlayerResource(int playerId)
    {
        if (playerManager == null || playerManager.players == null)
        {
            return null;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null && player.playerId == playerId)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// Handles normalize card ID for card state, hand state, or targeting.
    /// </summary>
    public static string NormalizeCardId(string cardName)
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            return string.Empty;
        }

        return cardName.Replace("(Clone)", "").Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks the current state to decide whether player targeting card ID is true.
    /// </summary>
    public static bool IsPlayerTargetingCardId(string cardId)
    {
        return cardId == "stealcard" ||
            cardId == "steal card" ||
            cardId == "tradehands" ||
            cardId == "trade hands" ||
            cardId == "disrupt";
    }

    /// <summary>
    /// Handles requires board target ID for card state, hand state, or targeting.
    /// </summary>
    public static bool RequiresBoardTargetId(string cardId)
    {
        return cardId == "opengate" ||
            cardId == "open gate" ||
            cardId == "redirectflow" ||
            cardId == "redirect flow" ||
            cardId == "lockgate" ||
            cardId == "lock gate" ||
            cardId == "takeover" ||
            cardId == "take over" ||
            cardId == "freezeclaim" ||
            cardId == "freeze claim" ||
            cardId == "powerboost" ||
            cardId == "power boost" ||
            cardId == "shocktrap" ||
            cardId == "shock trap";
    }

    /// <summary>
    /// Handles requires target selection for card state, hand state, or targeting.
    /// </summary>
    public bool RequiresTargetSelection(string cardId)
    {
        string normalizedCardId = NormalizeCardId(cardId);
        /// <summary>
        /// Handles is player targeting card ID for card draw manager.
        /// </summary>
        return IsPlayerTargetingCardId(normalizedCardId) || RequiresBoardTargetId(normalizedCardId);
    }

    /// <summary>
    /// Builds configured deck card IDs from configured scene objects and runtime state.
    /// </summary>
    public List<string> BuildConfiguredDeckCardIds()
    {
        List<string> deckCardIds = new List<string>();

        if (HasConfiguredDeckEntries())
        {
            foreach (CardDeckEntry entry in deckEntries)
            {
                if (entry == null || entry.cardPrefab == null)
                {
                    continue;
                }

                for (int i = 0; i < Mathf.Max(0, entry.copies); i++)
                {
                    deckCardIds.Add(NormalizeCardId(entry.cardPrefab.name));
                }
            }

            return deckCardIds;
        }

        if (cardTypePrefabs != null)
        {
            foreach (GameObject prefab in cardTypePrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                for (int i = 0; i < copiesPerCardType; i++)
                {
                    deckCardIds.Add(NormalizeCardId(prefab.name));
                }
            }
        }

        return deckCardIds;
    }

    /// <summary>
    /// Returns max hand size for player used by card handling or target selection.
    /// </summary>
    public int GetMaxHandSizeForPlayer(int playerId)
    {
        PlayerHand hand = playerManager != null ? playerManager.GetPlayerHand(playerId) : null;
        return hand != null ? hand.maxHandSize : 5;
    }

    /// <summary>
    /// Clears all player hands for online bootstrap and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearAllPlayerHandsForOnlineBootstrap()
    {
        if (playerManager == null || playerManager.players == null)
        {
            return;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            PlayerHand hand = player.GetPlayerHand();

            if (hand != null)
            {
                hand.Clear();
            }

            player.ForceSetCardCount(0);
        }

        selectedCard = null;
        pendingPlayedCard = null;
        RenderCurrentPlayerHand();
    }

    /// <summary>
    /// Applies online private hand state to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOnlinePrivateHandState(int ownerPlayerId, IEnumerable<string> cardIds, int handCount)
    {
        PlayerResource owner = GetPlayerResource(ownerPlayerId);

        if (owner == null)
        {
            return;
        }

        PlayerHand hand = owner.GetPlayerHand();

        if (hand == null)
        {
            return;
        }

        List<GameObject> prefabs = new List<GameObject>();

        if (cardIds != null)
        {
            foreach (string cardId in cardIds)
            {
                GameObject prefab = FindCardPrefabById(cardId);

                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }
        }

        hand.CopyFrom(prefabs);
        owner.SetCardCount(handCount);
        pendingPlayedCard = null;
        selectedCard = null;

        if (PhotonOnlineGameSceneManager.Instance != null &&
            PhotonOnlineGameSceneManager.Instance.IsLocalOnlinePlayer(ownerPlayerId))
        {
            RenderCurrentPlayerHand();
        }
        else if (playerManager != null)
        {
            playerManager.RefreshPlayerUI(ownerPlayerId);
        }
    }

    /// <summary>
    /// Decides whether should use authoritative online card sync should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldUseAuthoritativeOnlineCardSync()
    {
        return PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            PhotonOnlineCardSyncManager.Instance.IsInitializedForOnlineMatch;
    }
}
