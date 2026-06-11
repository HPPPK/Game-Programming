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

    /// <summary>
    /// Finds and stores tower build area references before scene gameplay begins.
    /// </summary>
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

    /// <summary>
    /// Responds to a mouse click on this scene object and starts the related action.
    /// </summary>
    private void OnMouseDown()
    {
        ReportBuildAreaClicked();
    }

    /// <summary>
    /// Responds to a UI click and forwards it to the related menu or gameplay action.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        ReportBuildAreaClicked();
    }

    /// <summary>
    /// Handles report build area clicked for land ownership, tower actions, or build UI.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether any targeting mode active is true.
    /// </summary>
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

    /// <summary>
    /// Checks whether build tower is allowed before enabling that action.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether public is true.
    /// </summary>
    public bool IsPublic()
    {
        return areaType == BuildAreaType.Public || isPublicBuildArea;
    }

    /// <summary>
    /// Checks the current state to decide whether claimable is true.
    /// </summary>
    public bool IsClaimable()
    {
        return areaType == BuildAreaType.Claimable;
    }

    /// <summary>
    /// Checks the current state to decide whether owned by is true.
    /// </summary>
    public bool IsOwnedBy(int playerId)
    {
        return isOwned && ownerPlayerId == playerId;
    }

    /// <summary>
    /// Checks the current state to decide whether owned by other player is true.
    /// </summary>
    public bool IsOwnedByOtherPlayer(int playerId)
    {
        return isOwned && ownerPlayerId != playerId;
    }

    /// <summary>
    /// Checks the current state to decide whether unowned is true.
    /// </summary>
    public bool IsUnowned()
    {
        return !isOwned || ownerPlayerId < 0;
    }

    /// <summary>
    /// Checks whether control gate is allowed before enabling that action.
    /// </summary>
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

    /// <summary>
    /// Handles controls gate for land ownership, tower actions, or build UI.
    /// </summary>
    public bool ControlsGate(GameObject gate)
    {
        if (gate == null)
        {
            return false;
        }

        return linkedGates.Contains(gate);
    }

    /// <summary>
    /// Returns linked gates used by land, tower, cost, or build decisions.
    /// </summary>
    public List<GameObject> GetLinkedGates()
    {
        return linkedGates;
    }

    /// <summary>
    /// Handles claim area for land ownership, tower actions, or build UI.
    /// </summary>
    public void ClaimArea(int playerId)
    {
        SetOwner(playerId, playerManager);
    }

    /// <summary>
    /// Sets owner and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Clears owner and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearOwner()
    {
        isOwned = false;
        ownerPlayerId = -1;
        ClearActivationDelay();
        RefreshOwnershipVisual(playerManager);
    }

    /// <summary>
    /// Resets for eliminated player for a new turn, wave, player, scene, or match state.
    /// </summary>
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

    /// <summary>
    /// Checks whether be taken over by is allowed before enabling that action.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether frozen is true.
    /// </summary>
    public bool IsFrozen()
    {
        return isFrozenOrSealed;
    }

    /// <summary>
    /// Checks whether use for land or tower action is allowed before enabling that action.
    /// </summary>
    public bool CanUseForLandOrTowerAction()
    {
        return !isFrozenOrSealed;
    }

    /// <summary>
    /// Returns normal takeover cost used by land, tower, cost, or build decisions.
    /// </summary>
    public int GetNormalTakeoverCost()
    {
        return Mathf.CeilToInt(landPurchaseCost * 1.5f);
    }

    /// <summary>
    /// Returns take over card cost used by card handling or target selection.
    /// </summary>
    public int GetTakeOverCardCost()
    {
        return Mathf.CeilToInt(GetNormalTakeoverCost() / 2f);
    }

    /// <summary>
    /// Checks whether be frozen is allowed before enabling that action.
    /// </summary>
    public bool CanBeFrozen()
    {
        return areaType == BuildAreaType.Claimable && !isFrozenOrSealed;
    }

    /// <summary>
    /// Handles freeze for player for land ownership, tower actions, or build UI.
    /// </summary>
    public void FreezeForPlayer(int playerId)
    {
        isFrozenOrSealed = true;
        frozenByPlayerId = playerId;
        frozenUntilPlayerNextTurn = true;
        SetFrozenVisual(true);
    }

    /// <summary>
    /// Clears freeze and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearFreeze()
    {
        isFrozenOrSealed = false;
        frozenByPlayerId = -1;
        frozenUntilPlayerNextTurn = false;
        SetFrozenVisual(false);
    }

    /// <summary>
    /// Sets frozen visual and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Clears freeze if pending for player and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearFreezeIfPendingForPlayer(int playerId)
    {
        if (isFrozenOrSealed && frozenUntilPlayerNextTurn && frozenByPlayerId == playerId)
        {
            ClearFreeze();
        }
    }

    /// <summary>
    /// Marks inactive until next turn so later turns or systems can react to it.
    /// </summary>
    public void MarkInactiveUntilNextTurn(int playerId)
    {
        inactiveForPlayerId = playerId;
        activatesNextTurn = true;
    }

    /// <summary>
    /// Activates for player if pending once its pending condition is satisfied.
    /// </summary>
    public void ActivateForPlayerIfPending(int playerId)
    {
        if (IsInactiveForPlayer(playerId))
        {
            ClearActivationDelay();
        }
    }

    /// <summary>
    /// Checks the current state to decide whether inactive for player is true.
    /// </summary>
    public bool IsInactiveForPlayer(int playerId)
    {
        return activatesNextTurn && inactiveForPlayerId == playerId;
    }

    /// <summary>
    /// Clears activation delay and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearActivationDelay()
    {
        inactiveForPlayerId = -1;
        activatesNextTurn = false;
    }

    /// <summary>
    /// Removes current tower from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Refreshes ownership visual from the latest gameplay data.
    /// </summary>
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

    /// <summary>
    /// Sets tower and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Shows available with the correct current context.
    /// </summary>
    public void ShowAvailable(bool available)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.enabled = true;
        highlightRenderer.color = available ? availableColor : unavailableColor;
    }

    /// <summary>
    /// Shows highlight with the correct current context.
    /// </summary>
    public void ShowHighlight(Color color)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.enabled = true;
        highlightRenderer.color = color;
    }

    /// <summary>
    /// Hides highlight and clears temporary visual state.
    /// </summary>
    public void HideHighlight()
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.color = normalColor;
    }
}
