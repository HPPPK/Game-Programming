/*
 * File: BuildTowerManager.cs
 *
 * Purpose:
 * Implements BuildTowerManager for the tower layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tower prefabs, build spots, projectiles, or tower-related scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for BuildTowerManager within the tower system.
 * - Coordinate related objects, state changes, and cross-system communication.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify BuildTowerManager in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Reflection;

public class BuildTowerManager : MonoBehaviour
{
    public static BuildTowerManager Instance { get; private set; }

    [Header("Player")]
    public int currentPlayerId = 0;
    public PlayerResource currentPlayerResource;
    public PlayerManager playerManager;

    [Header("Tower")]
    public GameObject cannonTowerPrefab;
    public int cannonTowerCost = 6;
    public Transform towersParent;

    [Header("Tower Type Prefabs")]
    public GameObject defaultCannonPrefab;
    public GameObject defaultArcherPrefab;
    public GameObject defaultFrostPrefab;
    public GameObject defaultShockPrefab;
    public GameObject defaultSniperPrefab;

    [Header("Player Tower Prefabs")]
    public GameObject[] cannonPrefabsByPlayerId = new GameObject[4];
    public GameObject[] archerPrefabsByPlayerId = new GameObject[4];
    public GameObject[] frostPrefabsByPlayerId = new GameObject[4];
    public GameObject[] shockPrefabsByPlayerId = new GameObject[4];
    public GameObject[] sniperPrefabsByPlayerId = new GameObject[4];

    [Header("Radial Menu")]
    public RadialTowerMenu radialTowerMenu;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Camera")]
    public Camera targetCamera;

    [Header("Toast Message")]
    public MonoBehaviour toastMessage;
    public GameObject toastMessageObject;
    public TextMeshProUGUI toastText;
    public float toastDuration = 1.2f;

    private Coroutine toastCoroutine;
    private TowerBuildArea lastClickedArea;
    private int lastClickedFrame = -1;

    private void Awake()
    {
        Instance = this;

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (radialTowerMenu != null)
        {
            radialTowerMenu.Initialize(this);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBuildTowerAtMouse();
        }
    }

    private void TryBuildTowerAtMouse()
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource();

        if (TurnSourceResolver.IsAIPrototypeActive() && (turnSource == null || !turnSource.CanHumanAct))
        {
            return;
        }

        if (GateTargetingManager.Instance != null && GateTargetingManager.Instance.IsAnyTargetingActive())
        {
            return;
        }

        TileTargetingManager tileTargetingManager = FindObjectOfType<TileTargetingManager>();

        if (tileTargetingManager != null && tileTargetingManager.IsTargeting())
        {
            return;
        }

        PlayerTargetingManager playerTargetingManager = FindObjectOfType<PlayerTargetingManager>();

        if (playerTargetingManager != null && playerTargetingManager.IsTargeting())
        {
            return;
        }

        TowerTargetingManager towerTargetingManager = FindObjectOfType<TowerTargetingManager>();

        if (towerTargetingManager != null && towerTargetingManager.IsTargeting())
        {
            return;
        }

        ShockTrapTargetingManager shockTrapTargetingManager = FindObjectOfType<ShockTrapTargetingManager>();

        if (shockTrapTargetingManager != null && shockTrapTargetingManager.IsTargeting())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (targetCamera == null)
        {
            ShowToast("Camera is missing.");
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );

        Vector2 clickPosition = new Vector2(worldPosition.x, worldPosition.y);
        Collider2D[] hits = Physics2D.OverlapPointAll(clickPosition);

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        TowerBuildArea buildArea = null;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            buildArea = hit.GetComponent<TowerBuildArea>();

            if (buildArea == null)
            {
                buildArea = hit.GetComponentInParent<TowerBuildArea>();
            }

            if (buildArea != null)
            {
                break;
            }
        }

        if (buildArea == null)
        {
            return;
        }

        HandleBuildAreaClicked(buildArea);
    }

    public void HandleBuildAreaClicked(TowerBuildArea buildArea)
    {
        if (!CanInteractWithBuildAreas())
        {
            return;
        }

        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (ShouldBlockTutorialBuildAreaClick(buildArea))
        {
            return;
        }

        if (buildArea != null && lastClickedArea == buildArea && lastClickedFrame == Time.frameCount)
        {
            return;
        }

        lastClickedArea = buildArea;
        lastClickedFrame = Time.frameCount;

        if (buildArea != null)
        {
            Debug.Log("BuildTowerManager received clicked area: " + buildArea.name);
        }

        if (buildArea == null)
        {
            return;
        }

        PlayerResource activePlayerResource = GetCurrentPlayerResource();

        if (activePlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (buildArea.IsFrozen())
        {
            ShowToast("This land is frozen.");
            return;
        }

        if (buildArea.isOccupied)
        {
            HandleOccupiedBuildAreaClick(buildArea);
            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsUnowned())
        {
            if (radialTowerMenu != null)
            {
                radialTowerMenu.ShowBuyLandConfirm(buildArea, buildArea.landPurchaseCost);
            }
            else if (TryBuyLand(buildArea))
            {
                ShowBuildMenuOrFallback(buildArea);
            }

            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsOwnedByOtherPlayer(GetCurrentPlayerId()))
        {
            ShowToast("This land belongs to another player.");
            return;
        }

        ShowBuildMenuOrFallback(buildArea);
    }

    private void HandleOccupiedBuildAreaClick(TowerBuildArea buildArea)
    {
        int activePlayerId = GetCurrentPlayerId();

        if (buildArea.towerOwnerPlayerId >= 0 && buildArea.towerOwnerPlayerId != activePlayerId)
        {
            ShowToast("This tower belongs to another player.");
            return;
        }

        TowerStats stats = GetTowerStatsFromArea(buildArea);

        if (stats == null)
        {
            ShowToast("Tower stats are missing.");
            return;
        }

        if (radialTowerMenu != null)
        {
            radialTowerMenu.ShowTowerManagement(buildArea);
        }
    }

    public void HideBuildInteractionUI()
    {
        if (radialTowerMenu != null)
        {
            radialTowerMenu.Hide();
        }
    }

    public bool TryBuyLand(TowerBuildArea buildArea)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.BuyLand, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (!CanPerformTowerOrLandAction())
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            PhotonOnlineBuildSyncManager syncManager = PhotonOnlineBuildSyncManager.Instance != null
                ? PhotonOnlineBuildSyncManager.Instance
                : FindObjectOfType<PhotonOnlineBuildSyncManager>();

            if (syncManager == null)
            {
                ShowToast("Online build sync is missing.");
                return false;
            }

            return syncManager.RequestBuyLand(buildArea);
        }

        return TryBuyLandForPlayer(buildArea, GetCurrentPlayerId(), GetCurrentPlayerResource(), true);
    }

    public bool TryBuyLandForPlayer(TowerBuildArea buildArea, int playerId)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        return TryBuyLandForPlayer(buildArea, playerId, playerResource, false);
    }

    private bool TryBuyLandForPlayer(
        TowerBuildArea buildArea,
        int playerId,
        PlayerResource playerResource,
        bool showMessages)
    {
        if (buildArea == null)
        {
            if (showMessages) ShowToast("Land tile is missing.");
            return false;
        }

        if (playerResource == null)
        {
            if (showMessages) ShowToast("Player resource is missing.");
            return false;
        }

        if (buildArea.IsFrozen())
        {
            if (showMessages) ShowToast("This land is frozen.");
            return false;
        }

        if (!buildArea.IsClaimable() || !buildArea.IsUnowned())
        {
            if (showMessages) ShowToast("This land cannot be purchased.");
            return false;
        }

        if (!playerResource.CanAfford(buildArea.landPurchaseCost))
        {
            if (showMessages) ShowToast("Not enough gold to buy this land.");
            return false;
        }

        if (!playerResource.SpendMoney(buildArea.landPurchaseCost))
        {
            if (showMessages) ShowToast("Not enough gold to buy this land.");
            return false;
        }

        buildArea.SetOwner(playerId, playerManager);
        RefreshCurrentPlayerUI();
        if (showMessages) ShowToast("Land purchased.");
        TutorialManager.Instance?.NotifyLandPurchased(buildArea.gameObject);
        return true;
    }

    private bool TryPurchaseLand(TowerBuildArea buildArea)
    {
        return TryBuyLand(buildArea);
    }

    private void TryBuildTower(TowerBuildArea buildArea)
    {
        TryBuildTower(buildArea, TowerType.Cannon, GetCurrentPlayerId(), GetCurrentPlayerResource(), true);
    }

    private bool TryBuildTower(
        TowerBuildArea buildArea,
        TowerType towerType,
        int activePlayerId,
        PlayerResource activePlayerResource,
        bool showMessages)
    {
        if (activePlayerResource == null)
        {
            if (showMessages) ShowToast("Player resource is missing.");
            return false;
        }

        if (buildArea.IsFrozen())
        {
            if (showMessages) ShowToast("This land is frozen.");
            return false;
        }

        bool usesPlayerSpecificTowerPrefab = HasPlayerSpecificTowerPrefab(towerType, activePlayerId);
        GameObject towerPrefab = ResolveTowerPrefab(towerType, activePlayerId);

        if (towerPrefab == null)
        {
            if (showMessages) ShowToast("Tower prefab is missing.");
            return false;
        }

        if (buildArea.isOccupied)
        {
            if (showMessages) ShowToast("A tower is already built here.");
            return false;
        }

        if (!buildArea.CanBuildTower(activePlayerId))
        {
            if (showMessages) ShowToast("This land belongs to another player.");
            return false;
        }

        int towerCost = GetTowerCost(towerType, activePlayerId);

        if (!activePlayerResource.CanAfford(towerCost))
        {
            if (showMessages) ShowToast("Not enough gold to build a tower.");
            return false;
        }

        if (!activePlayerResource.SpendMoney(towerCost))
        {
            if (showMessages) ShowToast("Not enough gold to build a tower.");
            return false;
        }

        Transform spawnPoint = buildArea.towerSpawnPoint != null
            ? buildArea.towerSpawnPoint
            : buildArea.transform;

        GameObject tower = towersParent != null
            ? Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity, towersParent)
            : Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity);

        Debug.Log("Building " + towerType + " for Player " + activePlayerId + " using prefab " + towerPrefab.name);
        Debug.Log("Tower built under parent: " + (tower.transform.parent != null ? tower.transform.parent.name : "None"));

        ForceTowerAlphaOpaque(tower);
        EnsureTowerCollider(tower);

        TowerStats towerStats = EnsureTowerStats(tower, towerType, towerCost, activePlayerId);
        CannonTower cannonTower = tower.GetComponent<CannonTower>();

        if (cannonTower != null)
        {
            cannonTower.ownerPlayerId = activePlayerId;
            cannonTower.ownerResource = activePlayerResource;
            cannonTower.ApplyOwnerVisual(playerManager, activePlayerId, !usesPlayerSpecificTowerPrefab);
            cannonTower.SyncAttackFieldsFromStats();
        }
        else if (!usesPlayerSpecificTowerPrefab)
        {
            ApplyOwnerVisualToGenericTower(tower, activePlayerId);
        }

        ForceTowerAlphaOpaque(tower);

        buildArea.SetTower(tower);
        buildArea.towerOwnerPlayerId = activePlayerId;
        TutorialManager.Instance?.RegisterRuntimeBuiltTower(buildArea.gameObject, tower);
        RefreshCurrentPlayerUI();
        if (showMessages) ShowToast(towerStats.towerType + " tower built.");
        TutorialManager.Instance?.NotifyTowerBuilt(buildArea.gameObject);
        AudioManager.Instance?.PlayBuild();
        return true;
    }

    public void TryBuildTowerFromMenu(TowerBuildArea buildArea, TowerType towerType)
    {
        TryBuildTower(buildArea, towerType);
    }

    public void TryUpgradeTowerFromMenu(TowerBuildArea buildArea)
    {
        TryUpgradeTower(buildArea);
    }

    public void TrySellTowerFromMenu(TowerBuildArea buildArea)
    {
        TrySellTower(buildArea);
    }

    public bool TryBuildTower(TowerBuildArea buildArea, TowerType towerType)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.BuildTower, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (!CanPerformTowerOrLandAction())
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            PhotonOnlineBuildSyncManager syncManager = PhotonOnlineBuildSyncManager.Instance != null
                ? PhotonOnlineBuildSyncManager.Instance
                : FindObjectOfType<PhotonOnlineBuildSyncManager>();

            if (syncManager == null)
            {
                ShowToast("Online build sync is missing.");
                return false;
            }

            return syncManager.RequestBuildTower(buildArea, towerType);
        }

        return TryBuildTowerForPlayer(buildArea, towerType, GetCurrentPlayerId(), true);
    }

    public bool TryBuildTowerForPlayer(TowerBuildArea buildArea, TowerType towerType, int playerId)
    {
        return TryBuildTowerForPlayer(buildArea, towerType, playerId, false);
    }

    public bool TryBuildTowerForPlayer(TowerBuildArea buildArea, TowerType towerType, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        return TryBuildTower(buildArea, towerType, playerId, playerResource, showMessages);
    }

    public bool ConfirmBuyLand(TowerBuildArea buildArea)
    {
        return TryBuyLand(buildArea);
    }

    public bool ConfirmBuildTower(TowerBuildArea buildArea, TowerType towerType)
    {
        return TryBuildTower(buildArea, towerType);
    }

    public bool ConfirmUpgradeTower(TowerBuildArea buildArea)
    {
        return TryUpgradeTower(buildArea);
    }

    public bool ConfirmSellTower(TowerBuildArea buildArea)
    {
        return TrySellTower(buildArea);
    }

    public bool TryUpgradeTower(TowerBuildArea buildArea)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.UpgradeTower, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (!CanPerformTowerOrLandAction())
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            PhotonOnlineBuildSyncManager syncManager = PhotonOnlineBuildSyncManager.Instance != null
                ? PhotonOnlineBuildSyncManager.Instance
                : FindObjectOfType<PhotonOnlineBuildSyncManager>();

            if (syncManager == null)
            {
                ShowToast("Online build sync is missing.");
                return false;
            }

            return syncManager.RequestUpgradeTower(buildArea);
        }

        return TryUpgradeTowerForPlayer(buildArea, GetCurrentPlayerId(), true);
    }

    public bool TryUpgradeTowerForPlayer(TowerBuildArea buildArea, int playerId)
    {
        return TryUpgradeTowerForPlayer(buildArea, playerId, false);
    }

    public bool TryUpgradeTowerForPlayer(TowerBuildArea buildArea, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        return TryUpgradeTower(buildArea, playerId, playerResource, showMessages);
    }

    public bool TrySellTower(TowerBuildArea buildArea)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.SellTower, buildArea != null ? buildArea.gameObject : null))
        {
            return false;
        }

        if (!CanPerformTowerOrLandAction())
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            PhotonOnlineBuildSyncManager syncManager = PhotonOnlineBuildSyncManager.Instance != null
                ? PhotonOnlineBuildSyncManager.Instance
                : FindObjectOfType<PhotonOnlineBuildSyncManager>();

            if (syncManager == null)
            {
                ShowToast("Online build sync is missing.");
                return false;
            }

            return syncManager.RequestSellTower(buildArea);
        }

        return TrySellTowerForPlayer(buildArea, GetCurrentPlayerId(), true);
    }

    public bool TrySellTowerForPlayer(TowerBuildArea buildArea, int playerId)
    {
        return TrySellTowerForPlayer(buildArea, playerId, false);
    }

    public bool TrySellTowerForPlayer(TowerBuildArea buildArea, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        return TrySellTower(buildArea, playerId, playerResource, showMessages);
    }

    public bool TryBuildTowerForAI(TowerBuildArea buildArea, TowerType type, int playerId)
    {
        return TryBuildTowerForPlayer(buildArea, type, playerId, false);
    }

    public bool TryUpgradeTowerForAI(TowerBuildArea buildArea, int playerId)
    {
        return TryUpgradeTowerForPlayer(buildArea, playerId, false);
    }

    public bool TrySellTowerForAI(TowerBuildArea buildArea, int playerId)
    {
        return TrySellTowerForPlayer(buildArea, playerId, false);
    }

    public int GetTowerCost(TowerType towerType)
    {
        return GetTowerCost(towerType, GetCurrentPlayerId());
    }

    // Used by RadialTowerMenu previews so the ghost tower matches the real build prefab.
    public GameObject GetTowerPreviewPrefab(TowerType towerType)
    {
        return ResolveTowerPrefab(towerType, GetCurrentPlayerId());
    }

    public int GetTowerCost(TowerType towerType, int playerId)
    {
        GameObject prefab = ResolveTowerPrefab(towerType, playerId);
        TowerStats stats = prefab != null ? prefab.GetComponent<TowerStats>() : null;

        if (stats == null && prefab != null)
        {
            stats = prefab.GetComponentInChildren<TowerStats>();
        }

        if (stats != null && stats.baseCost > 0)
        {
            return stats.baseCost;
        }

        return cannonTowerCost;
    }

    public GameObject GetResolvedTowerPrefab(TowerType towerType, int playerId)
    {
        return ResolveTowerPrefab(towerType, playerId);
    }

    public TowerBuildArea ResolveBuildAreaByStableId(string buildAreaId)
    {
        if (string.IsNullOrWhiteSpace(buildAreaId))
        {
            return null;
        }

        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>(true);

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea != null && buildArea.name == buildAreaId)
            {
                return buildArea;
            }
        }

        return null;
    }

    public bool ApplyOnlineBuyLand(string buildAreaId, int ownerPlayerId, int newGold)
    {
        TowerBuildArea buildArea = ResolveBuildAreaByStableId(buildAreaId);
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;

        if (buildArea == null || playerResource == null)
        {
            return false;
        }

        buildArea.SetOwner(ownerPlayerId, playerManager);
        ApplySyncedPlayerGold(playerResource, newGold);
        FinalizeSyncedAreaChange(buildArea);
        return true;
    }

    public bool ApplyOnlineBuildTower(string buildAreaId, string towerId, TowerType towerType, int ownerPlayerId, int level, int newGold)
    {
        TowerBuildArea buildArea = ResolveBuildAreaByStableId(buildAreaId);
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;

        if (buildArea == null || playerResource == null)
        {
            return false;
        }

        GameObject tower = EnsureOnlineTowerState(buildArea, towerId, towerType, ownerPlayerId, Mathf.Max(1, level));

        if (tower == null)
        {
            return false;
        }

        ApplySyncedPlayerGold(playerResource, newGold);
        FinalizeSyncedAreaChange(buildArea);
        return true;
    }

    public bool ApplyOnlineUpgradeTower(string buildAreaId, string towerId, int ownerPlayerId, int newLevel, int newGold)
    {
        TowerBuildArea buildArea = ResolveBuildAreaByStableId(buildAreaId);
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;
        TowerStats currentStats = GetTowerStatsFromArea(buildArea);

        if (buildArea == null || playerResource == null || currentStats == null)
        {
            return false;
        }

        GameObject tower = EnsureOnlineTowerState(
            buildArea,
            string.IsNullOrWhiteSpace(towerId) ? buildAreaId + "_tower" : towerId,
            currentStats.towerType,
            ownerPlayerId,
            Mathf.Max(1, newLevel)
        );

        if (tower == null)
        {
            return false;
        }

        ApplySyncedPlayerGold(playerResource, newGold);
        FinalizeSyncedAreaChange(buildArea);
        return true;
    }

    public bool ApplyOnlineSellTower(string buildAreaId, string towerId, int ownerPlayerId, int newGold)
    {
        TowerBuildArea buildArea = ResolveBuildAreaByStableId(buildAreaId);
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;

        if (buildArea == null || playerResource == null)
        {
            return false;
        }

        buildArea.RemoveCurrentTower();
        buildArea.towerOwnerPlayerId = -1;
        ApplySyncedPlayerGold(playerResource, newGold);
        FinalizeSyncedAreaChange(buildArea);
        return true;
    }

    private bool TryUpgradeTower(
        TowerBuildArea buildArea,
        int playerId,
        PlayerResource playerResource,
        bool showMessages)
    {
        TowerStats stats = GetTowerStatsFromArea(buildArea);

        if (stats == null)
        {
            if (showMessages) ShowToast("Tower stats are missing.");
            return false;
        }

        if (buildArea.towerOwnerPlayerId != playerId)
        {
            if (showMessages) ShowToast("This tower belongs to another player.");
            return false;
        }

        if (!stats.CanUpgrade())
        {
            if (showMessages) ShowToast("Max Level.");
            return false;
        }

        int upgradeCost = stats.GetUpgradeCost();

        if (playerResource == null || !playerResource.SpendMoney(upgradeCost))
        {
            if (showMessages) ShowToast("Not enough gold to upgrade.");
            return false;
        }

        stats.Upgrade();
        CannonTower tower = buildArea.currentTower != null ? buildArea.currentTower.GetComponent<CannonTower>() : null;

        if (tower == null && buildArea.currentTower != null)
        {
            tower = buildArea.currentTower.GetComponentInChildren<CannonTower>();
        }

        if (tower != null)
        {
            tower.SyncAttackFieldsFromStats();
        }

        RefreshCurrentPlayerUI();
        if (showMessages) ShowToast("Tower upgraded.");
        TutorialManager.Instance?.NotifyTowerUpgraded(buildArea.gameObject);
        return true;
    }

    private bool TrySellTower(
        TowerBuildArea buildArea,
        int playerId,
        PlayerResource playerResource,
        bool showMessages)
    {
        TowerStats stats = GetTowerStatsFromArea(buildArea);

        if (buildArea == null || buildArea.currentTower == null || stats == null)
        {
            if (showMessages) ShowToast("No tower to sell.");
            return false;
        }

        if (buildArea.towerOwnerPlayerId != playerId)
        {
            if (showMessages) ShowToast("This tower belongs to another player.");
            return false;
        }

        if (playerResource != null)
        {
            playerResource.AddMoney(stats.GetSellValue());
        }

        buildArea.RemoveCurrentTower();
        buildArea.towerOwnerPlayerId = -1;
        RefreshCurrentPlayerUI();
        if (showMessages) ShowToast("Tower sold.");
        TutorialManager.Instance?.NotifyTowerSold(buildArea.gameObject);
        return true;
    }

    private GameObject EnsureOnlineTowerState(TowerBuildArea buildArea, string towerId, TowerType towerType, int ownerPlayerId, int level)
    {
        if (buildArea == null)
        {
            return null;
        }

        GameObject existingTower = buildArea.currentTower;
        TowerStats existingStats = GetTowerStatsFromArea(buildArea);
        bool needsRebuild = existingTower == null ||
            existingStats == null ||
            existingStats.towerType != towerType ||
            buildArea.towerOwnerPlayerId != ownerPlayerId ||
            (existingStats != null && existingStats.level > level);

        GameObject tower = existingTower;

        if (needsRebuild)
        {
            if (buildArea.currentTower != null)
            {
                buildArea.RemoveCurrentTower();
            }

            GameObject towerPrefab = ResolveTowerPrefab(towerType, ownerPlayerId);

            if (towerPrefab == null)
            {
                return null;
            }

            Transform spawnPoint = buildArea.towerSpawnPoint != null
                ? buildArea.towerSpawnPoint
                : buildArea.transform;

            tower = towersParent != null
                ? Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity, towersParent)
                : Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity);

            if (!string.IsNullOrWhiteSpace(towerId))
            {
                tower.name = towerId;
            }

            ForceTowerAlphaOpaque(tower);
            EnsureTowerCollider(tower);

            int baseCost = GetTowerCost(towerType, ownerPlayerId);
            TowerStats towerStats = EnsureTowerStats(tower, towerType, baseCost, ownerPlayerId);
            CannonTower cannonTower = tower.GetComponent<CannonTower>();

            if (cannonTower == null)
            {
                cannonTower = tower.GetComponentInChildren<CannonTower>();
            }

            if (cannonTower != null)
            {
                cannonTower.ownerPlayerId = ownerPlayerId;
                cannonTower.ownerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;
                cannonTower.ApplyOwnerVisual(playerManager, ownerPlayerId, !HasPlayerSpecificTowerPrefab(towerType, ownerPlayerId));
                cannonTower.SyncAttackFieldsFromStats();
            }
            else if (!HasPlayerSpecificTowerPrefab(towerType, ownerPlayerId))
            {
                ApplyOwnerVisualToGenericTower(tower, ownerPlayerId);
            }

            buildArea.SetTower(tower);
            buildArea.towerOwnerPlayerId = ownerPlayerId;
            TutorialManager.Instance?.RegisterRuntimeBuiltTower(buildArea.gameObject, tower);
            existingStats = towerStats;
        }

        if (existingStats == null)
        {
            existingStats = GetTowerStatsFromArea(buildArea);
        }

        if (existingStats == null)
        {
            return null;
        }

        while (existingStats.level < level && existingStats.CanUpgrade())
        {
            existingStats.Upgrade();
        }

        CannonTower syncedTower = buildArea.currentTower != null ? buildArea.currentTower.GetComponent<CannonTower>() : null;

        if (syncedTower == null && buildArea.currentTower != null)
        {
            syncedTower = buildArea.currentTower.GetComponentInChildren<CannonTower>();
        }

        if (syncedTower != null)
        {
            syncedTower.ownerPlayerId = ownerPlayerId;
            syncedTower.ownerResource = playerManager != null ? playerManager.GetPlayerResource(ownerPlayerId) : null;
            syncedTower.SyncAttackFieldsFromStats();
        }

        return buildArea.currentTower;
    }

    private void ShowBuildMenuOrFallback(TowerBuildArea buildArea)
    {
        if (buildArea != null)
        {
            Debug.Log("Showing build menu for " + buildArea.name);
        }

        if (radialTowerMenu != null)
        {
            radialTowerMenu.ShowBuildSelection(buildArea);
            return;
        }

        TryBuildTower(buildArea);
    }

    private Vector3 GetAreaMenuPosition(TowerBuildArea buildArea)
    {
        if (buildArea == null)
        {
            return Vector3.zero;
        }

        Transform spawnPoint = buildArea.towerSpawnPoint != null ? buildArea.towerSpawnPoint : buildArea.transform;
        return spawnPoint.position;
    }

    private TowerStats GetTowerStatsFromArea(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.currentTower == null)
        {
            return null;
        }

        TowerStats stats = buildArea.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : buildArea.currentTower.GetComponentInChildren<TowerStats>();
    }

    private TowerStats EnsureTowerStats(GameObject tower, TowerType towerType, int baseCost, int ownerPlayerId)
    {
        TowerStats stats = tower != null ? tower.GetComponent<TowerStats>() : null;

        if (stats == null && tower != null)
        {
            stats = tower.GetComponentInChildren<TowerStats>();
        }

        if (stats == null && tower != null)
        {
            stats = tower.AddComponent<TowerStats>();
        }

        if (stats != null)
        {
            stats.towerType = towerType;
            stats.ownerPlayerId = ownerPlayerId;

            if (stats.baseCost <= 0)
            {
                stats.baseCost = baseCost;
            }

            if (stats.totalGoldInvested <= 0)
            {
                stats.totalGoldInvested = baseCost;
            }
        }

        return stats;
    }

    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : currentPlayerId;
    }

    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : currentPlayerResource;
    }

    // Phase 2B is still local-only gameplay execution, so non-turn online
    // clients must be blocked at the shared build/tower entry points.
    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    private bool CanInteractWithBuildAreas()
    {
        if (TurnSourceResolver.IsAIPrototypeActive())
        {
            ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource();

            if (turnSource == null || !turnSource.CanHumanAct)
            {
                ShowToast("You cannot build during enemy wave.");
                HideBuildInteractionUI();
                return false;
            }
        }
        else if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast("You cannot build during enemy wave.");
            HideBuildInteractionUI();
            return false;
        }

        return true;
    }

    private bool CanPerformTowerOrLandAction()
    {
        if (!CanInteractWithBuildAreas())
        {
            return false;
        }

        return true;
    }

    private bool ShouldBlockTutorialBuildAreaClick(TowerBuildArea buildArea)
    {
        if (buildArea == null)
        {
            return false;
        }

        TutorialActionType actionType;

        if (buildArea.isOccupied)
        {
            actionType = TutorialActionType.InspectTower;
        }
        else if (buildArea.IsClaimable() && buildArea.IsUnowned())
        {
            actionType = TutorialActionType.SelectLand;
        }
        else
        {
            actionType = TutorialActionType.SelectOwnedLand;
        }

        if (TutorialActionGate.BlockIfNotAllowed(actionType, buildArea.gameObject))
        {
            return true;
        }

        TutorialManager.Instance?.NotifyBuildAreaSelected(actionType, buildArea.gameObject);
        return false;
    }

    private GameObject GetPlayerManagerTowerPrefab(int playerId)
    {
        if (playerManager != null)
        {
            GameObject playerTowerPrefab = playerManager.GetTowerPrefabForPlayer(playerId);

            if (playerTowerPrefab != null)
            {
                return playerTowerPrefab;
            }
        }

        return null;
    }

    private GameObject ResolveTowerPrefab(TowerType towerType, int playerId)
    {
        GameObject typedPlayerPrefab = GetPlayerTypedTowerPrefab(towerType, playerId);

        if (typedPlayerPrefab != null)
        {
            return typedPlayerPrefab;
        }

        // Current art setup stores the four colored CannonTower prefabs on
        // PlayerManager. Use that as the color fallback for every tower type.
        GameObject visualConfigPrefab = GetPlayerManagerTowerPrefab(playerId);

        if (visualConfigPrefab != null)
        {
            return visualConfigPrefab;
        }

        GameObject defaultPrefab = GetDefaultTowerPrefab(towerType);

        if (defaultPrefab != null)
        {
            return defaultPrefab;
        }

        return cannonTowerPrefab;
    }

    private bool HasPlayerSpecificTowerPrefab(int playerId)
    {
        return playerManager != null && playerManager.GetTowerPrefabForPlayer(playerId) != null;
    }

    private bool HasPlayerSpecificTowerPrefab(TowerType towerType, int playerId)
    {
        return GetPlayerTypedTowerPrefab(towerType, playerId) != null || HasPlayerSpecificTowerPrefab(playerId);
    }

    private GameObject GetPlayerTypedTowerPrefab(TowerType towerType, int playerId)
    {
        GameObject[] prefabs = GetTypedPrefabArray(towerType);

        if (prefabs == null || playerId < 0 || playerId >= prefabs.Length)
        {
            return null;
        }

        return prefabs[playerId];
    }

    private GameObject[] GetTypedPrefabArray(TowerType towerType)
    {
        if (towerType == TowerType.Archer) return archerPrefabsByPlayerId;
        if (towerType == TowerType.Frost) return frostPrefabsByPlayerId;
        if (towerType == TowerType.Shock) return shockPrefabsByPlayerId;
        if (towerType == TowerType.Sniper) return sniperPrefabsByPlayerId;
        return cannonPrefabsByPlayerId;
    }

    private GameObject GetDefaultTowerPrefab(TowerType towerType)
    {
        if (towerType == TowerType.Archer) return defaultArcherPrefab;
        if (towerType == TowerType.Frost) return defaultFrostPrefab;
        if (towerType == TowerType.Shock) return defaultShockPrefab;
        if (towerType == TowerType.Sniper) return defaultSniperPrefab;
        return defaultCannonPrefab != null ? defaultCannonPrefab : cannonTowerPrefab;
    }

    private void ApplyOwnerVisualToGenericTower(GameObject tower, int playerId)
    {
        if (tower == null || playerManager == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tower.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        Sprite towerSprite = playerManager.GetTowerSpriteForPlayer(playerId);

        if (towerSprite != null)
        {
            spriteRenderer.sprite = towerSprite;
        }

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
    }

    private void ForceTowerAlphaOpaque(GameObject tower)
    {
        if (tower == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tower.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
    }

    private void EnsureTowerCollider(GameObject tower)
    {
        if (tower == null)
        {
            return;
        }

        Collider2D collider = tower.GetComponentInChildren<Collider2D>();

        if (collider != null)
        {
            return;
        }

        BoxCollider2D boxCollider = tower.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void RefreshCurrentPlayerUI()
    {
        if (playerManager != null)
        {
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    private void ApplySyncedPlayerGold(PlayerResource playerResource, int newGold)
    {
        if (playerResource == null || newGold < 0)
        {
            return;
        }

        playerResource.money = newGold;
        playerResource.RefreshUI();
    }

    private void FinalizeSyncedAreaChange(TowerBuildArea buildArea)
    {
        if (buildArea != null)
        {
            buildArea.RefreshOwnershipVisual(playerManager);
        }

        if (radialTowerMenu != null && radialTowerMenu.IsOpen)
        {
            radialTowerMenu.Hide();
        }

        if (playerManager != null)
        {
            playerManager.RefreshAllPlayerStatusPanels();
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    private void ShowToast(string message)
    {
        bool shown = TryCallToastMethod(message);

        if (!shown)
        {
            ShowToastObject(message);
        }

        Debug.Log(message);
    }

    private bool TryCallToastMethod(string message)
    {
        if (toastMessage == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "ShowMessage",
            "Show",
            "ShowToast",
            "Display",
            "ShowWarningMessage"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = toastMessage.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null
            );

            if (method == null)
            {
                continue;
            }

            method.Invoke(toastMessage, new object[] { message });
            return true;
        }

        return false;
    }

    private void ShowToastObject(string message)
    {
        if (toastMessageObject == null && toastText == null)
        {
            return;
        }

        if (toastMessageObject == null && toastText != null)
        {
            toastMessageObject = toastText.transform.parent != null
                ? toastText.transform.parent.gameObject
                : toastText.gameObject;
        }

        if (toastText == null && toastMessageObject != null)
        {
            toastText = toastMessageObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (toastText != null)
        {
            toastText.text = message;
            toastText.fontSize = GetToastFontSize(message);
        }

        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(true);
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(HideToastAfterDelay());
    }

    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSeconds(toastDuration);

        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(false);
        }
    }

    private float GetToastFontSize(string message)
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
}
