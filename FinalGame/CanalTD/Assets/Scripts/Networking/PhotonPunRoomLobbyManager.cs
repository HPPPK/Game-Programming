/*
 * File: PhotonPunRoomLobbyManager.cs
 *
 * Purpose:
 * Implements PhotonPunRoomLobbyManager for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PhotonPunRoomLobbyManager within the networking system.
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
 * - Verify PhotonPunRoomLobbyManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
#endif

#if PHOTON_UNITY_NETWORKING
public class PhotonPunRoomLobbyManager : MonoBehaviourPunCallbacks
#else
public class PhotonPunRoomLobbyManager : MonoBehaviour
#endif
{
    private const string DefaultGameVersion = "0.1";
    private const string DefaultFixedRegion = "asia";
    private const string OnlinePhotonMode = "OnlinePhotonPUN2";
    private const string PrivateRoomKind = "private";
    private const string MatchmakingRoomKind = "matchmaking";

    [Header("Scene Integration")]
    public ModeSelectSceneManager modeSelectSceneManager;
    public string onlineGameSceneName = "GameScene";

    [Header("Optional Buttons")]
    public Button joinRoomButton;
    public Button readyButton;
    public Button startMatchButton;
    public Button matchmakingButton;
    public Button leaveRoomButton;

    [Header("Optional Debug UI")]
    public TMP_Text connectionStatusText;

    [Header("Photon Settings")]
    public string gameVersion = DefaultGameVersion;
    public string fixedRegion = DefaultFixedRegion;
    public byte maxPlayersPerRoom = 4;
    public byte minPlayersToStart = 2;

    public bool IsOnlineRoomFlowActive { get; private set; }

    private bool joinRoomButtonBound;
    private bool readyButtonBound;
    private bool startButtonBound;
    private bool matchmakingButtonBound;
    private bool leaveButtonBound;
    private bool usernameListenerBound;
    private bool isConnectingToPhoton;
    private bool isCreatingOrJoiningRoom;
    private bool isMatchmaking;

#if PHOTON_UNITY_NETWORKING
    private enum PendingLobbyAction
    {
        None,
        AutoCreatePrivateRoom,
        JoinPrivateRoomByCode,
        JoinRandomMatchmaking
    }

    private PendingLobbyAction pendingLobbyAction = PendingLobbyAction.None;
    private string pendingJoinRoomCode = string.Empty;
    private string pendingPrivateRoomCode = string.Empty;
    private bool leaveRoomWhenPanelCloses;
    private int privateRoomCreateAttempts;
#endif

    private void Awake()
    {
        if (modeSelectSceneManager == null)
        {
            modeSelectSceneManager = FindObjectOfType<ModeSelectSceneManager>();
        }

#if PHOTON_UNITY_NETWORKING
        PhotonNetwork.AutomaticallySyncScene = true;
#endif

        BindOptionalButton(ref joinRoomButtonBound, joinRoomButton, OnClickJoinRoom);
        BindOptionalButton(ref readyButtonBound, readyButton, ToggleReady);
        BindOptionalButton(ref startButtonBound, startMatchButton, TryStartOnlineMatch);
        BindOptionalButton(ref matchmakingButtonBound, matchmakingButton, StartMatchmaking);
        BindOptionalButton(ref leaveButtonBound, leaveRoomButton, LeaveRoom);
        BindUsernameListener();

        SetConnectionStatus("Offline");
        UpdateLobbyUIState();

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.SetOnlineRoomCodeDisplay("----");
        }
    }

    private void OnDestroy()
    {
        UnbindOptionalButton(ref joinRoomButtonBound, joinRoomButton, OnClickJoinRoom);
        UnbindOptionalButton(ref readyButtonBound, readyButton, ToggleReady);
        UnbindOptionalButton(ref startButtonBound, startMatchButton, TryStartOnlineMatch);
        UnbindOptionalButton(ref matchmakingButtonBound, matchmakingButton, StartMatchmaking);
        UnbindOptionalButton(ref leaveButtonBound, leaveRoomButton, LeaveRoom);
        UnbindUsernameListener();
    }

    // Called when OnlineModePanel is opened. The final design auto-enters a private room.
    public void OnOnlinePanelOpened()
    {
        HandleOnlinePanelOpened();
    }

    // Called when OnlineModePanel is opened. The final design auto-enters a private room.
    public void HandleOnlinePanelOpened()
    {
#if PHOTON_UNITY_NETWORKING
        IsOnlineRoomFlowActive = true;
        leaveRoomWhenPanelCloses = false;
        ApplyLocalDisplayName();

        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Photon already in room. Reusing current room snapshot.");
            ClearPersistentLobbyMessage();
            RefreshLobbySnapshot();
            return;
        }

        ShowPersistentLobbyMessage("Connecting to Photon...");
        RequestAutoCreatePrivateRoom();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    // Closing the online panel safely leaves any current room so the friend room does not linger.
    public void HandleOnlinePanelClosed()
    {
#if PHOTON_UNITY_NETWORKING
        IsOnlineRoomFlowActive = false;

        if (PhotonNetwork.InRoom)
        {
            leaveRoomWhenPanelCloses = true;
            PhotonNetwork.LeaveRoom();
            return;
        }

        ClearOnlineLobbyUI();
#else
        ClearOnlineLobbyUI();
#endif
    }

    public void ConnectToPhoton()
    {
#if PHOTON_UNITY_NETWORKING
        LogPhotonConnectionRequestDiagnostics("ConnectToPhoton");
        Debug.Log("Photon connect requested.");
        ApplyLocalDisplayName();
        PhotonNetwork.AutomaticallySyncScene = true;
        ApplyPhotonConnectionSettings();

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Photon already connected.");
            UpdateLobbyUIState();

            if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
            {
                PhotonNetwork.JoinLobby();
            }

            return;
        }

        if (isConnectingToPhoton)
        {
            Debug.Log("Photon already connecting.");
            SetConnectionStatus("Connecting...");
            UpdateLobbyUIState();
            return;
        }

        isConnectingToPhoton = true;
        SetConnectionStatus("Connecting...");
        UpdateLobbyUIState();
        PhotonNetwork.ConnectUsingSettings();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void OnClickJoinRoom()
    {
#if PHOTON_UNITY_NETWORKING
        string rawRoomCode = modeSelectSceneManager != null
            ? modeSelectSceneManager.GetOnlineJoinRoomCode()
            : string.Empty;
        string normalizedRoomCode = NormalizeRoomCode(rawRoomCode);

        if (string.IsNullOrWhiteSpace(normalizedRoomCode))
        {
            ShowLobbyMessage("Enter a valid room code.");
            return;
        }

        pendingJoinRoomCode = normalizedRoomCode;
        pendingLobbyAction = PendingLobbyAction.JoinPrivateRoomByCode;
        ShowPersistentLobbyMessage("Joining room " + pendingJoinRoomCode + "...");

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        EnsureConnectedAndLobbyReady();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void StartMatchmaking()
    {
#if PHOTON_UNITY_NETWORKING
        if (!IsLocalPlayerReady())
        {
            ShowLobbyMessage("Ready up before matchmaking.");
            return;
        }

        pendingLobbyAction = PendingLobbyAction.JoinRandomMatchmaking;
        pendingJoinRoomCode = string.Empty;
        isMatchmaking = true;
        ShowPersistentLobbyMessage("Matchmaking...");
        UpdateLobbyUIState();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        EnsureConnectedAndLobbyReady();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void LeaveRoom()
    {
#if PHOTON_UNITY_NETWORKING
        ClearPendingLobbyState();
        ClearPersistentLobbyMessage();

        if (!PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Not in a room.");
            return;
        }

        PhotonNetwork.LeaveRoom();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void ToggleReady()
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Join a room first.");
            return;
        }

        ApplyLocalDisplayName();

        bool currentReady = GetPlayerBoolProperty(
            PhotonNetwork.LocalPlayer,
            PhotonLobbyPropertyKeys.Ready,
            false
        );

        Hashtable changedProperties = new Hashtable
        {
            { PhotonLobbyPropertyKeys.Ready, !currentReady },
            { PhotonLobbyPropertyKeys.PlayerName, PhotonNetwork.NickName },
            { PhotonLobbyPropertyKeys.IsAI, false }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(changedProperties);
        ShowLobbyMessage("Ready updated.");
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void TryStartOnlineMatch()
    {
#if PHOTON_UNITY_NETWORKING
        Debug.Log("Online start requested.");
        Debug.Log("Is MasterClient = " + PhotonNetwork.IsMasterClient);
        Debug.Log("AutomaticallySyncScene = " + PhotonNetwork.AutomaticallySyncScene);

        if (!PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Join a room first.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            ShowLobbyMessage("Only host can start.");
            return;
        }

        if (!IsLocalPlayerReady())
        {
            ShowLobbyMessage("Ready up before starting.");
            return;
        }

        AssignSlotsAuthoritatively();

        if (!CanStartMatch())
        {
            return;
        }

        RefreshLobbySnapshot();
        PersistCurrentRoomSnapshotToPlayerPrefs();
        ShowLobbyMessage("Match starting.");
        PhotonNetwork.CurrentRoom.IsOpen = false;
        Debug.Log("Calling PhotonNetwork.LoadLevel(" + onlineGameSceneName + ")");
        PhotonNetwork.LoadLevel(onlineGameSceneName);
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void OnClickStartMatch()
    {
        TryStartOnlineMatch();
    }

    private void BindUsernameListener()
    {
        if (modeSelectSceneManager == null ||
            modeSelectSceneManager.onlineUsernameInput == null ||
            usernameListenerBound)
        {
            return;
        }

        modeSelectSceneManager.onlineUsernameInput.onValueChanged.AddListener(OnOnlineUsernameChanged);
        usernameListenerBound = true;
    }

    private void UnbindUsernameListener()
    {
        if (modeSelectSceneManager == null ||
            modeSelectSceneManager.onlineUsernameInput == null ||
            !usernameListenerBound)
        {
            return;
        }

        modeSelectSceneManager.onlineUsernameInput.onValueChanged.RemoveListener(OnOnlineUsernameChanged);
        usernameListenerBound = false;
    }

    private void OnOnlineUsernameChanged(string _)
    {
#if PHOTON_UNITY_NETWORKING
        ApplyLocalDisplayName();

        if (PhotonNetwork.InRoom)
        {
            RefreshLobbySnapshot();
        }
#endif
    }

    private void BindOptionalButton(ref bool boundFlag, Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || boundFlag)
        {
            return;
        }

        if (button.onClick.GetPersistentEventCount() > 0)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        boundFlag = true;
    }

    private void UnbindOptionalButton(ref bool boundFlag, Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || !boundFlag)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        boundFlag = false;
    }

    private void UpdateLobbyUIState()
    {
        bool inRoom = IsInPhotonRoom();
        bool masterClient = IsPhotonMasterClient();
        bool busy = isConnectingToPhoton || isCreatingOrJoiningRoom;

        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = !busy;
        }

        if (readyButton != null)
        {
            readyButton.interactable = inRoom;
        }

        if (matchmakingButton != null)
        {
            matchmakingButton.interactable = !busy;
        }

        if (startMatchButton != null)
        {
            startMatchButton.interactable = inRoom && masterClient;
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.gameObject.SetActive(inRoom || busy);
            leaveRoomButton.interactable = inRoom;
        }
    }

    private void SetConnectionStatus(string message)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
        }
    }

    private void ShowLobbyMessage(string message)
    {
        Debug.Log(message);

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ShowOnlineLobbyMessage(message);
        }

        SetConnectionStatus(message);
    }

    private void ShowPersistentLobbyMessage(string message)
    {
        Debug.Log(message);

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ShowPersistentToast(message);
            modeSelectSceneManager.ShowOnlinePlaceholderMessage("");
        }

        SetConnectionStatus(message);
    }

    private void ClearPersistentLobbyMessage()
    {
        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ClearPersistentToast();
        }
    }

    private void ClearOnlineLobbyUI()
    {
        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ClearOnlineRoomSnapshot();
            modeSelectSceneManager.ShowOnlinePlaceholderMessage("");
        }

        SetConnectionStatus("Offline");
        UpdateLobbyUIState();
    }

#if PHOTON_UNITY_NETWORKING
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master.");
        isConnectingToPhoton = false;
        ClearPersistentLobbyMessage();
        SetConnectionStatus("Connected");
        UpdateLobbyUIState();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        ShowLobbyMessage("Connected to Photon");
        UpdateLobbyUIState();
        ExecutePendingLobbyActionIfReady();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (pendingLobbyAction == PendingLobbyAction.AutoCreatePrivateRoom && privateRoomCreateAttempts < 5)
        {
            privateRoomCreateAttempts += 1;
            pendingPrivateRoomCode = GenerateShortRoomCode();
            CreatePrivateFriendRoom(pendingPrivateRoomCode);
            return;
        }

        Debug.Log("Create room failed: " + message);
        ClearPendingLobbyState();
        ClearPersistentLobbyMessage();
        ShowLobbyMessage("Create room failed: " + message);
        UpdateLobbyUIState();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log(
            "Join room failed: " + message +
            ", returnCode=" + returnCode +
            ", pendingJoinRoomCode=" + pendingJoinRoomCode +
            ", activeScene=" + SceneManager.GetActiveScene().name +
            ", inRoom=" + PhotonNetwork.InRoom +
            ", inLobby=" + PhotonNetwork.InLobby +
            ", gameVersion=" + PhotonNetwork.GameVersion +
            ", cloudRegion=" + PhotonNetwork.CloudRegion +
            ", clientState=" + PhotonNetwork.NetworkClientState
        );
        ClearPendingLobbyState();
        ClearPersistentLobbyMessage();
        ShowLobbyMessage(
            "Join room failed: " + message +
            ". Check that the host is still in the room, both clients use the same Photon settings, and enter only the shown room code."
        );
        UpdateLobbyUIState();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log(
            "Matchmaking join-random failed: " + message +
            ", returnCode=" + returnCode +
            ", isMatchmaking=" + isMatchmaking +
            ", pendingLobbyAction=" + pendingLobbyAction
        );

        if (isMatchmaking || pendingLobbyAction == PendingLobbyAction.JoinRandomMatchmaking)
        {
            ShowPersistentLobbyMessage("No open matchmaking room found. Creating one...");
            CreatePublicMatchmakingRoom();
            return;
        }

        ClearPendingLobbyState();
        ClearPersistentLobbyMessage();
        ShowLobbyMessage("Matchmaking failed: " + message);
        UpdateLobbyUIState();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected with cause: " + cause);
        bool wasMatchmaking = isMatchmaking;
        ClearPersistentLobbyMessage();

        if (leaveRoomWhenPanelCloses)
        {
            leaveRoomWhenPanelCloses = false;
            ClearPendingLobbyState();
            ClearOnlineLobbyUI();
            return;
        }

        if (IsOnlineRoomFlowActive)
        {
            ClearPendingLobbyState();
            SetConnectionStatus("Disconnected");
            ShowLobbyMessage("Photon connection failed: " + cause);
            UpdateLobbyUIState();
            return;
        }

        if (wasMatchmaking || pendingLobbyAction != PendingLobbyAction.None)
        {
            ShowLobbyMessage("Photon connection failed: " + cause);
        }

        ClearPendingLobbyState();
        UpdateLobbyUIState();
    }

    public override void OnJoinedRoom()
    {
        IsOnlineRoomFlowActive = true;
        isCreatingOrJoiningRoom = false;
        bool joinedMatchmaking = isMatchmaking || pendingLobbyAction == PendingLobbyAction.JoinRandomMatchmaking;
        isMatchmaking = false;
        ClearPersistentLobbyMessage();
        ApplyLocalDisplayName();

        if (PhotonNetwork.IsMasterClient)
        {
            AssignSlotsAuthoritatively();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage(joinedMatchmaking ? "Joined matchmaking room." : "Joined room " + GetCurrentRoomCode() + ".");
        ClearPendingLobbyState();
        UpdateLobbyUIState();
    }

    public override void OnLeftRoom()
    {
        if (leaveRoomWhenPanelCloses)
        {
            leaveRoomWhenPanelCloses = false;
            ClearOnlineLobbyUI();
            return;
        }

        if (pendingLobbyAction != PendingLobbyAction.None)
        {
            ExecutePendingLobbyActionIfReady();
            return;
        }

        ClearOnlineLobbyUI();
        ShowLobbyMessage("Left room.");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignSlotsAuthoritatively();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage(newPlayer.NickName + " joined.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignSlotsAuthoritatively();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage(otherPlayer.NickName + " left.");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignSlotsAuthoritatively();
        }

        RefreshLobbySnapshot();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignSlotsAuthoritatively();
        }

        RefreshLobbySnapshot();
    }

    private void RequestAutoCreatePrivateRoom()
    {
        pendingLobbyAction = PendingLobbyAction.AutoCreatePrivateRoom;
        pendingJoinRoomCode = string.Empty;
        pendingPrivateRoomCode = GenerateShortRoomCode();
        privateRoomCreateAttempts = 0;
        UpdateLobbyUIState();
        EnsureConnectedAndLobbyReady();
    }

    private void EnsureConnectedAndLobbyReady()
    {
        ApplyLocalDisplayName();
        ApplyPhotonConnectionSettings();

        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            ExecutePendingLobbyActionIfReady();
            return;
        }

        if (!PhotonNetwork.InLobby)
        {
            isConnectingToPhoton = false;
            PhotonNetwork.JoinLobby();
            return;
        }

        ExecutePendingLobbyActionIfReady();
    }

    private void ExecutePendingLobbyActionIfReady()
    {
        if (!PhotonNetwork.IsConnected || (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom))
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }

            return;
        }

        if (PhotonNetwork.InRoom)
        {
            RefreshLobbySnapshot();
            return;
        }

        if (pendingLobbyAction == PendingLobbyAction.AutoCreatePrivateRoom)
        {
            if (string.IsNullOrWhiteSpace(pendingPrivateRoomCode))
            {
                pendingPrivateRoomCode = GenerateShortRoomCode();
            }

            CreatePrivateFriendRoom(pendingPrivateRoomCode);
            return;
        }

        if (pendingLobbyAction == PendingLobbyAction.JoinPrivateRoomByCode)
        {
            if (string.IsNullOrWhiteSpace(pendingJoinRoomCode))
            {
                ShowLobbyMessage("Enter a room code.");
                pendingLobbyAction = PendingLobbyAction.None;
                return;
            }

            isCreatingOrJoiningRoom = true;
            Debug.Log(
                "Attempting JoinRoom. roomCode=" + pendingJoinRoomCode +
                ", activeScene=" + SceneManager.GetActiveScene().name +
                ", isConnected=" + PhotonNetwork.IsConnected +
                ", inLobby=" + PhotonNetwork.InLobby +
                ", inRoom=" + PhotonNetwork.InRoom +
                ", gameVersion=" + PhotonNetwork.GameVersion +
                ", cloudRegion=" + PhotonNetwork.CloudRegion +
                ", clientState=" + PhotonNetwork.NetworkClientState
            );
            PhotonNetwork.JoinRoom(pendingJoinRoomCode);
            return;
        }

        if (pendingLobbyAction == PendingLobbyAction.JoinRandomMatchmaking)
        {
            Hashtable expectedProperties = new Hashtable
            {
                { PhotonLobbyPropertyKeys.RoomKind, MatchmakingRoomKind },
                { PhotonLobbyPropertyKeys.MatchMode, OnlinePhotonMode }
            };

            isCreatingOrJoiningRoom = true;
            Debug.Log(
                "Attempting JoinRandomRoom for matchmaking. activeScene=" + SceneManager.GetActiveScene().name +
                ", isConnected=" + PhotonNetwork.IsConnected +
                ", inLobby=" + PhotonNetwork.InLobby +
                ", inRoom=" + PhotonNetwork.InRoom +
                ", clientState=" + PhotonNetwork.NetworkClientState
            );
            PhotonNetwork.JoinRandomRoom(expectedProperties, maxPlayersPerRoom);
        }
    }

    private void CreatePrivateFriendRoom(string roomCode)
    {
        isCreatingOrJoiningRoom = true;
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = false,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = new Hashtable
            {
                { PhotonLobbyPropertyKeys.MatchMode, OnlinePhotonMode },
                { PhotonLobbyPropertyKeys.RoomKind, PrivateRoomKind },
                { PhotonLobbyPropertyKeys.RoomCode, roomCode }
            }
        };

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.SetOnlineRoomCodeDisplay(roomCode);
        }

        Debug.Log(
            "Creating private friend room. roomCode=" + roomCode +
            ", activeScene=" + SceneManager.GetActiveScene().name +
            ", isConnected=" + PhotonNetwork.IsConnected +
            ", inLobby=" + PhotonNetwork.InLobby +
            ", gameVersion=" + PhotonNetwork.GameVersion +
            ", cloudRegion=" + PhotonNetwork.CloudRegion +
            ", clientState=" + PhotonNetwork.NetworkClientState
        );
        PhotonNetwork.CreateRoom(roomCode, roomOptions, TypedLobby.Default);
        UpdateLobbyUIState();
    }

    private void CreatePublicMatchmakingRoom()
    {
        isCreatingOrJoiningRoom = true;
        string roomCode = GenerateShortRoomCode();

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = new Hashtable
            {
                { PhotonLobbyPropertyKeys.MatchMode, OnlinePhotonMode },
                { PhotonLobbyPropertyKeys.RoomKind, MatchmakingRoomKind },
                { PhotonLobbyPropertyKeys.RoomCode, roomCode }
            }
        };

        Debug.Log(
            "Creating public matchmaking room. roomCode=" + roomCode +
            ", activeScene=" + SceneManager.GetActiveScene().name +
            ", isConnected=" + PhotonNetwork.IsConnected +
            ", inLobby=" + PhotonNetwork.InLobby +
            ", gameVersion=" + PhotonNetwork.GameVersion +
            ", cloudRegion=" + PhotonNetwork.CloudRegion +
            ", clientState=" + PhotonNetwork.NetworkClientState
        );
        PhotonNetwork.CreateRoom(roomCode, roomOptions, TypedLobby.Default);
        UpdateLobbyUIState();
    }

    private void ClearPendingLobbyState()
    {
        pendingLobbyAction = PendingLobbyAction.None;
        pendingJoinRoomCode = string.Empty;
        pendingPrivateRoomCode = string.Empty;
        privateRoomCreateAttempts = 0;
        isCreatingOrJoiningRoom = false;
        isMatchmaking = false;
        isConnectingToPhoton = false;
        UpdateLobbyUIState();
    }

    private void ApplyLocalDisplayName()
    {
        string displayName = modeSelectSceneManager != null
            ? modeSelectSceneManager.GetOnlineDisplayName()
            : "Player";

        PhotonNetwork.NickName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { PhotonLobbyPropertyKeys.PlayerName, PhotonNetwork.NickName },
            { PhotonLobbyPropertyKeys.IsAI, false }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(updatedProperties);
    }

    private void ApplyPhotonConnectionSettings()
    {
        string normalizedGameVersion = string.IsNullOrWhiteSpace(gameVersion)
            ? DefaultGameVersion
            : gameVersion.Trim();

        PhotonNetwork.GameVersion = normalizedGameVersion;

        if (PhotonNetwork.PhotonServerSettings == null || PhotonNetwork.PhotonServerSettings.AppSettings == null)
        {
            return;
        }

        string normalizedFixedRegion = string.IsNullOrWhiteSpace(fixedRegion)
            ? DefaultFixedRegion
            : fixedRegion.Trim().ToLowerInvariant();

        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = normalizedGameVersion;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = normalizedFixedRegion;
    }

    private void AssignSlotsAuthoritatively()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        List<Player> sortedPlayers = new List<Player>(PhotonNetwork.PlayerList);
        sortedPlayers.Sort((left, right) => left.ActorNumber.CompareTo(right.ActorNumber));
        bool[] occupiedSlots = new bool[maxPlayersPerRoom];
        Dictionary<int, Player> slotOwners = new Dictionary<int, Player>();

        Debug.Log("Authoritative slot assignment started. Local actor number = " + PhotonNetwork.LocalPlayer.ActorNumber);

        foreach (Player player in sortedPlayers)
        {
            int claimedSlot = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);
            int claimedPlayerId = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.PlayerId, -1);
            string displayName = GetPlayerStringProperty(
                player,
                PhotonLobbyPropertyKeys.PlayerName,
                string.IsNullOrWhiteSpace(player.NickName) ? "Player" : player.NickName
            );

            Debug.Log(
                "Photon player before assignment: name=" + displayName +
                ", nick=" + player.NickName +
                ", actor=" + player.ActorNumber +
                ", slotIndex=" + claimedSlot +
                ", playerId=" + claimedPlayerId
            );

            if (claimedSlot >= 0 &&
                claimedSlot < occupiedSlots.Length &&
                claimedPlayerId == claimedSlot &&
                !slotOwners.ContainsKey(claimedSlot))
            {
                occupiedSlots[claimedSlot] = true;
                slotOwners[claimedSlot] = player;
            }
        }

        foreach (Player player in sortedPlayers)
        {
            int existingSlot = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);
            int existingPlayerId = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.PlayerId, -1);

            if (existingSlot >= 0 &&
                existingSlot < occupiedSlots.Length &&
                existingPlayerId == existingSlot &&
                slotOwners.TryGetValue(existingSlot, out Player existingOwner) &&
                existingOwner == player)
            {
                continue;
            }

            int freeSlot = FindFirstFreeSlot(occupiedSlots);

            if (freeSlot < 0)
            {
                return;
            }

            occupiedSlots[freeSlot] = true;
            slotOwners[freeSlot] = player;

            Hashtable updatedProperties = new Hashtable
            {
                { PhotonLobbyPropertyKeys.SlotIndex, freeSlot },
                { PhotonLobbyPropertyKeys.PlayerId, freeSlot },
                { PhotonLobbyPropertyKeys.PlayerName, string.IsNullOrWhiteSpace(player.NickName) ? "Player" : player.NickName },
                { PhotonLobbyPropertyKeys.Ready, false },
                { PhotonLobbyPropertyKeys.IsAI, false }
            };

            player.SetCustomProperties(updatedProperties);
        }

        Debug.Log(BuildSlotAssignmentDebugString(sortedPlayers));
    }

    private bool CanStartMatch()
    {
        List<Player> joinedPlayers = GetSlottedPlayers();

        if (joinedPlayers.Count < minPlayersToStart)
        {
            ShowLobbyMessage("Need at least " + minPlayersToStart + " players.");
            return false;
        }

        foreach (Player player in joinedPlayers)
        {
            if (!GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.Ready, false))
            {
                ShowLobbyMessage("Waiting for all players.");
                return false;
            }
        }

        return true;
    }

    private bool IsLocalPlayerReady()
    {
        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        return GetPlayerBoolProperty(PhotonNetwork.LocalPlayer, PhotonLobbyPropertyKeys.Ready, false);
    }

    private List<Player> GetSlottedPlayers()
    {
        List<Player> players = new List<Player>();

        if (!PhotonNetwork.InRoom)
        {
            return players;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int slotIndex = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);

            if (slotIndex >= 0 && slotIndex < maxPlayersPerRoom)
            {
                players.Add(player);
            }
        }

        return players;
    }

    private void RefreshLobbySnapshot()
    {
        PlayerSetupData[] snapshot = new PlayerSetupData[maxPlayersPerRoom];
        int localSlotIndex = 0;
        bool localPlayerReady = false;
        List<Player> sortedPlayers = new List<Player>();

        if (PhotonNetwork.InRoom)
        {
            sortedPlayers.AddRange(PhotonNetwork.PlayerList);
            sortedPlayers.Sort((left, right) => left.ActorNumber.CompareTo(right.ActorNumber));
            Debug.Log("Refreshing Photon lobby snapshot. Local actor number = " + PhotonNetwork.LocalPlayer.ActorNumber);

            foreach (Player player in sortedPlayers)
            {
                int slotIndex = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);
                int playerId = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.PlayerId, -1);
                string displayName = GetPlayerStringProperty(
                    player,
                    PhotonLobbyPropertyKeys.PlayerName,
                    string.IsNullOrWhiteSpace(player.NickName) ? "Player" : player.NickName
                );
                bool isReady = GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.Ready, false);
                bool isAI = GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.IsAI, false);

                Debug.Log(
                    "Photon snapshot player: name=" + displayName +
                    ", nick=" + player.NickName +
                    ", actor=" + player.ActorNumber +
                    ", slotIndex=" + slotIndex +
                    ", playerId=" + playerId +
                    ", ready=" + isReady
                );

                if (slotIndex < 0 || slotIndex >= maxPlayersPerRoom)
                {
                    continue;
                }

                PlayerSetupData snapshotData = new PlayerSetupData(slotIndex, displayName, isAI, AIDifficulty.Easy);
                snapshotData.isReady = isReady;
                snapshot[slotIndex] = snapshotData;

                if (player == PhotonNetwork.LocalPlayer)
                {
                    localSlotIndex = slotIndex;
                    localPlayerReady = isReady;
                }
            }
        }

        Debug.Log(BuildSnapshotDebugString(snapshot));

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ApplyOnlineRoomSnapshot(snapshot, localSlotIndex, localPlayerReady);
            modeSelectSceneManager.SetOnlineRoomCodeDisplay(GetCurrentRoomCode());
        }

        PersistRoomSnapshotToPlayerPrefs(snapshot, localSlotIndex);
        UpdateLobbyUIState();
    }

    private void PersistCurrentRoomSnapshotToPlayerPrefs()
    {
        PlayerSetupData[] snapshot = new PlayerSetupData[maxPlayersPerRoom];
        int localSlotIndex = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int slotIndex = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);

            if (slotIndex < 0 || slotIndex >= maxPlayersPerRoom)
            {
                continue;
            }

            PlayerSetupData data = new PlayerSetupData(
                slotIndex,
                GetPlayerStringProperty(player, PhotonLobbyPropertyKeys.PlayerName, player.NickName),
                GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.IsAI, false),
                AIDifficulty.Easy
            );

            data.isReady = GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.Ready, false);
            snapshot[slotIndex] = data;

            if (player == PhotonNetwork.LocalPlayer)
            {
                localSlotIndex = slotIndex;
            }
        }

        PersistRoomSnapshotToPlayerPrefs(snapshot, localSlotIndex);
    }

    private void PersistRoomSnapshotToPlayerPrefs(PlayerSetupData[] snapshot, int localSlotIndex)
    {
        for (int playerId = 0; playerId < maxPlayersPerRoom; playerId++)
        {
            PlayerSetupData data = snapshot != null && playerId < snapshot.Length ? snapshot[playerId] : null;

            if (data == null)
            {
                PlayerPrefs.DeleteKey("PlayerName_" + playerId);
                PlayerPrefs.SetInt("PlayerIsAI_" + playerId, 0);
                PlayerPrefs.SetInt("PlayerReady_" + playerId, 0);
                PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, AIDifficulty.Easy.ToString());
                continue;
            }

            PlayerPrefs.SetString("PlayerName_" + playerId, data.displayName);
            PlayerPrefs.SetInt("PlayerIsAI_" + playerId, data.isAI ? 1 : 0);
            PlayerPrefs.SetInt("PlayerReady_" + playerId, data.isReady ? 1 : 0);
            PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, data.aiDifficulty.ToString());
        }

        PlayerPrefs.SetString("GameMode", OnlinePhotonMode);
        PlayerPrefs.SetInt("OnlineLocalPlayerId", localSlotIndex);
        PlayerPrefs.SetString("PhotonRoomName", GetCurrentRoomCode());
        PlayerPrefs.Save();
    }

    private string GetCurrentRoomCode()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return "----";
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PhotonLobbyPropertyKeys.RoomCode))
        {
            object codeValue = PhotonNetwork.CurrentRoom.CustomProperties[PhotonLobbyPropertyKeys.RoomCode];

            if (codeValue is string stringCode && !string.IsNullOrWhiteSpace(stringCode))
            {
                return stringCode;
            }
        }

        return PhotonNetwork.CurrentRoom.Name;
    }

    private string NormalizeRoomCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return string.Empty;
        }

        string normalizedRoomCode = roomCode.Trim();
        int labelSeparatorIndex = normalizedRoomCode.LastIndexOf(':');

        if (labelSeparatorIndex >= 0 && labelSeparatorIndex < normalizedRoomCode.Length - 1)
        {
            normalizedRoomCode = normalizedRoomCode.Substring(labelSeparatorIndex + 1);
        }

        StringBuilder roomCodeBuilder = new StringBuilder(normalizedRoomCode.Length);

        for (int i = 0; i < normalizedRoomCode.Length; i++)
        {
            char currentCharacter = normalizedRoomCode[i];

            if (char.IsLetterOrDigit(currentCharacter))
            {
                roomCodeBuilder.Append(char.ToUpperInvariant(currentCharacter));
            }
        }

        return roomCodeBuilder.ToString();
    }

    private string GenerateShortRoomCode()
    {
        return Random.Range(1000, 9999).ToString();
    }

    private int FindFirstFreeSlot(bool[] occupiedSlots)
    {
        if (occupiedSlots == null)
        {
            return -1;
        }

        for (int i = 0; i < occupiedSlots.Length; i++)
        {
            if (!occupiedSlots[i])
            {
                return i;
            }
        }

        return -1;
    }

    private int GetPlayerIntProperty(Player player, string key, int fallbackValue)
    {
        if (player == null || player.CustomProperties == null || !player.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        object value = player.CustomProperties[key];

        if (value is byte byteValue)
        {
            return byteValue;
        }

        if (value is int intValue)
        {
            return intValue;
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

    private bool GetPlayerBoolProperty(Player player, string key, bool fallbackValue)
    {
        if (player == null || player.CustomProperties == null || !player.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        object value = player.CustomProperties[key];
        return value is bool boolValue ? boolValue : fallbackValue;
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

    private string BuildSlotAssignmentDebugString(List<Player> sortedPlayers)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Photon authoritative slot state:");

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Player player = sortedPlayers[i];
            string displayName = GetPlayerStringProperty(
                player,
                PhotonLobbyPropertyKeys.PlayerName,
                string.IsNullOrWhiteSpace(player.NickName) ? "Player" : player.NickName
            );
            int slotIndex = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);
            int playerId = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.PlayerId, -1);
            builder.Append(" [actor=");
            builder.Append(player.ActorNumber);
            builder.Append(", name=");
            builder.Append(displayName);
            builder.Append(", slot=");
            builder.Append(slotIndex);
            builder.Append(", playerId=");
            builder.Append(playerId);
            builder.Append("]");
        }

        return builder.ToString();
    }

    private string BuildSnapshotDebugString(PlayerSetupData[] snapshot)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Final slot snapshot:");

        for (int slotIndex = 0; slotIndex < maxPlayersPerRoom; slotIndex++)
        {
            PlayerSetupData data = snapshot != null && slotIndex < snapshot.Length ? snapshot[slotIndex] : null;
            builder.Append(" [slot ");
            builder.Append(slotIndex);
            builder.Append(" = ");

            if (data == null)
            {
                builder.Append("Empty");
            }
            else
            {
                builder.Append(data.displayName);
                builder.Append(", ready=");
                builder.Append(data.isReady);
            }

            builder.Append("]");
        }

        return builder.ToString();
    }
#endif

    private bool IsInPhotonRoom()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.InRoom;
#else
        return false;
#endif
    }

    private bool IsPhotonMasterClient()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.IsMasterClient;
#else
        return false;
#endif
    }

#if PHOTON_UNITY_NETWORKING
    private void LogPhotonConnectionRequestDiagnostics(string source)
    {
        PhotonOnlineGameSceneManager[] onlineManagers = Resources.FindObjectsOfTypeAll<PhotonOnlineGameSceneManager>();
        PhotonOnlineCardSyncManager[] cardSyncManagers = Resources.FindObjectsOfTypeAll<PhotonOnlineCardSyncManager>();

        Debug.Log(
            "Photon connection diagnostics source=" + source +
            ", activeScene=" + SceneManager.GetActiveScene().name +
            ", isConnected=" + PhotonNetwork.IsConnected +
            ", clientState=" + PhotonNetwork.NetworkClientState +
            ", inRoom=" + PhotonNetwork.InRoom +
            ", inLobby=" + PhotonNetwork.InLobby +
            ", onlineGameSceneManagerCount=" + (onlineManagers != null ? onlineManagers.Length : 0) +
            ", onlineCardSyncManagerCount=" + (cardSyncManagers != null ? cardSyncManagers.Length : 0)
        );
    }
#endif
}
