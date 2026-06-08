/*
 * File: CardDrawManager.cs
 *
 * Purpose:
 * Implements CardDrawManager for the card layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CardDrawManager within the card system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CardDrawManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
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

    private void OnEnable()
    {
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;
        }
    }

    private void OnDisable()
    {
        if (playerManager != null)
        {
            playerManager.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
        }
    }

    void Start()
    {
        if (!deckInitialized)
        {
            InitializeDeck();
        }

        RenderCurrentPlayerHand();
    }

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

    public void DrawCard()
    {
        TryDrawCardForCurrentPlayer();
    }

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
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
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

    IEnumerator DrawCardRoutine()
    {
        isBusy = true;

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(true);
        }

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

    public void DealInitialHands()
    {
        DealInitialHands(initialCardsPerPlayer);
    }

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

    public bool TryDrawForCurrentPlayer(bool countsAsTurnDraw)
    {
        PlayerResource currentPlayer = GetCurrentPlayerResource();
        return currentPlayer != null && TryDrawForPlayer(currentPlayer.playerId, countsAsTurnDraw);
    }

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

    public bool CanSelectCards()
    {
        return !isBusy && pendingPlayedCard == null && CanHumanUseCardsNow();
    }

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
    }

    public void DiscardSelectedCard()
    {
        TryDiscardSelectedCard();
    }

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
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
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

    public void PlaySelectedCard()
    {
        TryBeginPlaySelectedCard();
    }

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
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
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

            pendingPlayedCard = card;
            selectedCard = null;

            // Hide the played card without destroying it yet.
            // Confirm will consume it; Cancel will return it to the hand slot.
            card.gameObject.SetActive(false);

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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

            pendingPlayedCard = card;
            selectedCard = null;
            card.gameObject.SetActive(false);
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

    public void ConfirmPendingCard()
    {
        ConfirmPendingCard(true);
    }

    public bool ConfirmCardConsumeAfterSuccessfulResolution()
    {
        return ConfirmPendingCardInternal(true);
    }

    public bool ConfirmCardConsumeAfterSuccessfulResolution(bool consumePlayAction)
    {
        return ConfirmPendingCardInternal(consumePlayAction);
    }

    public void ConfirmPendingCard(bool consumePlayAction)
    {
        ConfirmPendingCardInternal(consumePlayAction);
    }

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

    public bool ConsumeSelectedCardAfterSuccessfulTargeting()
    {
        return ConfirmPendingCardInternal(true);
    }

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

    public bool CanConsumeSelectedCardAfterSuccessfulTargeting()
    {
        if (pendingPlayedCard == null)
        {
            return false;
        }

        TurnManager manager = GetTurnManager();
        return manager == null || manager.CanPlayCard();
    }

    public GameObject GetPendingCardPrefabForTargeting()
    {
        return pendingPlayedCard != null ? pendingPlayedCard.sourcePrefab : null;
    }

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

    public bool TryConsumeDirectPlayedCard(int playerId, GameObject cardPrefab)
    {
        return TryConsumeDirectPlayedCard(playerId, cardPrefab, true);
    }

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
        player.SyncCardCountFromHand();

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

        if (!hand.AddCard(cardPrefab))
        {
            return false;
        }

        player.SyncCardCountFromHand();
        RenderCurrentPlayerHand();
        return true;
    }

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

    public bool PrepareTutorialCardDemo(string cardId)
    {
        ResetTutorialCardAction();

        if (!ForceGiveTutorialCard(cardId))
        {
            return false;
        }

        return TrySelectVisibleCardById(cardId);
    }

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

    private bool IsTakeOverCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "takeover" || cardName == "take over";
    }

    private bool IsFreezeClaimCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "freezeclaim" || cardName == "freeze claim";
    }

    private bool IsStealCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "stealcard" || cardName == "steal card";
    }

    private bool IsTradeHandsCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "tradehands" || cardName == "trade hands";
    }

    private bool IsDisruptCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "disrupt";
    }

    private bool IsPowerBoostCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "powerboost" || cardName == "power boost";
    }

    private bool IsShockTrapCard(string cardName)
    {
        cardName = NormalizeCardName(cardName);
        return cardName == "shocktrap" || cardName == "shock trap";
    }

    private bool IsTileTargetingCard(string cardName)
    {
        return IsTakeOverCard(cardName) || IsFreezeClaimCard(cardName);
    }

    private bool IsPlayerTargetingCard(string cardName)
    {
        return IsStealCard(cardName) || IsTradeHandsCard(cardName) || IsDisruptCard(cardName);
    }

    private bool IsTowerTargetingCard(string cardName)
    {
        return IsPowerBoostCard(cardName);
    }

    private string NormalizeCardName(string cardName)
    {
        return NormalizeCardId(cardName);
    }

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

    public void ShowWarningMessage(string message)
    {
        StartCoroutine(ShowWarning(message));
    }

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

    private IEnumerator RefreshPresentNumberUINextFrame()
    {
        yield return null;
        RefreshPresentNumberUI();
    }

    private TurnManager GetTurnManager()
    {
        return turnManager != null ? turnManager : TurnManager.Instance;
    }

    private bool IsDisruptedThisTurn()
    {
        TurnManager manager = GetTurnManager();
        return manager != null && manager.IsCardActionsBlockedThisTurn();
    }

    // In GameScene_AIPrototype, ITurnSource is AIPrototypeTurnManager; in old GameScene it is TurnManager.
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
    private bool ShouldUseLegacyPhaseCheck()
    {
        return !TurnSourceResolver.IsAIPrototypeActive();
    }

    private void HandleCurrentPlayerChanged(int playerId)
    {
        if (pendingPlayedCard != null)
        {
            CancelPendingCard();
        }

        RenderCurrentPlayerHand();
    }

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
            return GetCurrentPlayerResource();
        }

        if (playerManager == null || playerManager.players == null)
        {
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

    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
    }

    private string GetBlockedTurnMessage()
    {
        return PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext()
            ? "Wait for your turn."
            : "Wait for your turn.";
    }

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

    private PlayerHand GetCurrentPlayerHand()
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerHand();
        }

        PlayerResource currentPlayer = GetCurrentPlayerResource();
        return currentPlayer != null ? currentPlayer.GetPlayerHand() : null;
    }

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

    private void RemoveCardFromCurrentHand(GameObject cardPrefab)
    {
        PlayerHand currentHand = GetCurrentPlayerHand();

        if (currentHand != null)
        {
            currentHand.RemoveCard(cardPrefab);
            SyncCurrentPlayerCardCount();
        }
    }

    private void SyncCurrentPlayerCardCount()
    {
        PlayerResource currentPlayer = GetCurrentPlayerResource();

        if (currentPlayer != null)
        {
            currentPlayer.SyncCardCountFromHand();
        }
    }

    private bool HasDrawableCard()
    {
        return GetRemainingDeckCount() > 0;
    }

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

    private void AddCardToDeck(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            return;
        }

        runtimeDeck.Add(cardPrefab);
        ShuffleRuntimeDeck();
    }

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

    private void NotifyTutorialCardPlayed(GameObject cardPrefab)
    {
        TutorialActionType actionType = cardPrefab != null
            ? GetTutorialActionTypeForCardName(cardPrefab.name)
            : TutorialActionType.PlayCard;

        TutorialManager.Instance?.NotifyCardPlayed(actionType, cardPrefab);
    }

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

    public static string NormalizeCardId(string cardName)
    {
        if (string.IsNullOrWhiteSpace(cardName))
        {
            return string.Empty;
        }

        return cardName.Replace("(Clone)", "").Trim().ToLowerInvariant();
    }

    public static bool IsPlayerTargetingCardId(string cardId)
    {
        return cardId == "stealcard" ||
            cardId == "steal card" ||
            cardId == "tradehands" ||
            cardId == "trade hands" ||
            cardId == "disrupt";
    }

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

    public bool RequiresTargetSelection(string cardId)
    {
        string normalizedCardId = NormalizeCardId(cardId);
        return IsPlayerTargetingCardId(normalizedCardId) || RequiresBoardTargetId(normalizedCardId);
    }

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

    public int GetMaxHandSizeForPlayer(int playerId)
    {
        PlayerHand hand = playerManager != null ? playerManager.GetPlayerHand(playerId) : null;
        return hand != null ? hand.maxHandSize : 5;
    }

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

            player.SetCardCount(0);
        }

        selectedCard = null;
        pendingPlayedCard = null;
        RenderCurrentPlayerHand();
    }

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

    private bool ShouldUseAuthoritativeOnlineCardSync()
    {
        return PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            PhotonOnlineCardSyncManager.Instance.IsInitializedForOnlineMatch;
    }
}
