/*
 * CanalTD - BuildTowerManager
 *
 * Purpose:
 * Coordinates land purchase, tower construction, tower upgrade, tower selling, and build feedback.
 *
 * Attached GameObject:
 * Configured in the Unity scene / Inspector with build areas, tower prefabs, UI, and resource references.
 *
 * Main responsibilities:
 * - Validate build-area ownership and resource costs.
 * - Build, upgrade, and sell towers through the selected build area.
 * - Coordinate tower UI, radial menu behavior, feedback messages, and tutorial gates.
 * - Send online build synchronization requests when online mode is active.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Build-area clicks, tower selections, resource state, ownership state, and tutorial/online permissions.
 *
 * Outputs / effects:
 * - Creates or removes tower objects, updates resources, changes ownership state, and displays build feedback.
 *
 * Authorship / assistance:
 * Game design, Unity implementation, integration, and final documentation were developed by Jingyu Pan
 * for an individual coursework submission. AI assistance was used as disclosed in the project documentation.
 *
 * Testing notes:
 * - Manually verify land purchase, ownership checks, build, upgrade, sell, insufficient-resource feedback, and tutorial-gated build steps.
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

    /// <summary>
    /// Finds and stores build tower manager references before scene gameplay begins.
    /// </summary>
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

    /// <summary>
    /// Checks build tower manager input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBuildTowerAtMouse();
        }
    }

    /// <summary>
    /// Attempts to build tower at mouse and returns false if rules, resources, or references block it.
    /// </summary>
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
            /// <summary>
            /// Handles vector3 for build tower manager.
            /// </summary>
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

    /// <summary>
    /// Chooses whether the clicked land opens buy-land, build-tower, or manage-tower UI.
    /// </summary>
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

    /// <summary>
    /// Handles clicks on an occupied tile by checking ownership and opening tower management.
    /// </summary>
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

    /// <summary>
    /// Hides build interaction UI and clears temporary visual state.
    /// </summary>
    public void HideBuildInteractionUI()
    {
        if (radialTowerMenu != null)
        {
            radialTowerMenu.Hide();
        }
    }

    /// <summary>
    /// Attempts to buy land and returns false if rules, resources, or references block it.
    /// </summary>
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

        /// <summary>
        /// Attempts to buy land for player and reports whether it succeeded.
        /// </summary>
        return TryBuyLandForPlayer(buildArea, GetCurrentPlayerId(), GetCurrentPlayerResource(), true);
    }

    /// <summary>
    /// Attempts to buy land for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryBuyLandForPlayer(TowerBuildArea buildArea, int playerId)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        /// <summary>
        /// Attempts to buy land for player and reports whether it succeeded.
        /// </summary>
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

    /// <summary>
    /// Attempts to purchase land and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryPurchaseLand(TowerBuildArea buildArea)
    {
        /// <summary>
        /// Attempts to buy land and reports whether it succeeded.
        /// </summary>
        return TryBuyLand(buildArea);
    }

    /// <summary>
    /// Attempts to build tower and returns false if rules, resources, or references block it.
    /// </summary>
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

    /// <summary>
    /// Attempts to build tower from menu and returns false if rules, resources, or references block it.
    /// </summary>
    public void TryBuildTowerFromMenu(TowerBuildArea buildArea, TowerType towerType)
    {
        TryBuildTower(buildArea, towerType);
    }

    /// <summary>
    /// Attempts to upgrade tower from menu and returns false if rules, resources, or references block it.
    /// </summary>
    public void TryUpgradeTowerFromMenu(TowerBuildArea buildArea)
    {
        TryUpgradeTower(buildArea);
    }

    /// <summary>
    /// Attempts to sell tower from menu and returns false if rules, resources, or references block it.
    /// </summary>
    public void TrySellTowerFromMenu(TowerBuildArea buildArea)
    {
        TrySellTower(buildArea);
    }

    /// <summary>
    /// Attempts to build tower and returns false if rules, resources, or references block it.
    /// </summary>
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

        /// <summary>
        /// Attempts to build tower for player and reports whether it succeeded.
        /// </summary>
        return TryBuildTowerForPlayer(buildArea, towerType, GetCurrentPlayerId(), true);
    }

    /// <summary>
    /// Attempts to build tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryBuildTowerForPlayer(TowerBuildArea buildArea, TowerType towerType, int playerId)
    {
        /// <summary>
        /// Attempts to build tower for player and reports whether it succeeded.
        /// </summary>
        return TryBuildTowerForPlayer(buildArea, towerType, playerId, false);
    }

    /// <summary>
    /// Attempts to build tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryBuildTowerForPlayer(TowerBuildArea buildArea, TowerType towerType, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        /// <summary>
        /// Attempts to build tower and reports whether it succeeded.
        /// </summary>
        return TryBuildTower(buildArea, towerType, playerId, playerResource, showMessages);
    }

    /// <summary>
    /// Confirms buy land and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmBuyLand(TowerBuildArea buildArea)
    {
        /// <summary>
        /// Attempts to buy land and reports whether it succeeded.
        /// </summary>
        return TryBuyLand(buildArea);
    }

    /// <summary>
    /// Confirms build tower and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmBuildTower(TowerBuildArea buildArea, TowerType towerType)
    {
        /// <summary>
        /// Attempts to build tower and reports whether it succeeded.
        /// </summary>
        return TryBuildTower(buildArea, towerType);
    }

    /// <summary>
    /// Confirms upgrade tower and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmUpgradeTower(TowerBuildArea buildArea)
    {
        /// <summary>
        /// Attempts to upgrade tower and reports whether it succeeded.
        /// </summary>
        return TryUpgradeTower(buildArea);
    }

    /// <summary>
    /// Confirms sell tower and applies the selected action if it is valid.
    /// </summary>
    public bool ConfirmSellTower(TowerBuildArea buildArea)
    {
        /// <summary>
        /// Attempts to sell tower and reports whether it succeeded.
        /// </summary>
        return TrySellTower(buildArea);
    }

    /// <summary>
    /// Attempts to upgrade tower and returns false if rules, resources, or references block it.
    /// </summary>
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

        /// <summary>
        /// Attempts to upgrade tower for player and reports whether it succeeded.
        /// </summary>
        return TryUpgradeTowerForPlayer(buildArea, GetCurrentPlayerId(), true);
    }

    /// <summary>
    /// Attempts to upgrade tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryUpgradeTowerForPlayer(TowerBuildArea buildArea, int playerId)
    {
        /// <summary>
        /// Attempts to upgrade tower for player and reports whether it succeeded.
        /// </summary>
        return TryUpgradeTowerForPlayer(buildArea, playerId, false);
    }

    /// <summary>
    /// Attempts to upgrade tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryUpgradeTowerForPlayer(TowerBuildArea buildArea, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        /// <summary>
        /// Attempts to upgrade tower and reports whether it succeeded.
        /// </summary>
        return TryUpgradeTower(buildArea, playerId, playerResource, showMessages);
    }

    /// <summary>
    /// Attempts to sell tower and returns false if rules, resources, or references block it.
    /// </summary>
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

        /// <summary>
        /// Attempts to sell tower for player and reports whether it succeeded.
        /// </summary>
        return TrySellTowerForPlayer(buildArea, GetCurrentPlayerId(), true);
    }

    /// <summary>
    /// Attempts to sell tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TrySellTowerForPlayer(TowerBuildArea buildArea, int playerId)
    {
        /// <summary>
        /// Attempts to sell tower for player and reports whether it succeeded.
        /// </summary>
        return TrySellTowerForPlayer(buildArea, playerId, false);
    }

    /// <summary>
    /// Attempts to sell tower for player and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TrySellTowerForPlayer(TowerBuildArea buildArea, int playerId, bool showMessages)
    {
        PlayerResource playerResource = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        /// <summary>
        /// Attempts to sell tower and reports whether it succeeded.
        /// </summary>
        return TrySellTower(buildArea, playerId, playerResource, showMessages);
    }

    /// <summary>
    /// Attempts to build tower for AI and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryBuildTowerForAI(TowerBuildArea buildArea, TowerType type, int playerId)
    {
        /// <summary>
        /// Attempts to build tower for player and reports whether it succeeded.
        /// </summary>
        return TryBuildTowerForPlayer(buildArea, type, playerId, false);
    }

    /// <summary>
    /// Attempts to upgrade tower for AI and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryUpgradeTowerForAI(TowerBuildArea buildArea, int playerId)
    {
        /// <summary>
        /// Attempts to upgrade tower for player and reports whether it succeeded.
        /// </summary>
        return TryUpgradeTowerForPlayer(buildArea, playerId, false);
    }

    /// <summary>
    /// Attempts to sell tower for AI and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TrySellTowerForAI(TowerBuildArea buildArea, int playerId)
    {
        /// <summary>
        /// Attempts to sell tower for player and reports whether it succeeded.
        /// </summary>
        return TrySellTowerForPlayer(buildArea, playerId, false);
    }

    /// <summary>
    /// Returns tower cost used by land, tower, cost, or build decisions.
    /// </summary>
    public int GetTowerCost(TowerType towerType)
    {
        /// <summary>
        /// Returns tower cost needed by this gameplay system.
        /// </summary>
        return GetTowerCost(towerType, GetCurrentPlayerId());
    }

    // Used by RadialTowerMenu previews so the ghost tower matches the real build prefab.
    /// <summary>
    /// Returns tower preview prefab used by land, tower, cost, or build decisions.
    /// </summary>
    public GameObject GetTowerPreviewPrefab(TowerType towerType)
    {
        /// <summary>
        /// Handles resolve tower prefab for build tower manager.
        /// </summary>
        return ResolveTowerPrefab(towerType, GetCurrentPlayerId());
    }

    /// <summary>
    /// Returns tower cost used by land, tower, cost, or build decisions.
    /// </summary>
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

    /// <summary>
    /// Returns resolved tower prefab used by land, tower, cost, or build decisions.
    /// </summary>
    public GameObject GetResolvedTowerPrefab(TowerType towerType, int playerId)
    {
        /// <summary>
        /// Handles resolve tower prefab for build tower manager.
        /// </summary>
        return ResolveTowerPrefab(towerType, playerId);
    }

    /// <summary>
    /// Looks up the target for build area by stable ID and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Applies online buy land to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Applies online build tower to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Applies online upgrade tower to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Applies online sell tower to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Ensures online tower state exists or is initialized before the flow continues.
    /// </summary>
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

    /// <summary>
    /// Shows build menu or fallback with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Returns area menu position used by land, tower, cost, or build decisions.
    /// </summary>
    private Vector3 GetAreaMenuPosition(TowerBuildArea buildArea)
    {
        if (buildArea == null)
        {
            return Vector3.zero;
        }

        Transform spawnPoint = buildArea.towerSpawnPoint != null ? buildArea.towerSpawnPoint : buildArea.transform;
        return spawnPoint.position;
    }

    /// <summary>
    /// Returns tower stats from area used by land, tower, cost, or build decisions.
    /// </summary>
    private TowerStats GetTowerStatsFromArea(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.currentTower == null)
        {
            return null;
        }

        TowerStats stats = buildArea.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : buildArea.currentTower.GetComponentInChildren<TowerStats>();
    }

    /// <summary>
    /// Ensures tower stats exists or is initialized before the flow continues.
    /// </summary>
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

    /// <summary>
    /// Returns current player ID used by land, tower, cost, or build decisions.
    /// </summary>
    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : currentPlayerId;
    }

    /// <summary>
    /// Returns current player resource used by land, tower, cost, or build decisions.
    /// </summary>
    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : currentPlayerResource;
    }

    // Phase 2B is still local-only gameplay execution, so non-turn online
    // clients must be blocked at the shared build/tower entry points.
    /// <summary>
    /// Decides whether should block online action should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    /// <summary>
    /// Checks whether interact with build areas is allowed before enabling that action.
    /// </summary>
    private bool CanInteractWithBuildAreas()
    {
        if (TurnSourceResolver.IsAIPrototypeActive())
        {
            ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource();

            if (turnSource == null || !turnSource.CanHumanAct)
            {
                ShowToast(TutorialManager.Instance != null
                    ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot build during enemy wave.")
                    : "You cannot build during enemy wave.");
                HideBuildInteractionUI();
                return false;
            }
        }
        else if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot build during enemy wave.")
                : "You cannot build during enemy wave.");
            HideBuildInteractionUI();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether perform tower or land action is allowed before enabling that action.
    /// </summary>
    private bool CanPerformTowerOrLandAction()
    {
        if (!CanInteractWithBuildAreas())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Decides whether should block tutorial build area click should happen in the current mode and turn state.
    /// </summary>
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

    /// <summary>
    /// Returns player manager tower prefab used by land, tower, cost, or build decisions.
    /// </summary>
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

    /// <summary>
    /// Looks up the target for tower prefab and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Checks whether player specific tower prefab is present before the code depends on it.
    /// </summary>
    private bool HasPlayerSpecificTowerPrefab(int playerId)
    {
        return playerManager != null && playerManager.GetTowerPrefabForPlayer(playerId) != null;
    }

    /// <summary>
    /// Checks whether player specific tower prefab is present before the code depends on it.
    /// </summary>
    private bool HasPlayerSpecificTowerPrefab(TowerType towerType, int playerId)
    {
        /// <summary>
        /// Returns player typed tower prefab needed by this gameplay system.
        /// </summary>
        return GetPlayerTypedTowerPrefab(towerType, playerId) != null || HasPlayerSpecificTowerPrefab(playerId);
    }

    /// <summary>
    /// Returns player typed tower prefab used by land, tower, cost, or build decisions.
    /// </summary>
    private GameObject GetPlayerTypedTowerPrefab(TowerType towerType, int playerId)
    {
        GameObject[] prefabs = GetTypedPrefabArray(towerType);

        if (prefabs == null || playerId < 0 || playerId >= prefabs.Length)
        {
            return null;
        }

        return prefabs[playerId];
    }

    /// <summary>
    /// Returns typed prefab array used by land, tower, cost, or build decisions.
    /// </summary>
    private GameObject[] GetTypedPrefabArray(TowerType towerType)
    {
        if (towerType == TowerType.Archer) return archerPrefabsByPlayerId;
        if (towerType == TowerType.Frost) return frostPrefabsByPlayerId;
        if (towerType == TowerType.Shock) return shockPrefabsByPlayerId;
        if (towerType == TowerType.Sniper) return sniperPrefabsByPlayerId;
        return cannonPrefabsByPlayerId;
    }

    /// <summary>
    /// Returns default tower prefab used by land, tower, cost, or build decisions.
    /// </summary>
    private GameObject GetDefaultTowerPrefab(TowerType towerType)
    {
        if (towerType == TowerType.Archer) return defaultArcherPrefab;
        if (towerType == TowerType.Frost) return defaultFrostPrefab;
        if (towerType == TowerType.Shock) return defaultShockPrefab;
        if (towerType == TowerType.Sniper) return defaultSniperPrefab;
        return defaultCannonPrefab != null ? defaultCannonPrefab : cannonTowerPrefab;
    }

    /// <summary>
    /// Applies owner visual to generic tower to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Handles force tower alpha opaque for land ownership, tower actions, or build UI.
    /// </summary>
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

    /// <summary>
    /// Ensures tower collider exists or is initialized before the flow continues.
    /// </summary>
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

    /// <summary>
    /// Refreshes current player UI from the latest gameplay data.
    /// </summary>
    private void RefreshCurrentPlayerUI()
    {
        if (playerManager != null)
        {
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    /// <summary>
    /// Applies synced player gold to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplySyncedPlayerGold(PlayerResource playerResource, int newGold)
    {
        if (playerResource == null || newGold < 0)
        {
            return;
        }

        playerResource.money = newGold;
        playerResource.RefreshUI();
    }

    /// <summary>
    /// Handles finalize synced area change for land ownership, tower actions, or build UI.
    /// </summary>
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

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    private void ShowToast(string message)
    {
        bool shown = TryCallToastMethod(message);

        if (!shown)
        {
            ShowToastObject(message);
        }

        Debug.Log(message);
    }

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
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

    /// <summary>
    /// Shows toast object with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Hides toast after delay and clears temporary visual state.
    /// </summary>
    private IEnumerator HideToastAfterDelay()
    {
        /// <summary>
        /// Handles wait for seconds for build tower manager.
        /// </summary>
        yield return new WaitForSeconds(toastDuration);

        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(false);
        }
    }

    /// <summary>
    /// Returns toast font size used by land, tower, cost, or build decisions.
    /// </summary>
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
