using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Player")]
    public int playerId = 0;

    [Header("Cards")]
    public List<GameObject> handCardPrefabs = new List<GameObject>();

    public void AddCard(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            return;
        }

        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
        }

        handCardPrefabs.Add(cardPrefab);
    }

    public bool RemoveCard(GameObject cardPrefab)
    {
        if (handCardPrefabs == null || cardPrefab == null)
        {
            return false;
        }

        return handCardPrefabs.Remove(cardPrefab);
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
