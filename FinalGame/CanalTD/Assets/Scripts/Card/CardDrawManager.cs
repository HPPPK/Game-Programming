/*
 * File: CardDrawManager.cs
 *
 * Purpose:
 * This script controls the card system used by the player during gameplay.
 * It builds a deck from the card prefab list, shuffles the deck, draws cards
 * into UI slots, lets the player select/discard/play a card, and coordinates
 * cards that require gate targeting.
 *
 * Main gameplay flow:
 * 1. Start() creates a deck by adding several copies of each card prefab.
 * 2. DrawCard() finds an empty hand slot and instantiates one card prefab there.
 * 3. CardInstanceSelectable reports clicks back to this manager through SelectCard().
 * 4. PlaySelectedCard() animates the selected card toward the draw pile area.
 * 5. If the card name maps to a GateActionType, this manager opens gate targeting
 *    mode and keeps the card hidden as pendingPlayedCard.
 * 6. GateTargetingManager later calls ConfirmPendingCard() or CancelPendingCard()
 *    depending on whether the gate action is confirmed or cancelled.
 *
 * Inspector setup:
 * - cardTypePrefabs should contain the available card UI prefabs.
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

public class CardDrawManager : MonoBehaviour
{
    [Header("9 Card Type Prefabs")]
    public GameObject[] cardTypePrefabs;

    [Header("Deck Settings")]
    public int copiesPerCardType = 4;

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

    private List<GameObject> deck = new List<GameObject>();
    private bool isBusy = false;
    private CardInstanceSelectable selectedCard;
    private CardInstanceSelectable pendingPlayedCard;

    void Start()
    {
        BuildDeck();
        ShuffleDeck();
        Debug.Log("Deck ready. Total cards = " + deck.Count);
    }

    void BuildDeck()
    {
        deck.Clear();

        foreach (GameObject prefab in cardTypePrefabs)
        {
            if (prefab == null) continue;

            for (int i = 0; i < copiesPerCardType; i++)
            {
                deck.Add(prefab);
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);

            GameObject temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public void DrawCard()
    {
        if (isBusy) return;

        if (TurnManager.Instance != null && !TurnManager.Instance.CanDrawCard())
        {
            StartCoroutine(ShowWarning("Cannot draw this turn."));
            return;
        }

        int emptySlotIndex = FindEmptySlot();

        if (emptySlotIndex == -1)
        {
            StartCoroutine(ShowWarning("Hand is full!"));
            return;
        }

        if (deck.Count <= 0)
        {
            StartCoroutine(ShowWarning("Deck is empty!"));
            return;
        }

        StartCoroutine(DrawCardRoutine(emptySlotIndex));
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

    IEnumerator DrawCardRoutine(int slotIndex)
    {
        isBusy = true;

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(true);
        }

        yield return new WaitForSeconds(animationTime);

        GameObject cardPrefab = deck[0];
        deck.RemoveAt(0);

        GameObject newCard = Instantiate(cardPrefab, cardSlots[slotIndex]);
        newCard.name = "CardView";

        ResetCardRect(newCard.GetComponent<RectTransform>());

        CardInstanceSelectable selectable = newCard.GetComponent<CardInstanceSelectable>();

        if (selectable == null)
        {
            selectable = newCard.AddComponent<CardInstanceSelectable>();
        }

        selectable.Init(this, cardPrefab, selectedMoveUpDistance);

        if (drawPileVisual != null)
        {
            drawPileVisual.SetActive(false);
        }

        Debug.Log("Drew card: " + cardPrefab.name + ". Cards left in deck = " + deck.Count);

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.SpendAP(1);
            TurnManager.Instance.OnCardDrawn();
        }

        isBusy = false;
    }

    public bool CanSelectCards()
    {
        return !isBusy && pendingPlayedCard == null;
    }

    public int GetHandCardCount()
    {
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

            if (slot.Find("CardView") != null)
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
        if (isBusy) return;
        if (pendingPlayedCard != null) return;

        if (selectedCard == null)
        {
            StartCoroutine(ShowWarning("No card selected!"));
            return;
        }

        GameObject returnedPrefab = selectedCard.sourcePrefab;

        if (returnedPrefab != null)
        {
            deck.Add(returnedPrefab);
            ShuffleDeck();
        }

        Debug.Log("Discarded card and returned to deck: " + returnedPrefab.name + ". Cards in deck = " + deck.Count);

        GameObject cardObject = selectedCard.gameObject;
        selectedCard = null;

        Destroy(cardObject);
    }

    public void PlaySelectedCard()
    {
        if (isBusy) return;
        if (pendingPlayedCard != null) return;

        if (TurnManager.Instance != null && !TurnManager.Instance.CanPlayCard())
        {
            StartCoroutine(ShowWarning("Cannot play a card this turn."));
            return;
        }

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

        selectedCard = null;
        Destroy(card.gameObject);

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnCardConfirmed();
        }

        isBusy = false;
    }

    public void ConfirmPendingCard()
    {
        if (pendingPlayedCard == null) return;

        Destroy(pendingPlayedCard.gameObject);
        pendingPlayedCard = null;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnCardConfirmed();
        }

        Debug.Log("Pending card confirmed and consumed.");
    }

    public void CancelPendingCard()
    {
        if (pendingPlayedCard == null) return;

        GameObject cardObject = pendingPlayedCard.gameObject;
        cardObject.SetActive(true);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        ResetCardRect(cardRect);

        pendingPlayedCard = null;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnCardCanceled();
        }

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
}
