/*
 * File: CardDrawManager.cs
 *
 * Purpose:
 * This script controls the card system used by the player during gameplay.
 * It builds a runtime deck from Inspector deck entries, shuffles and draws cards
 * into UI slots, lets the player select/discard/play a card, and coordinates
 * cards that require gate targeting.
 *
 * Main gameplay flow:
 * 1. InitializeDeck() builds the configured runtime deck.
 * 2. DrawCard() adds one prefab reference to the current player's PlayerHand.
 * 3. CardInstanceSelectable reports clicks back to this manager through SelectCard().
 * 4. PlaySelectedCard() animates the selected card toward the draw pile area.
 * 5. If the card name maps to a GateActionType, this manager opens gate targeting
 *    mode and keeps the card hidden as pendingPlayedCard.
 * 6. GateTargetingManager later calls ConfirmPendingCard() or CancelPendingCard()
 *    depending on whether the gate action is confirmed or cancelled.
 *
 * Inspector setup:
 * - deckEntries should contain the 28-card deck composition.
 * - cardSlots should point to the UI transforms where cards can be placed.
 * - drawPileVisual is optional and is used as the visual animation target.
 * - warningText is optional and displays short player feedback messages.
 *
 * Important dependency notes:
 * - CardInstanceSelectable must exist on card prefabs, or it is added at runtime.
 * - GateTargetingManager is used only for cards that affect gates.
 * - CursorToolManager is used to switch the cursor into hammer/targeting mode.
 */
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
            return;
        }

        if (isBusy) return;

        if (IsCurrentHandFull())
        {
            StartCoroutine(ShowWarning("Hand limit reached."));
            return;
        }

        if (!HasDrawableCard())
        {
            StartCoroutine(ShowWarning("Deck is empty."));
            return;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumeDraw())
        {
            return;
        }

        StartCoroutine(DrawCardRoutine());
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
        return !isBusy && pendingPlayedCard == null;
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
        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
            return;
        }

        if (isBusy) return;
        if (pendingPlayedCard != null) return;

        if (selectedCard == null)
        {
            StartCoroutine(ShowWarning("No card selected!"));
            return;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumeDiscard())
        {
            return;
        }

        GameObject returnedPrefab = selectedCard.sourcePrefab;

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
    }

    public void PlaySelectedCard()
    {
        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            StartCoroutine(ShowWarning("You cannot use cards during enemy wave."));
            return;
        }

        if (isBusy) return;
        if (pendingPlayedCard != null) return;

        if (selectedCard == null)
        {
            StartCoroutine(ShowWarning("No card selected!"));
            return;
        }

        GateActionType gateActionType = GetGateActionType(selectedCard.sourcePrefab.name);

        if (gateActionType != GateActionType.None &&
            (GateTargetingManager.Instance == null ||
             !GateTargetingManager.Instance.HasValidGateTargets(gateActionType)))
        {
            StartCoroutine(ShowWarning("No valid gates."));
            return;
        }

        if (IsTakeOverCard(selectedCard.sourcePrefab.name) && tileTargetingManager == null)
        {
            StartCoroutine(ShowWarning("Tile targeting manager is missing."));
            return;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.CanPlayCard())
        {
            manager.TryConsumePlayCard();
            return;
        }

        StartCoroutine(PlayCardRoutine(selectedCard));
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

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumePlayCard())
        {
            ResetCardRect(cardRect);
            card.SetSelected(true);
            isBusy = false;
            yield break;
        }

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

    public void ConfirmPendingCard(bool consumePlayAction)
    {
        if (pendingPlayedCard == null) return;

        TurnManager manager = GetTurnManager();

        if (consumePlayAction && manager != null && !manager.TryConsumePlayCard())
        {
            return;
        }

        RemoveCardFromCurrentHand(pendingPlayedCard.sourcePrefab);
        Destroy(pendingPlayedCard.gameObject);
        pendingPlayedCard = null;
        RenderCurrentPlayerHand();

        Debug.Log("Pending card confirmed and consumed.");
    }

    public bool ConsumeSelectedCardAfterSuccessfulTargeting()
    {
        if (pendingPlayedCard == null)
        {
            return false;
        }

        TurnManager manager = GetTurnManager();

        if (manager != null && !manager.TryConsumePlayCard())
        {
            return false;
        }

        RemoveCardFromCurrentHand(pendingPlayedCard.sourcePrefab);
        Destroy(pendingPlayedCard.gameObject);
        pendingPlayedCard = null;
        RenderCurrentPlayerHand();

        Debug.Log("Pending card confirmed and consumed.");
        return true;
    }

    public void CancelPendingCard()
    {
        if (pendingPlayedCard == null) return;

        if (tileTargetingManager != null && tileTargetingManager.IsTargeting())
        {
            tileTargetingManager.ExitWithoutConsumingCard();
        }

        GameObject cardObject = pendingPlayedCard.gameObject;
        cardObject.SetActive(true);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        ResetCardRect(cardRect);

        pendingPlayedCard = null;
        RenderCurrentPlayerHand();

        Debug.Log("Pending card cancelled and returned to hand.");
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
        cardName = cardName.Trim();

        if (cardName == "Open Gate")
        {
            return GateActionType.OpenGate;
        }

        if (cardName == "RedirectFlow")
        {
            return GateActionType.OpenGate;
        }

        if (cardName == "Lock Gate")
        {
            return GateActionType.LockGate;
        }

        if (cardName == "LockGate")
        {
            return GateActionType.LockGate;
        }

        return GateActionType.None;
    }

    private bool IsTakeOverCard(string cardName)
    {
        cardName = cardName.Trim();
        return cardName == "TakeOver" || cardName == "Take Over";
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
        int handCount = GetHandCardCount();

        if (playerManager != null)
        {
            PlayerResource currentPlayer = playerManager.GetCurrentPlayerResource();

            if (currentPlayer != null)
            {
                currentPlayer.SetCardCount(handCount);
            }

            playerManager.RefreshCurrentPlayerUI();
            return;
        }

        if (presentTheNumberUI != null)
        {
            presentTheNumberUI.SetCardCount(handCount);
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

        PlayerResource currentPlayer = GetCurrentPlayerResource();

        if (currentPlayer == null)
        {
            RefreshPresentNumberUI();
            return;
        }

        PlayerHand currentHand = GetCurrentPlayerHand();

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

    private int GetCurrentPlayerId()
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerId();
        }

        TurnManager manager = GetTurnManager();
        return manager != null ? manager.currentPlayerId : 0;
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
}
