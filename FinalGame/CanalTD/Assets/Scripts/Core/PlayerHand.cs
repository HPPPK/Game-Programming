/*
 * File: PlayerHand.cs
 *
 * Purpose:
 * Stores the real card hand for one player. CardDrawManager renders this data
 * into the shared visible card slots, but the UI card objects are not the source
 * of truth.
 *
 * Notes:
 * The hand stores card prefab references, enforces max hand size, and supports
 * random removal for cards such as Steal Card.
 */
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Player")]
    public int playerId = 0;

    [Header("Cards")]
    public int maxHandSize = 5;
    public List<GameObject> handCardPrefabs = new List<GameObject>();

    public bool CanAddCard()
    {
        return GetCardCount() < maxHandSize;
    }

    public bool AddCard(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            return false;
        }

        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
        }

        if (!CanAddCard())
        {
            return false;
        }

        handCardPrefabs.Add(cardPrefab);
        return true;
    }

    public bool RemoveCard(GameObject cardPrefab)
    {
        if (handCardPrefabs == null || cardPrefab == null)
        {
            return false;
        }

        return handCardPrefabs.Remove(cardPrefab);
    }

    public GameObject RemoveRandomCard()
    {
        if (handCardPrefabs == null || handCardPrefabs.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, handCardPrefabs.Count);
        GameObject cardPrefab = handCardPrefabs[randomIndex];
        handCardPrefabs.RemoveAt(randomIndex);
        return cardPrefab;
    }

    public int GetCardCount()
    {
        return handCardPrefabs != null ? handCardPrefabs.Count : 0;
    }

    public void Clear()
    {
        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
            return;
        }

        handCardPrefabs.Clear();
    }

    public List<GameObject> GetCards()
    {
        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
        }

        return handCardPrefabs;
    }

    public void CopyFrom(List<GameObject> cardPrefabs)
    {
        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
        }

        handCardPrefabs.Clear();

        if (cardPrefabs == null)
        {
            return;
        }

        foreach (GameObject cardPrefab in cardPrefabs)
        {
            if (cardPrefab != null)
            {
                handCardPrefabs.Add(cardPrefab);
            }
        }
    }
}
