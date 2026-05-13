using System.Collections.Generic;
using UnityEngine;

public enum BuildAreaType
{
    Public,
    Claimable
}

public class TowerBuildArea : MonoBehaviour
{
    [Header("Area Type")]
    public BuildAreaType areaType = BuildAreaType.Public;

    [Header("Ownership")]
    public int ownerPlayerId = -1;
    public bool isOwned = false;
    public int landPurchaseCost = 18;

    [Header("Gate Links")]
    public List<GameObject> linkedGates = new List<GameObject>();

    [Header("Tower State")]
    public bool isOccupied = false;
    public Transform towerSpawnPoint;
    public GameObject currentTower;

    [Header("Visual")]
    public SpriteRenderer highlightRenderer;
    public Color normalColor = Color.white;
    public Color availableColor = Color.green;
    public Color unavailableColor = Color.red;

    private void Awake()
    {
        if (towerSpawnPoint == null)
        {
            towerSpawnPoint = transform;
        }

        HideHighlight();
    }

    public bool CanBuildTower(int playerId)
    {
        if (isOccupied)
        {
            return false;
        }

        if (areaType == BuildAreaType.Public)
        {
            return true;
        }

        if (areaType == BuildAreaType.Claimable)
        {
            return isOwned && ownerPlayerId == playerId;
        }

        return false;
    }

    public bool IsPublic()
    {
        return areaType == BuildAreaType.Public;
    }

    public bool IsClaimable()
    {
        return areaType == BuildAreaType.Claimable;
    }

    public bool IsOwnedBy(int playerId)
    {
        return isOwned && ownerPlayerId == playerId;
    }

    public bool IsOwnedByOtherPlayer(int playerId)
    {
        return isOwned && ownerPlayerId != playerId;
    }

    public bool IsUnowned()
    {
        return !isOwned || ownerPlayerId < 0;
    }

    public bool CanControlGate(int playerId)
    {
        if (areaType == BuildAreaType.Public)
        {
            return true;
        }

        if (areaType == BuildAreaType.Claimable)
        {
            return isOwned && ownerPlayerId == playerId;
        }

        return false;
    }

    public bool ControlsGate(GameObject gate)
    {
        if (gate == null)
        {
            return false;
        }

        return linkedGates.Contains(gate);
    }

    public List<GameObject> GetLinkedGates()
    {
        return linkedGates;
    }

    public void ClaimArea(int playerId)
    {
        isOwned = true;
        ownerPlayerId = playerId;
    }

    public void ClearOwner()
    {
        isOwned = false;
        ownerPlayerId = -1;
    }

    public void SetTower(GameObject tower)
    {
        currentTower = tower;
        isOccupied = tower != null;
    }

    public void ShowAvailable(bool available)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.enabled = true;
        highlightRenderer.color = available ? availableColor : unavailableColor;
    }

    public void HideHighlight()
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.color = normalColor;
    }
}
