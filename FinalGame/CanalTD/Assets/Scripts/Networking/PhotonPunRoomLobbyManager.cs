/*
 * File: PhotonPunRoomLobbyManager.cs
 *
 * Purpose:
 * Adds the minimum Photon PUN2 room/lobby flow for CanalTD without touching
 * gameplay synchronization yet. This script handles connecting, creating/joining
 * rooms, leaving rooms, assigning stable slot-based playerIds, syncing ready
 * state, and loading GameScene through PhotonNetwork.LoadLevel.
 *
 * Notes:
 * - It intentionally synchronizes only lobby state for Phase 2A.
 * - Gameplay actions, waves, and runtime board objects are not networked here.
 * - When Photon PUN2 is not installed, this script still compiles and shows
 *   setup warnings instead of breaking the project.
 */
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    private const string OnlinePhotonMode = "OnlinePhotonPUN2";
    private const string RoomNamePrefix = "CanalTD-";

    [Header("Scene Integration")]
    public ModeSelectSceneManager modeSelectSceneManager;
    public string onlineGameSceneName = "GameScene";

    [Header("Lobby UI")]
    public TMP_InputField roomCodeInput;
    public TMP_Text connectionStatusText;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button leaveRoomButton;
    public Button startMatchButton;

    [Header("Photon Settings")]
    public string gameVersion = "0.1";
    public byte maxPlayersPerRoom = 4;
    public byte minPlayersToStart = 2;
    public bool autoConnectWhenPanelOpens = false;

    public bool IsOnlineRoomFlowActive { get; private set; }

    private bool createRoomButtonBound;
    private bool joinRoomButtonBound;
    private bool leaveRoomButtonBound;
    private bool startMatchButtonBound;
    private bool usernameListenerBound;

    private void Awake()
    {
        if (modeSelectSceneManager == null)
        {
            modeSelectSceneManager = FindObjectOfType<ModeSelectSceneManager>();
        }

        BindOptionalButton(ref createRoomButtonBound, createRoomButton, OnClickCreateRoom);
        BindOptionalButton(ref joinRoomButtonBound, joinRoomButton, OnClickJoinRoom);
        BindOptionalButton(ref leaveRoomButtonBound, leaveRoomButton, LeaveRoom);
        BindOptionalButton(ref startMatchButtonBound, startMatchButton, TryStartOnlineMatch);
        BindUsernameListener();

        UpdateLobbyUIState();
        SetConnectionStatus("Offline");
    }

    private void OnDestroy()
    {
        UnbindOptionalButton(ref createRoomButtonBound, createRoomButton, OnClickCreateRoom);
        UnbindOptionalButton(ref joinRoomButtonBound, joinRoomButton, OnClickJoinRoom);
        UnbindOptionalButton(ref leaveRoomButtonBound, leaveRoomButton, LeaveRoom);
        UnbindOptionalButton(ref startMatchButtonBound, startMatchButton, TryStartOnlineMatch);
        UnbindUsernameListener();
    }

    // Called by ModeSelectSceneManager when the shared Online panel becomes visible.
    public void HandleOnlinePanelOpened()
    {
        UpdateLobbyUIState();

        if (autoConnectWhenPanelOpens)
        {
            ConnectToPhoton();
        }
    }

    public void ConnectToPhoton()
    {
#if PHOTON_UNITY_NETWORKING
        ApplyLocalDisplayName();
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        if (PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
            {
                PhotonNetwork.JoinLobby();
            }

            SetConnectionStatus(PhotonNetwork.InRoom ? "In Room" : "Connected");
            UpdateLobbyUIState();
            return;
        }

        SetConnectionStatus("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void OnClickCreateRoom()
    {
#if PHOTON_UNITY_NETWORKING
        if (!EnsurePhotonReadyForLobbyAction())
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Already in a room.");
            return;
        }

        string roomName = GetDesiredRoomCode();
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = new Hashtable
            {
                { PhotonLobbyPropertyKeys.MatchMode, OnlinePhotonMode }
            }
        };

        SetConnectionStatus("Creating Room...");
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void OnClickJoinRoom()
    {
#if PHOTON_UNITY_NETWORKING
        if (!EnsurePhotonReadyForLobbyAction())
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Leave the current room before joining another one.");
            return;
        }

        string roomName = GetDesiredRoomCode();

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowLobbyMessage("Enter a room code.");
            return;
        }

        SetConnectionStatus("Joining Room...");
        PhotonNetwork.JoinRoom(roomName);
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void LeaveRoom()
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Not in a room.");
            return;
        }

        SetConnectionStatus("Leaving Room...");
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

        Hashtable updatedProperties = new Hashtable
        {
            { PhotonLobbyPropertyKeys.Ready, !currentReady },
            { PhotonLobbyPropertyKeys.PlayerName, PhotonNetwork.NickName },
            { PhotonLobbyPropertyKeys.IsAI, false }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(updatedProperties);
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    public void TryStartOnlineMatch()
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.InRoom)
        {
            ShowLobbyMessage("Join a room first.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            ShowLobbyMessage("Only the MasterClient can start the match.");
            return;
        }

        AssignMissingSlotsToJoinedPlayers();

        if (!CanStartMatch())
        {
            return;
        }

        PersistCurrentRoomSnapshotToPlayerPrefs();

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        SetConnectionStatus("Loading Match...");
        PhotonNetwork.LoadLevel(onlineGameSceneName);
#else
        ShowLobbyMessage("Photon PUN2 is not installed. Import Photon PUN2 first.");
#endif
    }

    private void BindUsernameListener()
    {
        if (modeSelectSceneManager == null ||
            modeSelectSceneManager.usernameInput == null ||
            usernameListenerBound)
        {
            return;
        }

        modeSelectSceneManager.usernameInput.onValueChanged.AddListener(OnUsernameChanged);
        usernameListenerBound = true;
    }

    private void UnbindUsernameListener()
    {
        if (modeSelectSceneManager == null ||
            modeSelectSceneManager.usernameInput == null ||
            !usernameListenerBound)
        {
            return;
        }

        modeSelectSceneManager.usernameInput.onValueChanged.RemoveListener(OnUsernameChanged);
        usernameListenerBound = false;
    }

    private void OnUsernameChanged(string _)
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
        bool isMasterClient = IsPhotonMasterClient();

        if (createRoomButton != null)
        {
            createRoomButton.interactable = !inRoom;
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = !inRoom;
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.gameObject.SetActive(inRoom);
            leaveRoomButton.interactable = inRoom;
        }

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(inRoom);
            startMatchButton.interactable = inRoom && isMasterClient;
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

#if PHOTON_UNITY_NETWORKING
    public override void OnConnectedToMaster()
    {
        SetConnectionStatus("Connected");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SetConnectionStatus("Lobby Ready");
        ShowLobbyMessage("Connected to Photon.");
        UpdateLobbyUIState();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetConnectionStatus("Create Room Failed");
        ShowLobbyMessage("Create room failed: " + message);
        UpdateLobbyUIState();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetConnectionStatus("Join Room Failed");
        ShowLobbyMessage("Join room failed: " + message);
        UpdateLobbyUIState();
    }

    public override void OnJoinedRoom()
    {
        IsOnlineRoomFlowActive = true;
        ApplyLocalDisplayName();

        if (PhotonNetwork.IsMasterClient)
        {
            AssignMissingSlotsToJoinedPlayers();
        }

        RefreshLobbySnapshot();
        SetConnectionStatus("Room: " + PhotonNetwork.CurrentRoom.Name);
        ShowLobbyMessage("Joined room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnLeftRoom()
    {
        IsOnlineRoomFlowActive = false;
        SetConnectionStatus("Lobby Ready");

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ClearOnlineRoomSnapshot();
            modeSelectSceneManager.ShowOnlinePlaceholderMessage("");
        }

        UpdateLobbyUIState();
        ShowLobbyMessage("Left room.");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignMissingSlotsToJoinedPlayers();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage(newPlayer.NickName + " joined the room.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignMissingSlotsToJoinedPlayers();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage(otherPlayer.NickName + " left the room.");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignMissingSlotsToJoinedPlayers();
        }

        RefreshLobbySnapshot();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignMissingSlotsToJoinedPlayers();
        }

        RefreshLobbySnapshot();
        ShowLobbyMessage("MasterClient is now " + newMasterClient.NickName + ".");
    }

    private bool EnsurePhotonReadyForLobbyAction()
    {
        ApplyLocalDisplayName();

        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
            ShowLobbyMessage("Connecting to Photon. Try again in a moment.");
            return false;
        }

        if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinLobby();
            ShowLobbyMessage("Joining Photon lobby. Try again in a moment.");
            return false;
        }

        return true;
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

    private void AssignMissingSlotsToJoinedPlayers()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        bool[] occupiedSlots = new bool[maxPlayersPerRoom];

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int claimedSlot = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);

            if (claimedSlot >= 0 && claimedSlot < occupiedSlots.Length)
            {
                occupiedSlots[claimedSlot] = true;
            }
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int existingSlot = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);

            if (existingSlot >= 0 && existingSlot < occupiedSlots.Length)
            {
                continue;
            }

            int freeSlot = FindFirstFreeSlot(occupiedSlots);

            if (freeSlot < 0)
            {
                Debug.LogWarning("No free slot remained for Photon player " + player.ActorNumber + ".");
                continue;
            }

            occupiedSlots[freeSlot] = true;

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

    private bool CanStartMatch()
    {
        List<Player> slottedPlayers = GetSlottedPlayers();

        if (slottedPlayers.Count < minPlayersToStart)
        {
            ShowLobbyMessage("Need at least " + minPlayersToStart + " players to start.");
            return false;
        }

        foreach (Player player in slottedPlayers)
        {
            if (!GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.Ready, false))
            {
                string playerName = GetPlayerStringProperty(player, PhotonLobbyPropertyKeys.PlayerName, player.NickName);
                ShowLobbyMessage(playerName + " is not ready.");
                return false;
            }
        }

        return true;
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

        if (PhotonNetwork.InRoom)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int slotIndex = GetPlayerIntProperty(player, PhotonLobbyPropertyKeys.SlotIndex, -1);

                if (slotIndex < 0 || slotIndex >= maxPlayersPerRoom)
                {
                    continue;
                }

                string displayName = GetPlayerStringProperty(
                    player,
                    PhotonLobbyPropertyKeys.PlayerName,
                    string.IsNullOrWhiteSpace(player.NickName) ? "Player" : player.NickName
                );

                bool isReady = GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.Ready, false);
                bool isAI = GetPlayerBoolProperty(player, PhotonLobbyPropertyKeys.IsAI, false);

                PlayerSetupData data = new PlayerSetupData(slotIndex, displayName, isAI, AIDifficulty.Easy);
                data.isReady = isReady;
                snapshot[slotIndex] = data;

                if (player == PhotonNetwork.LocalPlayer)
                {
                    localSlotIndex = slotIndex;
                    localPlayerReady = isReady;
                }
            }
        }

        if (modeSelectSceneManager != null)
        {
            modeSelectSceneManager.ApplyOnlineRoomSnapshot(snapshot, localSlotIndex, localPlayerReady);
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
        int slotCount = Mathf.Max(0, maxPlayersPerRoom);

        for (int playerId = 0; playerId < slotCount; playerId++)
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

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            PlayerPrefs.SetString("PhotonRoomName", PhotonNetwork.CurrentRoom.Name);
        }

        PlayerPrefs.Save();
    }

    private string GetDesiredRoomCode()
    {
        if (roomCodeInput != null && !string.IsNullOrWhiteSpace(roomCodeInput.text))
        {
            return roomCodeInput.text.Trim();
        }

        return RoomNamePrefix + Random.Range(1000, 9999);
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

        return fallbackValue;
    }

    private bool GetPlayerBoolProperty(Player player, string key, bool fallbackValue)
    {
        if (player == null || player.CustomProperties == null || !player.CustomProperties.ContainsKey(key))
        {
            return fallbackValue;
        }

        object value = player.CustomProperties[key];

        if (value is bool boolValue)
        {
            return boolValue;
        }

        return fallbackValue;
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
}
