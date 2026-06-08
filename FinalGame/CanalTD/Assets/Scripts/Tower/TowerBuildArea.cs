/*
 * File: TowerBuildArea.cs
 *
 * Purpose:
 * Implements TowerBuildArea for the tower layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TowerBuildArea within the tower system.
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
 * - Verify TowerBuildArea in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildAreaType
{
    Public,
    Claimable
}

public class TowerBuildArea : MonoBehaviour, IPointerClickHandler
{
    [Header("Area Type")]
    public BuildAreaType areaType = BuildAreaType.Public;
    public bool isPublicBuildArea = true;

    [Header("Ownership")]
    public int ownerPlayerId = -1;
    public bool isOwned = false;
    public int landPurchaseCost = 18;
    public PlayerManager playerManager;
    public int inactiveForPlayerId = -1;
    public bool activatesNextTurn = false;

    [Header("Take Over")]
    public bool isFrozenOrSealed = false;
    public int frozenByPlayerId = -1;
    public bool frozenUntilPlayerNextTurn = false;
    public GameObject freezeIcon;

    [Header("Gate Links")]
    public List<GameObject> linkedGates = new List<GameObject>();

    [Header("Tower State")]
    public bool isOccupied = false;
    public Transform towerSpawnPoint;
    public GameObject currentTower;
    public int towerOwnerPlayerId = -1;

    [Header("Visual")]
    public SpriteRenderer highlightRenderer;
    public SpriteRenderer areaVisualRenderer;
    public Color normalColor = Color.white;
    public Color availableColor = Color.green;
    public Color unavailableColor = Color.red;

    [Header("Click Routing")]
    public BuildTowerManager buildTowerManager;

    private Color neutralAreaColor = Color.white;
    private Sprite neutralAreaSprite;

    private void Awake()
    {
        if (towerSpawnPoint == null)
        {
            towerSpawnPoint = transform;
        }

        isPublicBuildArea = areaType == BuildAreaType.Public;

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (buildTowerManager == null)
        {
            buildTowerManager = FindObjectOfType<BuildTowerManager>();
        }

        if (areaVisualRenderer != null)
        {
            neutralAreaColor = areaVisualRenderer.color;
            neutralAreaSprite = areaVisualRenderer.sprite;
        }

        HideHighlight();
        SetFrozenVisual(isFrozenOrSealed);
        RefreshOwnershipVisual(playerManager);
    }

    private void OnMouseDown()
    {
        ReportBuildAreaClicked();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ReportBuildAreaClicked();
    }

    private void ReportBuildAreaClicked()
    {
        // Check if any targeting mode is active - build areas should not be clickable during targeting
        if (IsAnyTargetingModeActive())
        {
            return;
        }

        Debug.Log("BuildArea clicked: " + name);

        if (buildTowerManager == null)
        {
            buildTowerManager = FindObjectOfType<BuildTowerManager>();
        }

        if (buildTowerManager == null)
        {
            Debug.LogWarning(name + " cannot report click because BuildTowerManager is missing.");
            return;
        }

        buildTowerManager.HandleBuildAreaClicked(this);
    }

    private bool IsAnyTargetingModeActive()
    {
        // Check Gate targeting
        if (GateTargetingManager.Instance != null && GateTargetingManager.Instance.IsAnyTargetingActive())
        {
            return true;
        }

        // Check Tile targeting (Take Over, Freeze Claim)
        TileTargetingManager tileTargetingManager = FindObjectOfType<TileTargetingManager>();
        if (tileTargetingManager != null && tileTargetingManager.IsTargeting())
        {
            return true;
        }

        // Check Player targeting (Steal Card, Trade Hands, Disrupt)
        PlayerTargetingManager playerTargetingManager = FindObjectOfType<PlayerTargetingManager>();
        if (playerTargetingManager != null && playerTargetingManager.IsTargeting())
        {
            return true;
        }

        // Check Tower targeting
        TowerTargetingManager towerTargetingManager = FindObjectOfType<TowerTargetingManager>();
        if (towerTargetingManager != null && towerTargetingManager.IsTargeting())
        {
            return true;
        }

        // Check Shock Trap targeting
        ShockTrapTargetingManager shockTrapTargetingManager = FindObjectOfType<ShockTrapTargetingManager>();
        if (shockTrapTargetingManager != null && shockTrapTargetingManager.IsTargeting())
        {
            return true;
        }

        return false;
    }

    public bool CanBuildTower(int playerId)
    {
        if (!CanUseForLandOrTowerAction())
        {
            return false;
        }

        if (isOccupied)
        {
            return false;
        }

        if (areaType == BuildAreaType.Public || isPublicBuildArea)
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
        return areaType == BuildAreaType.Public || isPublicBuildArea;
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
        if (IsInactiveForPlayer(playerId))
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
        SetOwner(playerId, playerManager);
    }

    public void SetOwner(int playerId, PlayerManager visualPlayerManager)
    {
        if (!CanUseForLandOrTowerAction())
        {
            return;
        }

        isOwned = true;
        ownerPlayerId = playerId;
        RefreshOwnershipVisual(visualPlayerManager);
    }

    public void ClearOwner()
    {
        isOwned = false;
        ownerPlayerId = -1;
        ClearActivationDelay();
        RefreshOwnershipVisual(playerManager);
    }

    public void ResetForEliminatedPlayer(int eliminatedPlayerId)
    {
        if (frozenByPlayerId == eliminatedPlayerId)
        {
            ClearFreeze();
        }

        if (inactiveForPlayerId == eliminatedPlayerId)
        {
            ClearActivationDelay();
        }

        CannonTower tower = currentTower != null ? currentTower.GetComponent<CannonTower>() : null;

        if (tower == null && currentTower != null)
        {
            tower = currentTower.GetComponentInChildren<CannonTower>();
        }

        if (tower != null && tower.ownerPlayerId == eliminatedPlayerId)
        {
            RemoveCurrentTower();
        }

        if (ownerPlayerId == eliminatedPlayerId)
        {
            // When an eliminated player loses the land, the land returns to its
            // initial empty state. Remove any tower on it even if ownership data
            // was out of sync.
            RemoveCurrentTower();
            ClearFreeze();
            ClearOwner();
        }
    }

    public bool CanBeTakenOverBy(int playerId)
    {
        if (playerManager != null && playerManager.IsPlayerEliminated(ownerPlayerId))
        {
            return false;
        }

        return areaType == BuildAreaType.Claimable &&
            isOwned &&
            ownerPlayerId >= 0 &&
            ownerPlayerId != playerId &&
            CanUseForLandOrTowerAction();
    }

    public bool IsFrozen()
    {
        return isFrozenOrSealed;
    }

    public bool CanUseForLandOrTowerAction()
    {
        return !isFrozenOrSealed;
    }

    public int GetNormalTakeoverCost()
    {
        return Mathf.CeilToInt(landPurchaseCost * 1.5f);
    }

    public int GetTakeOverCardCost()
    {
        return Mathf.CeilToInt(GetNormalTakeoverCost() / 2f);
    }

    public bool CanBeFrozen()
    {
        return areaType == BuildAreaType.Claimable && !isFrozenOrSealed;
    }

    public void FreezeForPlayer(int playerId)
    {
        isFrozenOrSealed = true;
        frozenByPlayerId = playerId;
        frozenUntilPlayerNextTurn = true;
        SetFrozenVisual(true);
    }

    public void ClearFreeze()
    {
        isFrozenOrSealed = false;
        frozenByPlayerId = -1;
        frozenUntilPlayerNextTurn = false;
        SetFrozenVisual(false);
    }

    public void SetFrozenVisual(bool frozen)
    {
        if (freezeIcon != null)
        {
            freezeIcon.SetActive(frozen);
        }

        CannonTower cannonTower = currentTower != null ? currentTower.GetComponent<CannonTower>() : null;

        if (cannonTower == null && currentTower != null)
        {
            cannonTower = currentTower.GetComponentInChildren<CannonTower>();
        }

        if (cannonTower != null)
        {
            cannonTower.SetDisabledByFreeze(frozen);
        }
    }

    public void ClearFreezeIfPendingForPlayer(int playerId)
    {
        if (isFrozenOrSealed && frozenByPlayerId == playerId)
        {
            ClearFreeze();
        }
    }

    public void MarkInactiveUntilNextTurn(int playerId)
    {
        inactiveForPlayerId = playerId;
        activatesNextTurn = true;
    }

    public void ActivateForPlayerIfPending(int playerId)
    {
        if (IsInactiveForPlayer(playerId))
        {
            ClearActivationDelay();
        }
    }

    public bool IsInactiveForPlayer(int playerId)
    {
        return activatesNextTurn && inactiveForPlayerId == playerId;
    }

    public void ClearActivationDelay()
    {
        inactiveForPlayerId = -1;
        activatesNextTurn = false;
    }

    public void RemoveCurrentTower()
    {
        if (currentTower != null)
        {
            Destroy(currentTower);
        }

        currentTower = null;
        isOccupied = false;
        towerOwnerPlayerId = -1;
    }

    public void RefreshOwnershipVisual(PlayerManager visualPlayerManager)
    {
        if (areaVisualRenderer == null)
        {
            return;
        }

        if (areaType == BuildAreaType.Public || !isOwned || ownerPlayerId < 0)
        {
            areaVisualRenderer.color = neutralAreaColor;
            areaVisualRenderer.sprite = neutralAreaSprite;
            return;
        }

        PlayerManager manager = visualPlayerManager != null ? visualPlayerManager : playerManager;

        if (manager == null)
        {
            return;
        }

        Sprite ownerSprite = manager.GetOwnedLandSprite(ownerPlayerId);

        if (ownerSprite != null)
        {
            areaVisualRenderer.sprite = ownerSprite;
        }

        areaVisualRenderer.color = manager.GetOwnedLandColor(ownerPlayerId);
    }

    public void SetTower(GameObject tower)
    {
        currentTower = tower;
        isOccupied = tower != null;

        if (tower == null)
        {
            towerOwnerPlayerId = -1;
        }
        else
        {
            CannonTower cannonTower = tower.GetComponent<CannonTower>();

            if (cannonTower == null)
            {
                cannonTower = tower.GetComponentInChildren<CannonTower>();
            }

            towerOwnerPlayerId = cannonTower != null ? cannonTower.ownerPlayerId : ownerPlayerId;
        }

        SetFrozenVisual(isFrozenOrSealed);
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

    public void ShowHighlight(Color color)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.enabled = true;
        highlightRenderer.color = color;
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
