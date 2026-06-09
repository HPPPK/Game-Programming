/*
 * File: PhotonOnlineGameSceneManager.cs
 *
 * Purpose:
 * Implements PhotonOnlineGameSceneManager for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Online scene managers or networking helper objects used during lobby and match flow.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PhotonOnlineGameSceneManager within the networking system.
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
 * - Verify PhotonOnlineGameSceneManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using Photon.Realtime;
#endif

#if PHOTON_UNITY_NETWORKING
public class PhotonOnlineGameSceneManager : MonoBehaviourPunCallbacks, IOnEventCallback
#else
public class PhotonOnlineGameSceneManager : MonoBehaviour
#endif
{
    private const string OnlinePhotonMode = "OnlinePhotonPUN2";

#if PHOTON_UNITY_NETWORKING
    private const byte EndTurnRequestEventCode = 1;
#endif

    public static PhotonOnlineGameSceneManager Instance { get; private set; }

    [Header("Managers")]
    public PlayerManager playerManager;
    public TurnManager turnManager;
    public GamePhaseManager gamePhaseManager;
    public MonoBehaviour toastMessage;
    public PhotonOnlineBuildSyncManager onlineBuildSyncManager;
    public PhotonOnlineCardSyncManager onlineCardSyncManager;
    public PhotonOnlineWaveCombatSyncManager onlineWaveCombatSyncManager;

    [Header("Online Turn Settings")]
    public int minOnlinePlayersToStart = 2;
    public string homeSceneName = "HomeScene";

    public bool IsOnlineModeActive { get; private set; }
    public int LocalActorNumber { get; private set; } = -1;
    public int LocalPlayerId { get; private set; } = -1;
    public int LocalSlotIndex { get; private set; } = -1;
    public string LocalPlayerName { get; private set; } = "Player";

    private bool bootstrapComplete;
    private bool hasAppliedTurnState;
    private int lastAppliedTurnPlayerId = -1;
    private int lastAppliedRound = -1;
    private bool isLocalLeavingMatch;
    private bool shouldLoadHomeOnLeftRoom;
    private bool hasDetachedFromOnlineMatch;
    private bool hasBootstrappedOnlineMatch;
    private bool photonCallbacksRegistered;

    public bool HasEverBootstrappedOnlineMatch
    {
        get { return hasBootstrappedOnlineMatch; }
    }

    public bool IsLocalLeavingMatch
    {
        get { return isLocalLeavingMatch; }
    }

    public bool HasAuthoritativeTurnStateReady
    {
        get { return HasLiveOnlineMatchSession() && !ShouldIgnoreOnlineCallbacks() && bootstrapComplete; }
    }

    public int CurrentTurnPlayerId
    {
        get { return GetRoomCurrentTurnPlayerId(); }
    }

    public int CurrentRound
    {
        get { return GetRoomCurrentRound(); }
    }

    public bool HasDetachedFromOnlineMatch
    {
        get { return hasDetachedFromOnlineMatch; }
    }

    public static bool IsOnlinePhotonGameSceneActive()
    {
#if PHOTON_UNITY_NETWORKING
        if (Instance != null && Instance.HasLiveOnlineMatchSession())
        {
            return true;
        }

        return PhotonNetwork.InRoom &&
            !PhotonNetwork.OfflineMode &&
            PlayerPrefs.GetString("GameMode", string.Empty) == OnlinePhotonMode;
#else
        return false;
#endif
    }

    public static bool IsLiveOnlineGameSceneContext()
    {
        return SceneManager.GetActiveScene().name == "GameScene" && IsOnlinePhotonGameSceneActive();
    }

#if PHOTON_UNITY_NETWORKING
    // Shared gameplay entry points use this to block local interaction while
    // online turn authority belongs to another player.
    public static bool ShouldBlockLocalGameplayAction(bool showToast = true)
    {
        return OnlineTurnPermissionManager.ShouldBlockLocalGameplayAction(showToast);
    }
#endif

    private void Awake()
    {
        if (!IsGameSceneObjectRuntime())
        {
            Debug.Log("PhotonOnlineGameSceneManager Awake ignored outside GameScene. scene=" + SceneManager.GetActiveScene().name);
            return;
        }

        Instance = this;

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        if (gamePhaseManager == null)
        {
            gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        }

        EnsureOnlineBuildSyncManagerExists();
        EnsureOnlineWaveCombatSyncManagerExists();
    }

    private void OnEnable()
    {
        if (!IsGameSceneObjectRuntime())
        {
            return;
        }

#if PHOTON_UNITY_NETWORKING
        RegisterPhotonCallbacksIfNeeded();
#endif
    }

    private void OnDisable()
    {
        UnregisterPhotonCallbacksIfNeeded();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (!IsGameSceneObjectRuntime())
        {
            return;
        }

#if PHOTON_UNITY_NETWORKING
        if (!ShouldUseOnlineMode())
        {
            return;
        }

        hasDetachedFromOnlineMatch = false;
        IsOnlineModeActive = true;
        StartCoroutine(BootstrapOnlineGameSceneNextFrame());
#endif
    }

#if PHOTON_UNITY_NETWORKING
    private IEnumerator BootstrapOnlineGameSceneNextFrame()
    {
        yield return null;
        BootstrapOnlineGameScene();
    }

    private bool ShouldUseOnlineMode()
    {
        return PhotonNetwork.InRoom &&
            !PhotonNetwork.OfflineMode &&
            PlayerPrefs.GetString("GameMode", string.Empty) == OnlinePhotonMode;
    }

    public void BootstrapOnlineGameScene()
    {
        if (!ShouldUseOnlineMode() || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        IsOnlineModeActive = true;
        hasBootstrappedOnlineMatch = true;
        Debug.Log("Online GameScene bootstrap started.");

        CacheLocalPhotonIdentity();
        RebuildOnlineRosterFromPhoton();

        EnsureOnlineCardSyncManagerExists();

        if (onlineCardSyncManager != null)
        {
            onlineCardSyncManager.InitializeForOnlineMatch();
        }

        EnsureOnlineWaveCombatSyncManagerExists();

        if (onlineWaveCombatSyncManager != null)
        {
            onlineWaveCombatSyncManager.InitializeForOnlineMatch();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureRoomTurnStateInitialized();
        }

        ApplyRoomTurnState(forceApply: true);
        if (onlineBuildSyncManager != null)
        {
            onlineBuildSyncManager.InitializeForOnlineMatch();
        }
        bootstrapComplete = true;
    }

    public bool IsLocalOnlinePlayer(int playerId)
    {
        return IsOnlineModeActive && playerId >= 0 && playerId == LocalPlayerId;
    }

    public bool HasLiveOnlineMatchSession()
    {
        return !hasDetachedFromOnlineMatch &&
            (IsOnlineModeActive ||
             hasBootstrappedOnlineMatch ||
             ShouldUseOnlineMode() ||
             (PhotonNetwork.InRoom && !PhotonNetwork.OfflineMode));
    }

    public bool WasOnlineMatchScene()
    {
        return hasBootstrappedOnlineMatch || isLocalLeavingMatch || hasDetachedFromOnlineMatch;
    }

    public void RequestLocalExitToHome()
    {
        if (hasDetachedFromOnlineMatch || isLocalLeavingMatch)
        {
            return;
        }

        Debug.Log("Leaving room");
        isLocalLeavingMatch = true;
        shouldLoadHomeOnLeftRoom = true;
        IsOnlineModeActive = false;
        bootstrapComplete = false;
        hasAppliedTurnState = false;

        if (!PhotonNetwork.InRoom)
        {
            DetachFromOnlineMatchState();
            Debug.Log("Loading HomeScene");
            SceneManager.LoadScene(homeSceneName);
            return;
        }

        PhotonNetwork.LeaveRoom();
    }

    // Returns whether this local client is allowed to perform gameplay actions
    // during the current synchronized online turn.
    public bool IsLocalPlayerAllowedToAct(bool showToast = true)
    {
        return OnlineTurnPermissionManager.CanLocalPlayerAct(showToast);
    }

    public void DetachFromOnlineMatchState()
    {
        hasDetachedFromOnlineMatch = true;
        isLocalLeavingMatch = false;
        shouldLoadHomeOnLeftRoom = false;
        IsOnlineModeActive = false;
        bootstrapComplete = false;
        hasAppliedTurnState = false;
        lastAppliedTurnPlayerId = -1;
        lastAppliedRound = -1;
        LocalActorNumber = -1;
        LocalPlayerId = -1;
        LocalSlotIndex = -1;
        LocalPlayerName = "Player";
        OnlineTurnPermissionManager.ResetRuntimeState();
        ClearOnlineRuntimePlayerPrefs();
    }

    public bool HandleEndTurnButtonClicked()
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return false;
        }

        if (!OnlineTurnPermissionManager.CanLocalPlayerAct(true))
        {
            return true;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            AdvanceTurnAsMaster("Local host ended turn");
            return true;
        }

        object[] eventContent = { LocalPlayerId, LocalActorNumber };
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(EndTurnRequestEventCode, eventContent, eventOptions, sendOptions);
        Debug.Log("Online end turn requested by actor " + LocalActorNumber + " for playerId " + LocalPlayerId + ".");
        return true;
    }

    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        if (propertiesThatChanged.ContainsKey(PhotonLobbyPropertyKeys.CurrentTurnPlayerId) ||
            propertiesThatChanged.ContainsKey(PhotonLobbyPropertyKeys.CurrentRound) ||
            propertiesThatChanged.ContainsKey(PhotonLobbyPropertyKeys.OnlineGameActive))
        {
            LogOnlineRoomStateDiagnostics("OnRoomPropertiesUpdate");
            ApplyRoomTurnState(forceApply: false);
        }

        if (propertiesThatChanged.ContainsKey(PhotonLobbyPropertyKeys.OnlineBuildSnapshot))
        {
            EnsureOnlineBuildSyncManagerExists();
            
            if (onlineBuildSyncManager != null)
            {
                onlineBuildSyncManager.HandleRoomPropertiesUpdated();
            }
        }

        if (propertiesThatChanged.ContainsKey(PhotonLobbyPropertyKeys.OnlineCardPublicSnapshot))
        {
            if (!CanUseOnlineCardSyncRuntime())
            {
                return;
            }

            EnsureOnlineCardSyncManagerExists();

            if (onlineCardSyncManager != null)
            {
                onlineCardSyncManager.HandleRoomPropertiesUpdated();
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        Debug.Log("Online GameScene player joined. actor=" + newPlayer.ActorNumber + ", name=" + newPlayer.NickName);
        RebuildOnlineRosterFromPhoton();
        LogOnlineRoomStateDiagnostics("OnPlayerEnteredRoom");
        ShowOnlineToast(GetPhotonPlayerDisplayName(newPlayer) + " joined the room.");

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureCurrentTurnTargetsActivePlayer();
        }

        EnsureOnlineBuildSyncManagerExists();

        if (onlineBuildSyncManager != null)
        {
            onlineBuildSyncManager.InitializeForOnlineMatch();
        }

        if (CanUseOnlineCardSyncRuntime())
        {
            EnsureOnlineCardSyncManagerExists();

            if (onlineCardSyncManager != null)
            {
                onlineCardSyncManager.InitializeForOnlineMatch();
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        int leftPlayerId = GetPlayerIntProperty(otherPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
        bool wasCurrentTurnPlayer = leftPlayerId >= 0 && leftPlayerId == GetRoomCurrentTurnPlayerId();
        Debug.Log("Player left. actor=" + otherPlayer.ActorNumber + ", playerId=" + leftPlayerId + ", name=" + otherPlayer.NickName);
        RebuildOnlineRosterFromPhoton();
        LogOnlineRoomStateDiagnostics("OnPlayerLeftRoom");
        ShowOnlineToast(otherPlayer.IsMasterClient ? "Host left the match." : GetPhotonPlayerDisplayName(otherPlayer) + " left the game.");

        if (wasCurrentTurnPlayer)
        {
            Debug.Log("Current turn player left");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureCurrentTurnTargetsActivePlayer();
        }

        EnsureOnlineBuildSyncManagerExists();

        if (onlineBuildSyncManager != null)
        {
            onlineBuildSyncManager.InitializeForOnlineMatch();
        }

        if (CanUseOnlineCardSyncRuntime())
        {
            EnsureOnlineCardSyncManagerExists();

            if (onlineCardSyncManager != null)
            {
                onlineCardSyncManager.InitializeForOnlineMatch();
            }
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        Debug.Log("MasterClient switched. new actor=" + newMasterClient.ActorNumber + ", local actor=" + PhotonNetwork.LocalPlayer.ActorNumber);
        RebuildOnlineRosterFromPhoton();
        LogOnlineRoomStateDiagnostics("OnMasterClientSwitched");
        ShowOnlineToast("Host migrated.");

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureCurrentTurnTargetsActivePlayer();
        }

        EnsureOnlineBuildSyncManagerExists();

        if (onlineBuildSyncManager != null)
        {
            onlineBuildSyncManager.InitializeForOnlineMatch();
        }

        if (CanUseOnlineCardSyncRuntime())
        {
            EnsureOnlineCardSyncManagerExists();

            if (onlineCardSyncManager != null)
            {
                onlineCardSyncManager.InitializeForOnlineMatch();
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        RebuildOnlineRosterFromPhoton();
        LogOnlineRoomStateDiagnostics("OnPlayerPropertiesUpdate");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!IsOnlineModeActive || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        Debug.LogWarning("Online GameScene disconnected. cause=" + cause);
        ShowOnlineToast(GetDisconnectToastMessage(cause));
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");

        if (!shouldLoadHomeOnLeftRoom)
        {
            return;
        }

        DetachFromOnlineMatchState();
        Debug.Log("Loading HomeScene");
        SceneManager.LoadScene(homeSceneName);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (!IsOnlineModeActive || !PhotonNetwork.IsMasterClient || ShouldIgnoreOnlineCallbacks())
        {
            return;
        }

        if (photonEvent.Code != EndTurnRequestEventCode)
        {
            return;
        }

        object[] requestData = photonEvent.CustomData as object[];

        if (requestData == null || requestData.Length < 2)
        {
            Debug.LogWarning("Invalid room state: rejected online end turn request because payload was missing.");
            return;
        }

        int requestedPlayerId = ConvertToInt(requestData[0], -1);
        int senderActorNumber = photonEvent.Sender;

        if (requestedPlayerId < 0)
        {
            Debug.LogWarning("Invalid room state: rejected online end turn request because requestedPlayerId was invalid. senderActor=" + senderActorNumber);
            return;
        }

        int currentTurnPlayerId = GetRoomCurrentTurnPlayerId();
        int resolvedSenderPlayerId = GetPlayerIdByActorNumber(senderActorNumber);

        if (currentTurnPlayerId != requestedPlayerId || resolvedSenderPlayerId != requestedPlayerId)
        {
            Debug.LogWarning("Turn mismatch/playerId mismatch: rejected online end turn request. senderActor=" + senderActorNumber + ", requestedPlayerId=" + requestedPlayerId + ", resolvedSenderPlayerId=" + resolvedSenderPlayerId + ", currentTurnPlayerId=" + currentTurnPlayerId);
            return;
        }

        AdvanceTurnAsMaster("Received end turn request from actor " + senderActorNumber);
    }

    private void CacheLocalPhotonIdentity()
    {
        LocalActorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        LocalSlotIndex = GetPlayerIntProperty(PhotonNetwork.LocalPlayer, PhotonLobbyPropertyKeys.SlotIndex, -1);
        LocalPlayerId = GetPlayerIntProperty(PhotonNetwork.LocalPlayer, PhotonLobbyPropertyKeys.PlayerId, LocalSlotIndex);
        LocalPlayerName = GetPlayerStringProperty(
            PhotonNetwork.LocalPlayer,
            PhotonLobbyPropertyKeys.PlayerName,
            string.IsNullOrWhiteSpace(PhotonNetwork.NickName) ? "Player" : PhotonNetwork.NickName
        );

        Debug.Log("Online local player id = " + LocalPlayerId + ", actor = " + LocalActorNumber + ", slot = " + LocalSlotIndex + ", name = " + LocalPlayerName);
    }

    private void RebuildOnlineRosterFromPhoton()
    {
        if (playerManager == null || playerManager.players == null)
        {
            return;
        }

        Dictionary<int, Player> activePlayersById = new Dictionary<int, Player>();
        List<Player> photonPlayers = new List<Player>(PhotonNetwork.PlayerList);
        photonPlayers.Sort((left, right) =>
        {
            int leftSlot = GetPlayerIntProperty(left, PhotonLobbyPropertyKeys.SlotIndex, int.MaxValue);
            int rightSlot = GetPlayerIntProperty(right, PhotonLobbyPropertyKeys.SlotIndex, int.MaxValue);

            if (leftSlot != rightSlot)
            {
                return leftSlot.CompareTo(rightSlot);
            }

            return left.ActorNumber.CompareTo(right.ActorNumber);
        });

        foreach (Player photonPlayer in photonPlayers)
        {
            int playerId = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
            int slotIndex = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.SlotIndex, -1);

            if (playerId < 0)
            {
                Debug.LogWarning("Invalid room state: active Photon player has no playerId. actor=" + photonPlayer.ActorNumber + ", name=" + photonPlayer.NickName);
                continue;
            }

            if (slotIndex != playerId)
            {
                Debug.LogWarning("PlayerId mismatch: actor=" + photonPlayer.ActorNumber + ", slotIndex=" + slotIndex + ", playerId=" + playerId + ", name=" + photonPlayer.NickName);
            }

            if (activePlayersById.ContainsKey(playerId))
            {
                Debug.LogWarning("Invalid room state: duplicate online playerId " + playerId + " for actor=" + photonPlayer.ActorNumber + ".");
                continue;
            }

            activePlayersById[playerId] = photonPlayer;
        }

        for (int i = 0; i < playerManager.players.Count; i++)
        {
            PlayerResource playerResource = playerManager.players[i];

            if (playerResource == null)
            {
                continue;
            }

            if (activePlayersById.TryGetValue(playerResource.playerId, out Player photonPlayer))
            {
                string syncedName = GetPlayerStringProperty(
                    photonPlayer,
                    PhotonLobbyPropertyKeys.PlayerName,
                    string.IsNullOrWhiteSpace(photonPlayer.NickName) ? "Player " + playerResource.playerId : photonPlayer.NickName
                );

                playerResource.displayName = syncedName;
                playerResource.isEliminated = false;
            }
            else
            {
                playerResource.displayName = "Empty";
                playerResource.isEliminated = true;
                Debug.Log("Skipped inactive player slot " + playerResource.playerId + ".");
            }

            playerResource.RefreshUI();
        }

        playerManager.RefreshAllPlayerStatusPanels();
        Debug.Log(BuildActiveOnlinePlayerListLog(photonPlayers));
    }

    private void LogOnlineRoomStateDiagnostics(string context)
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("Invalid room state: no Photon room during " + context + ".");
            return;
        }

        HashSet<int> seenPlayerIds = new HashSet<int>();
        HashSet<int> seenSlots = new HashSet<int>();
        List<int> activePlayerIds = GetActiveOnlinePlayerIds();
        int currentTurnPlayerId = GetRoomCurrentTurnPlayerId();
        int currentRound = GetRoomCurrentRound();

        foreach (Player photonPlayer in PhotonNetwork.PlayerList)
        {
            int slotIndex = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.SlotIndex, -1);
            int playerId = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);

            if (slotIndex < 0 || playerId < 0)
            {
                Debug.LogWarning("Invalid room state: missing slot/playerId during " + context + ". actor=" + photonPlayer.ActorNumber + ", slotIndex=" + slotIndex + ", playerId=" + playerId + ".");
                continue;
            }

            if (slotIndex != playerId)
            {
                Debug.LogWarning("PlayerId mismatch during " + context + ". actor=" + photonPlayer.ActorNumber + ", slotIndex=" + slotIndex + ", playerId=" + playerId + ".");
            }

            if (!seenSlots.Add(slotIndex))
            {
                Debug.LogWarning("Invalid room state: duplicate slotIndex " + slotIndex + " during " + context + ".");
            }

            if (!seenPlayerIds.Add(playerId))
            {
                Debug.LogWarning("Invalid room state: duplicate playerId " + playerId + " during " + context + ".");
            }
        }

        if (currentTurnPlayerId >= 0 && !activePlayerIds.Contains(currentTurnPlayerId))
        {
            Debug.LogWarning("Turn mismatch during " + context + ". currentTurnPlayerId=" + currentTurnPlayerId + " is not active. activePlayers=" + string.Join(",", activePlayerIds) + ", round=" + currentRound + ".");
        }

        if (currentTurnPlayerId < 0)
        {
            Debug.Log("Online room state during " + context + ": wave/blocked turn state. currentTurnPlayerId=" + currentTurnPlayerId + ", round=" + currentRound + ", activePlayers=" + string.Join(",", activePlayerIds) + ".");
        }
    }

    private string BuildActiveOnlinePlayerListLog(List<Player> photonPlayers)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Active online player list:");

        foreach (Player photonPlayer in photonPlayers)
        {
            int playerId = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
            int slotIndex = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.SlotIndex, -1);
            string displayName = GetPlayerStringProperty(
                photonPlayer,
                PhotonLobbyPropertyKeys.PlayerName,
                string.IsNullOrWhiteSpace(photonPlayer.NickName) ? "Player" : photonPlayer.NickName
            );

            builder.Append(" [actor=");
            builder.Append(photonPlayer.ActorNumber);
            builder.Append(", slot=");
            builder.Append(slotIndex);
            builder.Append(", playerId=");
            builder.Append(playerId);
            builder.Append(", name=");
            builder.Append(displayName);
            builder.Append("]");
        }

        return builder.ToString();
    }

    private void EnsureRoomTurnStateInitialized()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }
        
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.CurrentTurnPlayerId) &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.CurrentRound))
        {
            return;
        }
        

        int firstActivePlayerId = GetFirstActiveOnlinePlayerId();

        if (firstActivePlayerId < 0)
        {
            return;
        }

        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { PhotonLobbyPropertyKeys.CurrentTurnPlayerId, firstActivePlayerId },
            { PhotonLobbyPropertyKeys.CurrentRound, 1 },
            { PhotonLobbyPropertyKeys.OnlineGameActive, true }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        Debug.Log("Current turn set. playerId=" + firstActivePlayerId + ", round=1");
    }

    private void EnsureCurrentTurnTargetsActivePlayer()
    {
        int currentTurnPlayerId = GetRoomCurrentTurnPlayerId();

        if (currentTurnPlayerId < 0)
        {
            Debug.Log("Turn repair skipped because an online wave or blocked action phase is active.");
            return;
        }

        if (IsPlayerIdActive(currentTurnPlayerId))
        {
            return;
        }

        AdvanceTurnAsMaster("Current turn player left or became inactive");
    }

    private void AdvanceTurnAsMaster(string reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int currentTurnPlayerId = GetRoomCurrentTurnPlayerId();
        int currentRound = GetRoomCurrentRound();
        List<int> activePlayerIds = GetActiveOnlinePlayerIds();

        if (activePlayerIds.Count == 0)
        {
            Debug.LogWarning("Invalid room state: cannot advance turn because there are no active online players. reason=" + reason);
            return;
        }

        if (currentTurnPlayerId < 0)
        {
            Debug.LogWarning("Turn mismatch: ignored AdvanceTurnAsMaster while currentTurnPlayerId=" + currentTurnPlayerId + ". This normally means the enemy wave is active. reason=" + reason);
            return;
        }

        int currentIndex = activePlayerIds.IndexOf(currentTurnPlayerId);
        int nextRound = currentRound;
        int nextPlayerId;

        if (currentIndex < 0)
        {
            Debug.Log("Skipped inactive player " + currentTurnPlayerId + " while advancing turn.");
            nextPlayerId = activePlayerIds[0];
        }
        else
        {
            int nextIndex = (currentIndex + 1) % activePlayerIds.Count;
            nextPlayerId = activePlayerIds[nextIndex];

            if (nextIndex == 0)
            {
                if (onlineWaveCombatSyncManager == null)
                {
                    EnsureOnlineWaveCombatSyncManagerExists();
                }

                if (onlineWaveCombatSyncManager != null &&
                    onlineWaveCombatSyncManager.TryStartWaveAsMaster(currentRound, currentRound))
                {
                    PhotonHashtable waveProperties = new PhotonHashtable
                    {
                        { PhotonLobbyPropertyKeys.CurrentTurnPlayerId, -1 },
                        { PhotonLobbyPropertyKeys.CurrentRound, currentRound },
                        { PhotonLobbyPropertyKeys.OnlineGameActive, true }
                    };

                    PhotonNetwork.CurrentRoom.SetCustomProperties(waveProperties);
                    Debug.Log("Online wave started after all players ended turns. round=" + currentRound);
                    return;
                }

                nextRound += 1;
            }
        }

        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { PhotonLobbyPropertyKeys.CurrentTurnPlayerId, nextPlayerId },
            { PhotonLobbyPropertyKeys.CurrentRound, nextRound },
            { PhotonLobbyPropertyKeys.OnlineGameActive, true }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        Debug.Log("Turn advanced. reason=" + reason + ", nextPlayerId=" + nextPlayerId + ", round=" + nextRound);
    }

    public void BeginNextOnlineRoundAfterWaveAsMaster(int nextRound)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        int firstActivePlayerId = GetFirstActiveOnlinePlayerId();

        if (firstActivePlayerId < 0)
        {
            return;
        }

        PhotonHashtable roomProperties = new PhotonHashtable
        {
            { PhotonLobbyPropertyKeys.CurrentTurnPlayerId, firstActivePlayerId },
            { PhotonLobbyPropertyKeys.CurrentRound, Mathf.Max(1, nextRound) },
            { PhotonLobbyPropertyKeys.OnlineGameActive, true }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        Debug.Log("Online next round started after wave. playerId=" + firstActivePlayerId + ", round=" + Mathf.Max(1, nextRound));
#endif
    }

    private void ApplyRoomTurnState(bool forceApply)
    {
        int currentTurnPlayerId = GetRoomCurrentTurnPlayerId();
        int currentRound = GetRoomCurrentRound();

        if (currentTurnPlayerId < 0)
        {
            if (PhotonOnlineWaveCombatSyncManager.Instance == null || !PhotonOnlineWaveCombatSyncManager.Instance.IsOnlineWaveRunning)
            {
                Debug.LogWarning("Invalid room state: currentTurnPlayerId is " + currentTurnPlayerId + " but no online wave manager reports an active wave.");
            }

            return;
        }

        if (!IsPlayerIdActive(currentTurnPlayerId))
        {
            Debug.LogWarning("Turn mismatch: applying turn for inactive playerId=" + currentTurnPlayerId + ", round=" + currentRound + ".");
        }

        if (!forceApply &&
            hasAppliedTurnState &&
            currentTurnPlayerId == lastAppliedTurnPlayerId &&
            currentRound == lastAppliedRound)
        {
            return;
        }

        PlayerResource activePlayer = playerManager != null ? playerManager.GetPlayerResource(currentTurnPlayerId) : null;
        bool disruptedThisTurn = activePlayer != null && activePlayer.HasPendingDisrupt();

#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.IsMasterClient && PhotonOnlineBuildSyncManager.Instance != null)
        {
            PhotonOnlineBuildSyncManager.Instance.ClearExpiredFreezeClaimsForPlayer(currentTurnPlayerId);
        }
#endif

        if (gamePhaseManager != null)
        {
            gamePhaseManager.currentPhase = GamePhase.PlayerPhase;
            gamePhaseManager.currentRound = Mathf.Max(1, currentRound);
        }

        if (turnManager != null)
        {
            turnManager.currentPlayerId = currentTurnPlayerId;
            turnManager.StartTurn(disruptedThisTurn);
        }

        if (activePlayer != null && disruptedThisTurn)
        {
            activePlayer.ConsumeDisruptForThisTurn();
        }

        if (playerManager != null)
        {
            playerManager.SetCurrentPlayer(currentTurnPlayerId, false);
            playerManager.RefreshAllPlayerStatusPanels();
        }

        if (CurrentTurnIndicatorManager.Instance != null)
        {
            CurrentTurnIndicatorManager.Instance.UpdateCurrentTurnIndicator(currentTurnPlayerId);
        }

        OnlineTurnPermissionManager.NotifyTurnStateApplied(this, currentTurnPlayerId, currentRound);

        hasAppliedTurnState = true;
        lastAppliedTurnPlayerId = currentTurnPlayerId;
        lastAppliedRound = currentRound;
        Debug.Log("Current turn set. playerId=" + currentTurnPlayerId + ", round=" + currentRound);
    }

    private bool ShouldIgnoreOnlineCallbacks()
    {
        return hasDetachedFromOnlineMatch || isLocalLeavingMatch;
    }

    private void ClearOnlineRuntimePlayerPrefs()
    {
        PlayerPrefs.DeleteKey("OnlineLocalPlayerId");
        PlayerPrefs.DeleteKey("PhotonRoomName");
        PlayerPrefs.SetString("GameMode", string.Empty);
        PlayerPrefs.Save();
    }

    private int GetRoomCurrentTurnPlayerId()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return GetFirstActiveOnlinePlayerId();
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.CurrentTurnPlayerId))
        {
            object value = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.CurrentTurnPlayerId];
            return ConvertToInt(value, GetFirstActiveOnlinePlayerId());
        }

        return GetFirstActiveOnlinePlayerId();
    }

    private int GetRoomCurrentRound()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return 1;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.CurrentRound))
        {
            object value = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.CurrentRound];
            return Mathf.Max(1, ConvertToInt(value, 1));
        }

        return 1;
    }

    private int GetFirstActiveOnlinePlayerId()
    {
        List<int> activePlayerIds = GetActiveOnlinePlayerIds();
        return activePlayerIds.Count > 0 ? activePlayerIds[0] : -1;
    }

    private List<int> GetActiveOnlinePlayerIds()
    {
        List<int> activePlayerIds = new List<int>();

        foreach (Player photonPlayer in PhotonNetwork.PlayerList)
        {
            int playerId = GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);

            if (playerId < 0)
            {
                continue;
            }

            activePlayerIds.Add(playerId);
        }

        activePlayerIds.Sort();
        return activePlayerIds;
    }

    private bool IsPlayerIdActive(int playerId)
    {
        return GetActiveOnlinePlayerIds().Contains(playerId);
    }

    private int GetPlayerIdByActorNumber(int actorNumber)
    {
        foreach (Player photonPlayer in PhotonNetwork.PlayerList)
        {
            if (photonPlayer.ActorNumber != actorNumber)
            {
                continue;
            }

            return GetPlayerIntProperty(photonPlayer, PhotonLobbyPropertyKeys.PlayerId, -1);
        }

        return -1;
    }

    private int ConvertToInt(object value, int fallbackValue)
    {
        if (value is int intValue)
        {
            return intValue;
        }

        if (value is byte byteValue)
        {
            return byteValue;
        }

        if (value is short shortValue)
        {
            return shortValue;
        }

        if (value is long longValue)
        {
            return (int)longValue;
        }

        return fallbackValue;
    }

    private int GetPlayerIntProperty(Player player, string key, int fallbackValue)
    {
        if (player == null || player.CustomProperties == null || !player.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        return ConvertToInt(player.CustomProperties[key], fallbackValue);
    }

    private string GetPlayerStringProperty(Player player, string key, string fallbackValue)
    {
        if (player == null || player.CustomProperties == null || !player.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        object value = player.CustomProperties[key];
        return value is string stringValue ? stringValue : fallbackValue;
    }

    private string GetPhotonPlayerDisplayName(Player player)
    {
        return GetPlayerStringProperty(
            player,
            PhotonLobbyPropertyKeys.PlayerName,
            player != null && !string.IsNullOrWhiteSpace(player.NickName) ? player.NickName : "Player"
        );
    }

    private string GetDisconnectToastMessage(DisconnectCause cause)
    {
        switch (cause)
        {
            case DisconnectCause.Exception:
            case DisconnectCause.ExceptionOnConnect:
            case DisconnectCause.ServerTimeout:
            case DisconnectCause.ClientTimeout:
                return "Connection lost.";
            default:
                return "Disconnected from server.";
        }
    }
#endif

    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        if (gamePhaseManager != null &&
            gamePhaseManager.toastMessage != null &&
            !ReferenceEquals(gamePhaseManager.toastMessage, toastMessage) &&
            TryCallToastMethod(gamePhaseManager.toastMessage, message))
        {
            return;
        }

        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ShowToast(message);
            return;
        }

        CardDrawManager cardDrawManager = FindObjectOfType<CardDrawManager>();

        if (cardDrawManager != null)
        {
            cardDrawManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    public void ShowOnlineToast(string message)
    {
        ShowToast(message);
    }

    public string GetOnlineTurnOwnerDisplayName(int playerId)
    {
        if (playerManager != null)
        {
            return playerManager.GetPlayerDisplayName(playerId);
        }

        return "Player " + playerId;
    }

    public int GetOnlinePlayerIdByActorNumber(int actorNumber)
    {
#if PHOTON_UNITY_NETWORKING
        return GetPlayerIdByActorNumber(actorNumber);
#else
        return -1;
#endif
    }

    private void EnsureOnlineBuildSyncManagerExists()
    {
        if (!IsGameSceneObjectRuntime())
        {
            return;
        }

        if (onlineBuildSyncManager != null)
        {
            return;
        }

        onlineBuildSyncManager = FindObjectOfType<PhotonOnlineBuildSyncManager>();

        if (onlineBuildSyncManager != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("PhotonOnlineBuildSyncManager");
        onlineBuildSyncManager = managerObject.AddComponent<PhotonOnlineBuildSyncManager>();
    }

    private void EnsureOnlineCardSyncManagerExists()
    {
        if (!CanUseOnlineCardSyncRuntime())
        {
            return;
        }

        if (onlineCardSyncManager != null)
        {
            return;
        }

        onlineCardSyncManager = FindObjectOfType<PhotonOnlineCardSyncManager>();

        if (onlineCardSyncManager != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("PhotonOnlineCardSyncManager");
        onlineCardSyncManager = managerObject.AddComponent<PhotonOnlineCardSyncManager>();
    }

    private void EnsureOnlineWaveCombatSyncManagerExists()
    {
        if (!CanUseOnlineCardSyncRuntime())
        {
            return;
        }

        if (onlineWaveCombatSyncManager != null)
        {
            return;
        }

        onlineWaveCombatSyncManager = FindObjectOfType<PhotonOnlineWaveCombatSyncManager>();

        if (onlineWaveCombatSyncManager != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("PhotonOnlineWaveCombatSyncManager");
        onlineWaveCombatSyncManager = managerObject.AddComponent<PhotonOnlineWaveCombatSyncManager>();
    }

    private bool CanUseOnlineCardSyncRuntime()
    {
#if PHOTON_UNITY_NETWORKING
        return IsOnlineModeActive &&
            !ShouldIgnoreOnlineCallbacks() &&
            SceneManager.GetActiveScene().name == "GameScene" &&
            PhotonNetwork.InRoom &&
            !PhotonNetwork.OfflineMode;
#else
        return false;
#endif
    }

    private bool TryCallToastMethod(string message)
    {
        return TryCallToastMethod(toastMessage, message);
    }

    private bool TryCallToastMethod(MonoBehaviour toastSource, string message)
    {
        if (toastSource == null)
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
            MethodInfo method = toastSource.GetType().GetMethod(
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

            method.Invoke(toastSource, new object[] { message });
            return true;
        }

        return false;
    }

    private void RegisterPhotonCallbacksIfNeeded()
    {
#if PHOTON_UNITY_NETWORKING
        if (photonCallbacksRegistered)
        {
            return;
        }

        PhotonNetwork.AddCallbackTarget(this);
        photonCallbacksRegistered = true;
        Debug.Log("PhotonOnlineGameSceneManager registered Photon callbacks. scene=" + SceneManager.GetActiveScene().name);
#endif
    }

    private void UnregisterPhotonCallbacksIfNeeded()
    {
#if PHOTON_UNITY_NETWORKING
        if (!photonCallbacksRegistered)
        {
            return;
        }

        PhotonNetwork.RemoveCallbackTarget(this);
        photonCallbacksRegistered = false;
        Debug.Log("PhotonOnlineGameSceneManager unregistered Photon callbacks. scene=" + SceneManager.GetActiveScene().name);
#endif
    }

    private bool IsGameSceneObjectRuntime()
    {
        return SceneManager.GetActiveScene().name == "GameScene";
    }
}
