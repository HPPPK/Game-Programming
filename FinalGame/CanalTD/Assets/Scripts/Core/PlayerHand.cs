/*
 * File: PlayerHand.cs
 *
 * Purpose:
 * Implements PlayerHand for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PlayerHand within the core system.
 * - Update the owning object state and react to gameplay events during play.
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
 * - Verify PlayerHand in the scene or prefab where it is used and confirm the main happy path still works.
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

    /// <summary>
    /// Checks whether add card is allowed before enabling that action.
    /// </summary>
    public bool CanAddCard()
    {
        return GetCardCount() < maxHandSize;
    }

    /// <summary>
    /// Adds card to the relevant list, UI, hand, deck, or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Removes card from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
    public bool RemoveCard(GameObject cardPrefab)
    {
        if (handCardPrefabs == null || cardPrefab == null)
        {
            return false;
        }

        return handCardPrefabs.Remove(cardPrefab);
    }

    /// <summary>
    /// Removes random card from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Returns card count used by card handling or target selection.
    /// </summary>
    public int GetCardCount()
    {
        return handCardPrefabs != null ? handCardPrefabs.Count : 0;
    }

    /// <summary>
    /// Clears clear and removes its temporary gameplay or visual effect.
    /// </summary>
    public void Clear()
    {
        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
            return;
        }

        handCardPrefabs.Clear();
    }

    /// <summary>
    /// Returns cards used by card handling or target selection.
    /// </summary>
    public List<GameObject> GetCards()
    {
        if (handCardPrefabs == null)
        {
            handCardPrefabs = new List<GameObject>();
        }

        return handCardPrefabs;
    }

    /// <summary>
    /// Handles copy from for this gameplay system.
    /// </summary>
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
