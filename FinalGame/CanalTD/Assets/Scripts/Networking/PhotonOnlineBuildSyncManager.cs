/*
 * File: PhotonOnlineBuildSyncManager.cs
 *
 * Purpose:
 * Implements PhotonOnlineBuildSyncManager for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Online scene managers or networking helper objects used during lobby and match flow.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PhotonOnlineBuildSyncManager within the networking system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Keep multiplayer state aligned while respecting online lifecycle guards and scene context.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Photon room/player state, network events, and authoritative sync payloads when online mode is active.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Sends, applies, or guards online sync operations without changing project-level Photon settings.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PhotonOnlineBuildSyncManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System.Collections.Generic;
using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
#endif

#if PHOTON_UNITY_NETWORKING
public class PhotonOnlineBuildSyncManager : MonoBehaviourPunCallbacks, IOnEventCallback
#else
public class PhotonOnlineBuildSyncManager : MonoBehaviour
#endif
{
#if PHOTON_UNITY_NETWORKING
    private const byte BuildSyncRequestEventCode = 11;
    private const byte BuildSyncApplyEventCode = 12;
#endif

    public static PhotonOnlineBuildSyncManager Instance { get; private set; }

    [Header("Managers")]
    public PlayerManager playerManager;
    public BuildTowerManager buildTowerManager;
    public PhotonOnlineGameSceneManager onlineGameSceneManager;
    public RadialTowerMenu radialTowerMenu;

    private readonly Dictionary<string, TowerBuildArea> buildAreasById = new Dictionary<string, TowerBuildArea>();
    private bool initializedForOnlineMatch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
        RebuildBuildAreaRegistry();
    }

    private void OnEnable()
    {
#if PHOTON_UNITY_NETWORKING
        PhotonNetwork.AddCallbackTarget(this);
#endif
    }

    private void OnDisable()
    {
#if PHOTON_UNITY_NETWORKING
        PhotonNetwork.RemoveCallbackTarget(this);
#endif

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void InitializeForOnlineMatch()
    {
        AutoAssignReferences();
        RebuildBuildAreaRegistry();
        initializedForOnlineMatch = true;

#if PHOTON_UNITY_NETWORKING
        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() ||
            PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureSnapshotInitialized();
        }
        else
        {
            ApplySnapshotFromRoomProperties();
        }
#endif
    }

    public void HandleRoomPropertiesUpdated()
    {
#if PHOTON_UNITY_NETWORKING
        if (!initializedForOnlineMatch || !PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return;
        }

        ApplySnapshotFromRoomProperties();
#endif
    }

    public bool RequestBuyLand(TowerBuildArea buildArea)
    {
        return RequestAction(OnlineBuildActionType.BuyLand, buildArea, TowerType.Cannon);
    }

    public bool RequestBuildTower(TowerBuildArea buildArea, TowerType towerType)
    {
        return RequestAction(OnlineBuildActionType.BuildTower, buildArea, towerType);
    }

    public bool RequestUpgradeTower(TowerBuildArea buildArea)
    {
        return RequestAction(OnlineBuildActionType.UpgradeTower, buildArea, TowerType.Cannon);
    }

    public bool RequestSellTower(TowerBuildArea buildArea)
    {
        return RequestAction(OnlineBuildActionType.SellTower, buildArea, TowerType.Cannon);
    }

    public void UpdateOnlineTileStateSnapshot(
        string buildAreaId,
        bool isOwned,
        int ownerPlayerId,
        bool isFrozen,
        int frozenByPlayerId,
        bool frozenUntilPlayerNextTurn,
        bool clearTowerState)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null || string.IsNullOrWhiteSpace(buildAreaId))
        {
            return;
        }

        OnlineBuildSnapshot snapshot = GetSnapshotFromRoomOrScene();
        OnlineBuildAreaState areaState = GetOrCreateAreaState(snapshot, buildAreaId);
        areaState.isOwned = isOwned;
        areaState.ownerPlayerId = ownerPlayerId;
        areaState.isFrozen = isFrozen;
        areaState.frozenByPlayerId = frozenByPlayerId;
        areaState.frozenUntilPlayerNextTurn = frozenUntilPlayerNextTurn;

        if (clearTowerState)
        {
            areaState.towerExists = false;
            areaState.towerOwnerPlayerId = -1;
            areaState.towerTypeId = -1;
            areaState.towerLevel = 0;
        }

        WriteSnapshotToRoom(snapshot);
#endif
    }

    public int ClearExpiredFreezeClaimsForPlayer(int playerId)
    {
        int clearedCount = 0;

#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null || playerId < 0)
        {
            return clearedCount;
        }

        AutoAssignReferences();
        RebuildBuildAreaRegistryIfNeeded();

        OnlineBuildSnapshot snapshot = GetSnapshotFromRoomOrScene();

        foreach (OnlineBuildAreaState areaState in snapshot.areas)
        {
            if (areaState == null ||
                !areaState.isFrozen ||
                !areaState.frozenUntilPlayerNextTurn ||
                areaState.frozenByPlayerId != playerId)
            {
                continue;
            }

            areaState.isFrozen = false;
            areaState.frozenByPlayerId = -1;
            areaState.frozenUntilPlayerNextTurn = false;

            TowerBuildArea buildArea = ResolveBuildAreaById(areaState.buildAreaId);

            if (buildArea != null)
            {
                buildArea.ClearFreeze();
            }

            clearedCount++;
        }

        if (clearedCount > 0)
        {
            WriteSnapshotToRoom(snapshot);
            Debug.Log("Cleared expired online Freeze Claim tiles for playerId=" + playerId + ", count=" + clearedCount);
        }
#endif

        return clearedCount;
    }

    public void OnEvent(EventData photonEvent)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return;
        }

        if (photonEvent.Code == BuildSyncRequestEventCode)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            string requestJson = photonEvent.CustomData as string;
            OnlineBuildRequestData request = string.IsNullOrWhiteSpace(requestJson)
                ? null
                : JsonUtility.FromJson<OnlineBuildRequestData>(requestJson);

            if (request == null)
            {
                return;
            }

            ProcessRequestAsMaster(request, photonEvent.Sender);
            return;
        }

        if (photonEvent.Code != BuildSyncApplyEventCode)
        {
            return;
        }

        string applyJson = photonEvent.CustomData as string;
        OnlineBuildApplyData applyData = string.IsNullOrWhiteSpace(applyJson)
            ? null
            : JsonUtility.FromJson<OnlineBuildApplyData>(applyJson);

        if (applyData == null)
        {
            return;
        }

        ApplyConfirmedAction(applyData);
#endif
    }

    private bool RequestAction(OnlineBuildActionType actionType, TowerBuildArea buildArea, TowerType towerType)
    {
#if !PHOTON_UNITY_NETWORKING
        return false;
#else
        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return false;
        }

        AutoAssignReferences();
        RebuildBuildAreaRegistryIfNeeded();

        if (onlineGameSceneManager == null || buildArea == null)
        {
            return false;
        }

        if (!OnlineTurnPermissionManager.CanLocalPlayerAct(true))
        {
            return false;
        }

        string buildAreaId = GetBuildAreaId(buildArea);
        OnlineBuildRequestData request = new OnlineBuildRequestData
        {
            actionType = actionType,
            actorPlayerId = onlineGameSceneManager.LocalPlayerId,
            currentTurnPlayerId = onlineGameSceneManager.CurrentTurnPlayerId,
            buildAreaId = buildAreaId,
            towerId = GetTowerId(buildAreaId),
            towerTypeId = actionType == OnlineBuildActionType.BuildTower ? (int)towerType : -1
        };

        Debug.Log(GetRequestLogPrefix(actionType) +
            " actorPlayerId=" + request.actorPlayerId +
            ", currentTurnPlayerId=" + request.currentTurnPlayerId +
            ", buildAreaId=" + request.buildAreaId +
            ", towerId=" + request.towerId);

        if (PhotonNetwork.IsMasterClient)
        {
            ProcessRequestAsMaster(request, PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1);
            return true;
        }

        string requestJson = JsonUtility.ToJson(request);
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(BuildSyncRequestEventCode, requestJson, eventOptions, sendOptions);
        return true;
#endif
    }

#if PHOTON_UNITY_NETWORKING
    private void ProcessRequestAsMaster(OnlineBuildRequestData request, int senderActorNumber)
    {
        AutoAssignReferences();
        RebuildBuildAreaRegistryIfNeeded();

        OnlineBuildApplyData applyData = ValidateRequest(request, senderActorNumber);

        if (!applyData.accepted)
        {
            if (senderActorNumber == (PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1))
            {
                ApplyConfirmedAction(applyData);
                return;
            }

            RaiseEventOptions rejectOptions = new RaiseEventOptions
            {
                TargetActors = new[] { senderActorNumber }
            };
            PhotonNetwork.RaiseEvent(
                BuildSyncApplyEventCode,
                JsonUtility.ToJson(applyData),
                rejectOptions,
                new SendOptions { Reliability = true }
            );
            return;
        }

        UpdateSnapshotProperty(applyData);

        RaiseEventOptions applyOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(
            BuildSyncApplyEventCode,
            JsonUtility.ToJson(applyData),
            applyOptions,
            new SendOptions { Reliability = true }
        );
    }

    private OnlineBuildApplyData ValidateRequest(OnlineBuildRequestData request, int senderActorNumber)
    {
        OnlineBuildApplyData result = new OnlineBuildApplyData
        {
            actionType = request.actionType,
            actorPlayerId = request.actorPlayerId,
            currentTurnPlayerId = onlineGameSceneManager != null ? onlineGameSceneManager.CurrentTurnPlayerId : -1,
            buildAreaId = request.buildAreaId,
            towerId = request.towerId,
            towerTypeId = request.towerTypeId,
            ownerPlayerId = request.actorPlayerId,
            accepted = false
        };

        int resolvedSenderPlayerId = onlineGameSceneManager != null
            ? onlineGameSceneManager.GetOnlinePlayerIdByActorNumber(senderActorNumber)
            : -1;

        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            result.rejectReason = "Online game inactive.";
            LogValidation(result);
            return result;
        }

        if (resolvedSenderPlayerId != request.actorPlayerId)
        {
            result.rejectReason = "Sender mismatch.";
            LogValidation(result);
            return result;
        }

        if (onlineGameSceneManager == null ||
            onlineGameSceneManager.CurrentTurnPlayerId != request.actorPlayerId)
        {
            result.rejectReason = "Not current turn player.";
            LogValidation(result);
            return result;
        }

        TowerBuildArea buildArea = ResolveBuildAreaById(request.buildAreaId);

        if (buildArea == null)
        {
            result.rejectReason = "Build area missing.";
            LogValidation(result);
            return result;
        }

        PlayerResource actor = playerManager != null ? playerManager.GetPlayerResource(request.actorPlayerId) : null;

        if (actor == null)
        {
            result.rejectReason = "Actor missing.";
            LogValidation(result);
            return result;
        }

        switch (request.actionType)
        {
            case OnlineBuildActionType.BuyLand:
                ValidateBuyLand(buildArea, actor, result);
                break;
            case OnlineBuildActionType.BuildTower:
                ValidateBuildTower(buildArea, actor, request, result);
                break;
            case OnlineBuildActionType.UpgradeTower:
                ValidateUpgradeTower(buildArea, actor, result);
                break;
            case OnlineBuildActionType.SellTower:
                ValidateSellTower(buildArea, actor, result);
                break;
        }

        LogValidation(result);
        return result;
    }
#endif

    private void ValidateBuyLand(TowerBuildArea buildArea, PlayerResource actor, OnlineBuildApplyData result)
    {
        if (buildArea.IsFrozen())
        {
            result.rejectReason = "Land is frozen.";
            return;
        }

        if (!buildArea.IsClaimable() || !buildArea.IsUnowned())
        {
            result.rejectReason = "Land cannot be purchased.";
            return;
        }

        if (!actor.CanAfford(buildArea.landPurchaseCost))
        {
            result.rejectReason = "Not enough gold.";
            return;
        }

        result.accepted = true;
        result.ownerPlayerId = actor.playerId;
        result.newGold = actor.money - buildArea.landPurchaseCost;
    }

    private void ValidateBuildTower(TowerBuildArea buildArea, PlayerResource actor, OnlineBuildRequestData request, OnlineBuildApplyData result)
    {
        if (!System.Enum.IsDefined(typeof(TowerType), request.towerTypeId))
        {
            result.rejectReason = "Invalid tower type.";
            return;
        }

        if (buildArea.IsFrozen())
        {
            result.rejectReason = "Land is frozen.";
            return;
        }

        if (buildArea.isOccupied)
        {
            result.rejectReason = "Tower already exists.";
            return;
        }

        int actorPlayerId = actor.playerId;

        if (!buildArea.CanBuildTower(actorPlayerId))
        {
            result.rejectReason = "Build permission denied.";
            return;
        }

        TowerType towerType = (TowerType)request.towerTypeId;
        int towerCost = buildTowerManager != null ? buildTowerManager.GetTowerCost(towerType, actorPlayerId) : 0;

        if (!actor.CanAfford(towerCost))
        {
            result.rejectReason = "Not enough gold.";
            return;
        }

        result.accepted = true;
        result.ownerPlayerId = actorPlayerId;
        result.newGold = actor.money - towerCost;
        result.level = 1;
    }

    private void ValidateUpgradeTower(TowerBuildArea buildArea, PlayerResource actor, OnlineBuildApplyData result)
    {
        TowerStats stats = GetTowerStats(buildArea);

        if (stats == null)
        {
            result.rejectReason = "Tower missing.";
            return;
        }

        if (buildArea.towerOwnerPlayerId != actor.playerId)
        {
            result.rejectReason = "Tower belongs to another player.";
            return;
        }

        if (!stats.CanUpgrade())
        {
            result.rejectReason = "Tower already max level.";
            return;
        }

        int upgradeCost = stats.GetUpgradeCost();

        if (!actor.CanAfford(upgradeCost))
        {
            result.rejectReason = "Not enough gold.";
            return;
        }

        result.accepted = true;
        result.ownerPlayerId = actor.playerId;
        result.newGold = actor.money - upgradeCost;
        result.towerTypeId = (int)stats.towerType;
        result.level = stats.level + 1;
    }

    private void ValidateSellTower(TowerBuildArea buildArea, PlayerResource actor, OnlineBuildApplyData result)
    {
        TowerStats stats = GetTowerStats(buildArea);

        if (buildArea.currentTower == null || stats == null)
        {
            result.rejectReason = "No tower to sell.";
            return;
        }

        if (buildArea.towerOwnerPlayerId != actor.playerId)
        {
            result.rejectReason = "Tower belongs to another player.";
            return;
        }

        result.accepted = true;
        result.ownerPlayerId = actor.playerId;
        result.newGold = actor.money + stats.GetSellValue();
        result.towerTypeId = (int)stats.towerType;
        result.level = 0;
    }

    private void ApplyConfirmedAction(OnlineBuildApplyData applyData)
    {
        AutoAssignReferences();
        RebuildBuildAreaRegistryIfNeeded();

        if (!applyData.accepted)
        {
            Debug.Log(GetApplyLogPrefix(applyData.actionType) +
                " actorPlayerId=" + applyData.actorPlayerId +
                ", currentTurnPlayerId=" + applyData.currentTurnPlayerId +
                ", buildAreaId=" + applyData.buildAreaId +
                ", towerId=" + applyData.towerId +
                ", accepted=false, reason=" + applyData.rejectReason);

            if (!string.IsNullOrWhiteSpace(applyData.rejectReason) && onlineGameSceneManager != null)
            {
                onlineGameSceneManager.ShowOnlineToast(applyData.rejectReason);
            }

            return;
        }

        bool applied = false;

        switch (applyData.actionType)
        {
            case OnlineBuildActionType.BuyLand:
                applied = buildTowerManager != null &&
                    buildTowerManager.ApplyOnlineBuyLand(applyData.buildAreaId, applyData.ownerPlayerId, applyData.newGold);
                break;
            case OnlineBuildActionType.BuildTower:
                applied = buildTowerManager != null &&
                    buildTowerManager.ApplyOnlineBuildTower(
                        applyData.buildAreaId,
                        applyData.towerId,
                        (TowerType)applyData.towerTypeId,
                        applyData.ownerPlayerId,
                        applyData.level,
                        applyData.newGold
                    );
                break;
            case OnlineBuildActionType.UpgradeTower:
                applied = buildTowerManager != null &&
                    buildTowerManager.ApplyOnlineUpgradeTower(
                        applyData.buildAreaId,
                        applyData.towerId,
                        applyData.ownerPlayerId,
                        applyData.level,
                        applyData.newGold
                    );
                break;
            case OnlineBuildActionType.SellTower:
                applied = buildTowerManager != null &&
                    buildTowerManager.ApplyOnlineSellTower(
                        applyData.buildAreaId,
                        applyData.towerId,
                        applyData.ownerPlayerId,
                        applyData.newGold
                    );
                break;
        }

        Debug.Log(GetApplyLogPrefix(applyData.actionType) +
            " actorPlayerId=" + applyData.actorPlayerId +
            ", currentTurnPlayerId=" + applyData.currentTurnPlayerId +
            ", buildAreaId=" + applyData.buildAreaId +
            ", towerId=" + applyData.towerId +
            ", accepted=" + applied +
            ", reason=" + (applied ? "Applied" : "Apply failed"));

        if (applied)
        {
            ShowAppliedActionToast(applyData);
        }
    }

#if PHOTON_UNITY_NETWORKING
    private void EnsureSnapshotInitialized()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.OnlineBuildSnapshot))
        {
            return;
        }

        WriteSnapshotToRoom(CreateSnapshotFromScene());
    }

    private void ApplySnapshotFromRoomProperties()
    {
        if (PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.CurrentRoom.CustomProperties == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.OnlineBuildSnapshot))
        {
            return;
        }

        object value = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.OnlineBuildSnapshot];
        string snapshotJson = value as string;

        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return;
        }

        OnlineBuildSnapshot snapshot = JsonUtility.FromJson<OnlineBuildSnapshot>(snapshotJson);

        if (snapshot == null || snapshot.areas == null)
        {
            return;
        }

        ApplySnapshot(snapshot);
    }

    private void UpdateSnapshotProperty(OnlineBuildApplyData applyData)
    {
        OnlineBuildSnapshot snapshot = GetSnapshotFromRoomOrScene();
        OnlineBuildAreaState areaState = GetOrCreateAreaState(snapshot, applyData.buildAreaId);

        switch (applyData.actionType)
        {
            case OnlineBuildActionType.BuyLand:
                areaState.isOwned = true;
                areaState.ownerPlayerId = applyData.ownerPlayerId;
                areaState.isFrozen = false;
                areaState.frozenByPlayerId = -1;
                areaState.frozenUntilPlayerNextTurn = false;
                break;
            case OnlineBuildActionType.BuildTower:
                areaState.towerExists = true;
                areaState.towerOwnerPlayerId = applyData.ownerPlayerId;
                areaState.towerTypeId = applyData.towerTypeId;
                areaState.towerLevel = applyData.level;
                break;
            case OnlineBuildActionType.UpgradeTower:
                areaState.towerExists = true;
                areaState.towerOwnerPlayerId = applyData.ownerPlayerId;
                areaState.towerLevel = applyData.level;
                break;
            case OnlineBuildActionType.SellTower:
                areaState.towerExists = false;
                areaState.towerOwnerPlayerId = -1;
                areaState.towerTypeId = -1;
                areaState.towerLevel = 0;
                break;
        }

        WriteSnapshotToRoom(snapshot);
    }

    private OnlineBuildSnapshot GetSnapshotFromRoomOrScene()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.OnlineBuildSnapshot))
        {
            string snapshotJson = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.OnlineBuildSnapshot] as string;

            if (!string.IsNullOrWhiteSpace(snapshotJson))
            {
                OnlineBuildSnapshot snapshot = JsonUtility.FromJson<OnlineBuildSnapshot>(snapshotJson);

                if (snapshot != null && snapshot.areas != null)
                {
                    return snapshot;
                }
            }
        }

        return CreateSnapshotFromScene();
    }

    private void WriteSnapshotToRoom(OnlineBuildSnapshot snapshot)
    {
        if (PhotonNetwork.CurrentRoom == null || snapshot == null)
        {
            return;
        }

        PhotonHashtable properties = new PhotonHashtable
        {
            { PhotonLobbyPropertyKeys.OnlineBuildSnapshot, JsonUtility.ToJson(snapshot) }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }
#endif

    private OnlineBuildSnapshot CreateSnapshotFromScene()
    {
        RebuildBuildAreaRegistryIfNeeded();
        OnlineBuildSnapshot snapshot = new OnlineBuildSnapshot();

        foreach (KeyValuePair<string, TowerBuildArea> pair in buildAreasById)
        {
            TowerBuildArea buildArea = pair.Value;

            if (buildArea == null)
            {
                continue;
            }

            TowerStats stats = GetTowerStats(buildArea);
            snapshot.areas.Add(new OnlineBuildAreaState
            {
                buildAreaId = pair.Key,
                isOwned = buildArea.isOwned,
                ownerPlayerId = buildArea.ownerPlayerId,
                isFrozen = buildArea.isFrozenOrSealed,
                frozenByPlayerId = buildArea.frozenByPlayerId,
                frozenUntilPlayerNextTurn = buildArea.frozenUntilPlayerNextTurn,
                towerExists = buildArea.currentTower != null,
                towerOwnerPlayerId = buildArea.towerOwnerPlayerId,
                towerTypeId = stats != null ? (int)stats.towerType : -1,
                towerLevel = stats != null ? stats.level : 0
            });
        }

        return snapshot;
    }

    private void ApplySnapshot(OnlineBuildSnapshot snapshot)
    {
        if (snapshot == null || buildTowerManager == null)
        {
            return;
        }

        RebuildBuildAreaRegistryIfNeeded();

        foreach (OnlineBuildAreaState state in snapshot.areas)
        {
            TowerBuildArea buildArea = ResolveBuildAreaById(state.buildAreaId);

            if (buildArea == null)
            {
                continue;
            }

            if (state.isOwned && state.ownerPlayerId >= 0)
            {
                buildArea.SetOwner(state.ownerPlayerId, playerManager);
            }
            else
            {
                buildArea.ClearOwner();
            }

            if (state.isFrozen)
            {
                buildArea.FreezeForPlayer(state.frozenByPlayerId);
                buildArea.frozenUntilPlayerNextTurn = state.frozenUntilPlayerNextTurn;
            }
            else
            {
                buildArea.ClearFreeze();
            }

            if (!state.towerExists)
            {
                if (buildArea.currentTower != null)
                {
                    buildArea.RemoveCurrentTower();
                    buildArea.towerOwnerPlayerId = -1;
                }

                continue;
            }

            if (state.towerTypeId < 0)
            {
                continue;
            }

            buildTowerManager.ApplyOnlineBuildTower(
                state.buildAreaId,
                GetTowerId(state.buildAreaId),
                (TowerType)state.towerTypeId,
                state.towerOwnerPlayerId,
                state.towerLevel,
                -1
            );
        }

        if (playerManager != null)
        {
            playerManager.RefreshAllPlayerStatusPanels();
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    private OnlineBuildAreaState GetOrCreateAreaState(OnlineBuildSnapshot snapshot, string buildAreaId)
    {
        foreach (OnlineBuildAreaState area in snapshot.areas)
        {
            if (area != null && area.buildAreaId == buildAreaId)
            {
                return area;
            }
        }

        OnlineBuildAreaState created = new OnlineBuildAreaState { buildAreaId = buildAreaId };
        snapshot.areas.Add(created);
        return created;
    }

    private TowerBuildArea ResolveBuildAreaById(string buildAreaId)
    {
        RebuildBuildAreaRegistryIfNeeded();

        if (string.IsNullOrWhiteSpace(buildAreaId))
        {
            return null;
        }

        buildAreasById.TryGetValue(buildAreaId, out TowerBuildArea buildArea);
        return buildArea;
    }

    private void RebuildBuildAreaRegistryIfNeeded()
    {
        if (buildAreasById.Count == 0)
        {
            RebuildBuildAreaRegistry();
        }
    }

    private void RebuildBuildAreaRegistry()
    {
        buildAreasById.Clear();
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>(true);

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null || string.IsNullOrWhiteSpace(buildArea.name))
            {
                continue;
            }

            if (buildAreasById.ContainsKey(buildArea.name))
            {
                Debug.LogWarning("Duplicate online build area id found: " + buildArea.name);
                continue;
            }

            buildAreasById.Add(buildArea.name, buildArea);
        }
    }

    private string GetBuildAreaId(TowerBuildArea buildArea)
    {
        return buildArea != null ? buildArea.name : string.Empty;
    }

    private string GetTowerId(string buildAreaId)
    {
        return buildAreaId + "_tower";
    }

    private TowerStats GetTowerStats(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.currentTower == null)
        {
            return null;
        }

        TowerStats stats = buildArea.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : buildArea.currentTower.GetComponentInChildren<TowerStats>();
    }

    private void AutoAssignReferences()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (buildTowerManager == null)
        {
            buildTowerManager = FindObjectOfType<BuildTowerManager>();
        }

        if (onlineGameSceneManager == null)
        {
            onlineGameSceneManager = FindObjectOfType<PhotonOnlineGameSceneManager>();
        }

        if (radialTowerMenu == null && buildTowerManager != null)
        {
            radialTowerMenu = buildTowerManager.radialTowerMenu;
        }
    }

    private void LogValidation(OnlineBuildApplyData result)
    {
        Debug.Log(GetValidateLogPrefix(result.actionType) +
            " actorPlayerId=" + result.actorPlayerId +
            ", currentTurnPlayerId=" + result.currentTurnPlayerId +
            ", buildAreaId=" + result.buildAreaId +
            ", towerId=" + result.towerId +
            ", accepted=" + result.accepted +
            ", reason=" + (result.accepted ? "Accepted" : result.rejectReason));
    }

    private string GetRequestLogPrefix(OnlineBuildActionType actionType)
    {
        switch (actionType)
        {
            case OnlineBuildActionType.BuyLand: return "RequestBuyLand";
            case OnlineBuildActionType.BuildTower: return "RequestBuildTower";
            case OnlineBuildActionType.UpgradeTower: return "RequestUpgradeTower";
            case OnlineBuildActionType.SellTower: return "RequestSellTower";
        }

        return "RequestBuildAction";
    }

    private string GetValidateLogPrefix(OnlineBuildActionType actionType)
    {
        switch (actionType)
        {
            case OnlineBuildActionType.BuyLand: return "ValidateBuyLand";
            case OnlineBuildActionType.BuildTower: return "ValidateBuildTower";
            case OnlineBuildActionType.UpgradeTower: return "ValidateUpgradeTower";
            case OnlineBuildActionType.SellTower: return "ValidateSellTower";
        }

        return "ValidateBuildAction";
    }

    private string GetApplyLogPrefix(OnlineBuildActionType actionType)
    {
        switch (actionType)
        {
            case OnlineBuildActionType.BuyLand: return "ApplyBuyLand";
            case OnlineBuildActionType.BuildTower: return "ApplyBuildTower";
            case OnlineBuildActionType.UpgradeTower: return "ApplyUpgradeTower";
            case OnlineBuildActionType.SellTower: return "ApplySellTower";
        }

        return "ApplyBuildAction";
    }

    private void ShowAppliedActionToast(OnlineBuildApplyData applyData)
    {
        if (onlineGameSceneManager == null)
        {
            return;
        }

        bool isLocalActor = onlineGameSceneManager.LocalPlayerId >= 0 &&
            applyData.actorPlayerId == onlineGameSceneManager.LocalPlayerId;

        string actionText = GetToastActionText(applyData.actionType);

        if (string.IsNullOrWhiteSpace(actionText))
        {
            return;
        }

        if (isLocalActor)
        {
            onlineGameSceneManager.ShowOnlineToast("You " + actionText + ".");
            return;
        }

        string actorName = onlineGameSceneManager.GetOnlineTurnOwnerDisplayName(applyData.actorPlayerId);

        if (string.IsNullOrWhiteSpace(actorName))
        {
            actorName = "Player " + applyData.actorPlayerId;
        }

        onlineGameSceneManager.ShowOnlineToast(actorName + " " + actionText + ".");
    }

    private string GetToastActionText(OnlineBuildActionType actionType)
    {
        switch (actionType)
        {
            case OnlineBuildActionType.BuyLand:
                return "bought land";
            case OnlineBuildActionType.BuildTower:
                return "built a tower";
            case OnlineBuildActionType.UpgradeTower:
                return "upgraded a tower";
            case OnlineBuildActionType.SellTower:
                return "sold a tower";
        }

        return string.Empty;
    }
}
