/*
 * File: ModeSelectSceneManager.cs
 *
 * Purpose:
 * Implements ModeSelectSceneManager for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ModeSelectSceneManager within the ui system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify ModeSelectSceneManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeSelectSceneManager : MonoBehaviour
{
    [Header("Scenes")]
    public string homeSceneName = "HomeScene";
    public string gameSceneName = "GameScene";
    public string aiPrototypeSceneName = "GameScene_AIPrototype";

    [Header("Local Mode")]
    public TMP_InputField[] playerNameInputs;
    [FormerlySerializedAs("localPlayerSlotIndex")]
    public int legacyLocalPlayerSlotIndex = 0;

    [Header("Mode Panels")]
    public GameObject localModePanel;
    public GameObject aiPrototypePanel;
    public GameObject onlineModePanel;

    [Header("AI Prototype Panel")]
    public TMP_InputField aiPrototypeUsernameInput;
    public GameObject aiPrototypeDifficultyDropdownRoot;
    public TMP_Dropdown aiPrototypeDifficultyDropdown;
    public Button aiPrototypeReadyButton;
    public TMP_Text aiPrototypeReadyButtonText;
    public Button aiPrototypeStartButton;
    public Button aiPrototypeCloseButton;

    [Header("Online Mode Panel")]
    public TMP_InputField onlineUsernameInput;
    [FormerlySerializedAs("roomCodeDisplayText")]
    public TMP_Text onlineRoomCodeDisplayText;
    [FormerlySerializedAs("roomCodeInput")]
    public TMP_InputField onlineRoomCodeInput;
    public Button onlineJoinRoomButton;
    public Button onlineReadyButton;
    public TMP_Text onlineReadyButtonText;
    public Button onlineStartButton;
    public Button onlineCloseButton;
    public RoomSlotUI[] onlineRoomSlots;
    // Shared ready-state colors used by both AI Prototype and Online panels.
    public Color readyButtonNormalTextColor = Color.white;
    public Color readyButtonReadyTextColor = new Color(0.45f, 1f, 0.45f, 1f);

    [Header("Toast")]
    public GameObject toastMessageRoot;
    public TMP_Text toastMessageText;
    public float toastDisplaySeconds = 2f;
    public GameObject toastMessage;

    [Header("Legacy Compatibility (Obsolete)")]
    [Tooltip("Old combined Online/AI prototype panel. Kept only for button compatibility during migration.")]
    public GameObject onlineAIModePanel;
    [Tooltip("Old shared username input from the combined room prototype. Formal AI/Online panels do not use this.")]
    public TMP_InputField usernameInput;
    [Tooltip("Old mixed room slot UI used only by the legacy combined room prototype.")]
    public RoomSlotUI[] roomSlots;
    [Tooltip("Legacy status label used only by the combined room prototype.")]
    public TMP_Text placeholderMessageText;
    [Tooltip("Old Add AI button from the combined room prototype. Formal AI mode no longer adds/removes AI players.")]
    public Button addAIButton;
    [Tooltip("Old shared Ready button from the combined room prototype.")]
    public Button readyButton;
    [Tooltip("Old shared Ready label from the combined room prototype.")]
    public TMP_Text readyButtonText;
    [Tooltip("Old mixed room AI difficulty dropdown. Formal AI mode uses aiPrototypeDifficultyDropdown.")]
    public TMP_Dropdown aiDifficultyDropdown;
    [Tooltip("Old mixed room AI difficulty root. Formal AI mode uses aiPrototypeDifficultyDropdownRoot.")]
    public GameObject aiDifficultyDropdownRoot;

    private const int LocalPlayerCount = 4;
    private const string LocalFourPlayerMode = "Local4Player";
    private const string OnlineAIPrototypeMode = "OnlineAIPrototype";
    private readonly PlayerSetupData[] roomPlayers = new PlayerSetupData[LocalPlayerCount];
    private readonly bool[] roomReady = new bool[LocalPlayerCount];
    private AIDifficulty globalAIDifficulty = AIDifficulty.Easy;
    private bool localReady;
    private bool aiPrototypeReady;
    private bool onlinePhotonFlowActive;
    private bool hasRuntimeLocalSlotOverride;
    private int runtimeLocalPlayerSlotIndex;
    private bool cachedReadyButtonTextColor;
    private bool readyButtonListenerBound;
    private bool aiReadyButtonListenerBound;
    private bool aiStartButtonListenerBound;
    private bool aiCloseButtonListenerBound;
    private bool onlineReadyButtonListenerBound;
    private bool onlineStartButtonListenerBound;
    private bool onlineJoinButtonListenerBound;
    private bool onlineCloseButtonListenerBound;
    private Coroutine toastHideCoroutine;
    private bool persistentToastVisible;
    private PhotonPunRoomLobbyManager photonRoomLobbyManager;

    private void Awake()
    {
        photonRoomLobbyManager = FindObjectOfType<PhotonPunRoomLobbyManager>();
        ConfigureAIDifficultyDropdown();
        CacheReadyButtonTextColorIfNeeded(aiPrototypeReadyButtonText);
        CacheReadyButtonTextColorIfNeeded(onlineReadyButtonText);
        CacheReadyButtonTextColorIfNeeded(readyButtonText);

        BindReadyButtonListenerOnce();

        if (usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener(OnUsernameChanged);
        }

        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyDropdownChanged);
        }

        if (aiPrototypeDifficultyDropdown != null)
        {
            aiPrototypeDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyDropdownChanged);
        }

        BindOptionalButton(ref aiReadyButtonListenerBound, aiPrototypeReadyButton, ToggleAIPrototypeReady);
        BindOptionalButton(ref aiStartButtonListenerBound, aiPrototypeStartButton, StartAIPrototypeGame);
        BindOptionalButton(ref aiCloseButtonListenerBound, aiPrototypeCloseButton, CloseAIPrototypePanel);
        BindOptionalButton(ref onlineReadyButtonListenerBound, onlineReadyButton, ToggleOnlineReady);
        BindOptionalButton(ref onlineStartButtonListenerBound, onlineStartButton, StartOnlineModeGame);
        BindOptionalButton(ref onlineJoinButtonListenerBound, onlineJoinRoomButton, JoinOnlineRoomByCode);
        BindOptionalButton(ref onlineCloseButtonListenerBound, onlineCloseButton, CloseOnlineModePanel);

        BindOptionalInputListener(aiPrototypeUsernameInput);
        BindOptionalInputListener(onlineUsernameInput);
    }

    private void OnDestroy()
    {
        if (readyButton != null && readyButtonListenerBound)
        {
            readyButton.onClick.RemoveListener(ToggleLocalReady);
            readyButtonListenerBound = false;
        }

        if (usernameInput != null)
        {
            usernameInput.onValueChanged.RemoveListener(OnUsernameChanged);
        }

        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.onValueChanged.RemoveListener(OnAIDifficultyDropdownChanged);
        }

        if (aiPrototypeDifficultyDropdown != null)
        {
            aiPrototypeDifficultyDropdown.onValueChanged.RemoveListener(OnAIDifficultyDropdownChanged);
        }

        UnbindOptionalButton(ref aiReadyButtonListenerBound, aiPrototypeReadyButton, ToggleAIPrototypeReady);
        UnbindOptionalButton(ref aiStartButtonListenerBound, aiPrototypeStartButton, StartAIPrototypeGame);
        UnbindOptionalButton(ref aiCloseButtonListenerBound, aiPrototypeCloseButton, CloseAIPrototypePanel);
        UnbindOptionalButton(ref onlineReadyButtonListenerBound, onlineReadyButton, ToggleOnlineReady);
        UnbindOptionalButton(ref onlineStartButtonListenerBound, onlineStartButton, StartOnlineModeGame);
        UnbindOptionalButton(ref onlineJoinButtonListenerBound, onlineJoinRoomButton, JoinOnlineRoomByCode);
        UnbindOptionalButton(ref onlineCloseButtonListenerBound, onlineCloseButton, CloseOnlineModePanel);

        UnbindOptionalInputListener(aiPrototypeUsernameInput);
        UnbindOptionalInputListener(onlineUsernameInput);
    }

    private void Start()
    {
        // Start with both setup panels hidden so only the main mode buttons are visible.
        ClosePanels();
        ResetRoomSetup();
        ResetAIPrototypeSetup();
        UpdateReadyButtonUI();
        UpdateAIPrototypeUI();
    }

    // Binds the Ready button through code only when it is not already wired in the Inspector.
    private void BindReadyButtonListenerOnce()
    {
        if (readyButton == null || readyButtonListenerBound)
        {
            return;
        }

        if (readyButton.onClick.GetPersistentEventCount() > 0)
        {
            Debug.Log("Ready button uses Inspector OnClick binding; code listener was not added.");
            return;
        }

        readyButton.onClick.RemoveListener(ToggleLocalReady);
        readyButton.onClick.AddListener(ToggleLocalReady);
        readyButtonListenerBound = true;
        Debug.Log("Ready button listener bound once.");
    }

    // Shows local four-player options and hides legacy online/AI options.
    public void OpenLocalModePanel()
    {
        SetPanelActive(localModePanel, true, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
    }

    // Opens the final AI prototype setup panel with one human and fixed AI opponents.
    public void OpenAIPrototypePanel()
    {
        SetPanelActive(aiPrototypePanel, true, "AI prototype panel");
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
        ResetAIPrototypeSetup();
    }

    // Opens the final Photon online panel and lets the Photon manager auto-enter a private room.
    public void OpenOnlineModePanel()
    {
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(onlineModePanel, true, "Online mode panel");
        SetOnlineRoomCodeDisplay("----");

        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.OnOnlinePanelOpened();
        }
    }

    // Compatibility wrapper for older button bindings that still open the old combined panel.
    [System.Obsolete("Use OpenOnlineModePanel() or OpenAIPrototypePanel() depending on the target panel.")]
    public void OpenOnlineAIModePanel()
    {
        OpenOnlineModePanel();
    }

    // Hides both mode panels. This is useful when a close button should return to the clean mode choice screen.
    public void ClosePanels()
    {
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
    }

    // Saves four local player names into PlayerPrefs, then loads the gameplay scene.
    public void StartLocalFourPlayerGame()
    {
        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            string playerName = GetInputPlayerName(playerId);
            PlayerPrefs.SetString(GetPlayerNameKey(playerId), playerName);
            PlayerPrefs.SetInt("PlayerIsAI_" + playerId, 0);
        }

        PlayerPrefs.SetString("GameMode", LocalFourPlayerMode);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // Compatibility wrapper for older mixed-room prototype buttons.
    [System.Obsolete("Legacy combined room flow only. Formal AI mode does not add/remove AI slots.")]
    public void AddAIPlayerPlaceholder()
    {
        AddAIPlayer();
    }

    // Compatibility wrapper for older mixed-room prototype buttons.
    [System.Obsolete("Use OpenOnlineModePanel() and the room-code join flow from the Online panel.")]
    public void CreateRoomPlaceholder()
    {
        OpenOnlineModePanel();
    }

    public void CloseAIPrototypePanel()
    {
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
    }

    public void CloseOnlineModePanel()
    {
        SetPanelActive(onlineModePanel, false, "Online mode panel");

        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.HandleOnlinePanelClosed();
        }
    }

    // Returns to the HomeScene without changing saved player names.
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Adds the next available AI player into the first non-local empty slot.
    // This remains only for the legacy combined room prototype.
    public void AddAIPlayer()
    {
        if (IsAIPrototypePanelActive())
        {
            ShowLobbyMessage("AI Prototype mode uses fixed AI opponents. Add AI is no longer used here.");
            return;
        }

        if (ShouldUsePhotonOnlineFlow())
        {
            ShowLobbyMessage("AI slots are not available in the Photon room flow yet.");
            return;
        }

        RefreshLocalRoomPlayer();

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (IsLocalPlayerSlot(playerId))
            {
                continue;
            }

            if (roomPlayers[playerId] != null)
            {
                continue;
            }

            int aiNumber = CountAIPlayers() + 1;
            roomPlayers[playerId] = new PlayerSetupData(
                playerId,
                "Player " + aiNumber,
                true,
                globalAIDifficulty
            );
            roomReady[playerId] = true;

            RefreshRoomUI();
            ShowPlaceholderMessage("");
            return;
        }

        ShowRoomFullMessage();
    }

    // Removes an AI player from a specific room slot. The local player's slot cannot be removed.
    public void RemoveAIPlayer(int playerId)
    {
        RemoveSlot(playerId);
    }

    // Removes the clicked slot only. The local human slot cannot be removed.
    public void RemoveSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= LocalPlayerCount || IsLocalPlayerSlot(slotIndex))
        {
            return;
        }

        if (roomPlayers[slotIndex] == null)
        {
            return;
        }

        if (!roomPlayers[slotIndex].isAI)
        {
            Debug.Log("Only AI slots can be removed.");
            return;
        }

        roomPlayers[slotIndex] = null;
        roomReady[slotIndex] = false;
        RefreshRoomUI();
    }

    // Kept for compatibility with old buttons, but individual slot remove buttons should call RemoveSlot(slotIndex).
    public void RemoveLastAIPlayer()
    {
        for (int playerId = LocalPlayerCount - 1; playerId >= 0; playerId--)
        {
            if (IsLocalPlayerSlot(playerId))
            {
                continue;
            }

            if (roomPlayers[playerId] != null && roomPlayers[playerId].isAI)
            {
                RemoveAIPlayer(playerId);
                return;
            }
        }

        string message = "No AI player to remove.";
        Debug.Log(message);
        ShowPlaceholderMessage(message);
    }

    // Starts whichever flow the legacy shared Start button currently points at.
    // Formal panels should call StartAIPrototypeGame() or StartOnlineModeGame() directly.
    [System.Obsolete("Use StartAIPrototypeGame() or StartOnlineModeGame() based on the active panel.")]
    public void StartOnlineAIPrototypeGame()
    {
        if (IsOnlineModePanelActive())
        {
            StartOnlineModeGame();
            return;
        }

        if (IsAIPrototypePanelActive())
        {
            StartAIPrototypeGame();
            return;
        }

        if (ShouldUsePhotonOnlineFlow())
        {
            if (photonRoomLobbyManager != null)
            {
                photonRoomLobbyManager.TryStartOnlineMatch();
            }

            return;
        }

        RefreshLocalRoomPlayer();
        Debug.Log("Start clicked. localReady = " + localReady);

        if (!IsRoomFull())
        {
            ShowLobbyMessage("Need 4 players to start.");
            return;
        }

        if (!CanStartOnlineAIPrototypeGame())
        {
            string message = "Please ready up before starting.";
            ShowLobbyMessage(message);
            return;
        }

        SaveRoomSetupToPlayerPrefs();
        SceneManager.LoadScene(aiPrototypeSceneName);
    }

    public void StartAIPrototypeGame()
    {
        if (!aiPrototypeReady)
        {
            ShowLobbyMessage("Ready up before starting.");
            return;
        }

        SaveAIPrototypeSetupToPlayerPrefs();
        SceneManager.LoadScene(aiPrototypeSceneName);
    }

    public void StartOnlineModeGame()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.TryStartOnlineMatch();
        }
    }

    public void JoinOnlineRoomByCode()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.OnClickJoinRoom();
        }
    }

    // Routes old shared Ready button bindings to the formal panel-specific flow when needed.
    public void ToggleLocalReady()
    {
        if (IsOnlineModePanelActive())
        {
            ToggleOnlineReady();
            return;
        }

        if (IsAIPrototypePanelActive())
        {
            ToggleAIPrototypeReady();
            return;
        }

        if (ShouldUsePhotonOnlineFlow())
        {
            if (photonRoomLobbyManager != null)
            {
                photonRoomLobbyManager.ToggleReady();
            }

            return;
        }

        Debug.Log("ToggleLocalReady called");
        RefreshLocalRoomPlayer();

        int localSlot = GetLocalPlayerSlotIndex();

        if (roomPlayers[localSlot] == null || roomPlayers[localSlot].isAI)
        {
            Debug.LogWarning("Local player slot is missing or not human.");
            return;
        }

        localReady = !localReady;
        roomReady[localSlot] = localReady;
        roomPlayers[localSlot].isReady = localReady;
        Debug.Log("Local ready = " + localReady);
        RefreshRoomUI();
        UpdateReadyButtonUI();
        ShowPlaceholderMessage("");
    }

    public void ToggleAIPrototypeReady()
    {
        aiPrototypeReady = !aiPrototypeReady;
        UpdateAIPrototypeUI();
        ShowPlaceholderMessage("");
    }

    public void ToggleOnlineReady()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.ToggleReady();
        }
    }

    // Compatibility entry for older slot buttons. Difficulty is now global, not per-slot.
    public void CycleAIDifficulty(int slotIndex)
    {
        AIDifficulty nextDifficulty = AIDifficulty.Easy;

        if (globalAIDifficulty == AIDifficulty.Easy)
        {
            nextDifficulty = AIDifficulty.Medium;
        }
        else if (globalAIDifficulty == AIDifficulty.Medium)
        {
            nextDifficulty = AIDifficulty.Hard;
        }

        SetGlobalAIDifficulty(nextDifficulty);
    }

    // Compatibility entry for older scripts. This updates the shared AI difficulty for every AI slot.
    public void SetAIDifficulty(int slotIndex, AIDifficulty difficulty)
    {
        SetGlobalAIDifficulty(difficulty);
    }

    // Sets the shared AI difficulty and refreshes every AI slot display.
    public void SetGlobalAIDifficulty(AIDifficulty difficulty)
    {
        globalAIDifficulty = difficulty;

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (roomPlayers[playerId] != null && roomPlayers[playerId].isAI)
            {
                roomPlayers[playerId].aiDifficulty = globalAIDifficulty;
            }
        }

        if (aiDifficultyDropdown != null && aiDifficultyDropdown.value != (int)globalAIDifficulty)
        {
            aiDifficultyDropdown.SetValueWithoutNotify((int)globalAIDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
        }

        if (aiPrototypeDifficultyDropdown != null && aiPrototypeDifficultyDropdown.value != (int)globalAIDifficulty)
        {
            aiPrototypeDifficultyDropdown.SetValueWithoutNotify((int)globalAIDifficulty);
            aiPrototypeDifficultyDropdown.RefreshShownValue();
        }

        RefreshRoomUI();
        UpdateAIPrototypeUI();
    }

    // Toggles ready for a human slot. AI slots are always ready and empty slots are ignored.
    public void ToggleReady(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= LocalPlayerCount)
        {
            return;
        }

        if (IsLocalPlayerSlot(slotIndex))
        {
            ToggleLocalReady();
            return;
        }

        PlayerSetupData player = roomPlayers[slotIndex];

        if (player == null || player.isAI)
        {
            return;
        }

        roomReady[slotIndex] = !roomReady[slotIndex];
        player.isReady = roomReady[slotIndex];
        RefreshRoomSlot(slotIndex);
        ShowPlaceholderMessage("");
    }

    // Reads one input field and falls back to Player 1 through Player 4 when empty.
    private string GetInputPlayerName(int playerId)
    {
        if (playerNameInputs != null &&
            playerId < playerNameInputs.Length &&
            playerNameInputs[playerId] != null)
        {
            string typedName = playerNameInputs[playerId].text;

            if (!string.IsNullOrWhiteSpace(typedName))
            {
                return typedName.Trim();
            }
        }

        return "Player " + (playerId + 1);
    }

    // Builds the PlayerPrefs key used by GameScene player resources.
    private string GetPlayerNameKey(int playerId)
    {
        return "PlayerName_" + playerId;
    }

    // Resets the legacy mixed room prototype to one local player and every other slot empty.
    private void ResetRoomSetup()
    {
        globalAIDifficulty = AIDifficulty.Easy;
        localReady = false;
        onlinePhotonFlowActive = false;
        hasRuntimeLocalSlotOverride = false;
        ConfigureAIDifficultyDropdown();

        for (int playerId = 0; playerId < roomPlayers.Length; playerId++)
        {
            roomPlayers[playerId] = null;
            roomReady[playerId] = false;
        }

        RefreshLocalRoomPlayer();
        RefreshRoomUI();
        UpdateReadyButtonUI();
        ShowPlaceholderMessage("");
    }

    private void ResetAIPrototypeSetup()
    {
        aiPrototypeReady = false;

        if (aiPrototypeDifficultyDropdownRoot != null)
        {
            aiPrototypeDifficultyDropdownRoot.SetActive(true);
        }

        if (aiPrototypeDifficultyDropdown != null)
        {
            aiPrototypeDifficultyDropdown.interactable = true;
        }

        UpdateAIPrototypeUI();
    }

    // Updates the legacy local slot from the old shared UsernameInput, using a stable fallback when empty.
    private void RefreshLocalRoomPlayer()
    {
        int localSlot = GetLocalPlayerSlotIndex();
        string username = "Player";

        if (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text))
        {
            username = usernameInput.text.Trim();
        }

        roomPlayers[localSlot] = new PlayerSetupData(localSlot, username, false, AIDifficulty.Easy);
        roomPlayers[localSlot].isReady = localReady;
        roomReady[localSlot] = localReady;
    }

    // Refreshes all room UI: slot rows, remove buttons, and the Add AI button full-room state.
    public void RefreshRoomUI()
    {
        RefreshRoomSlots();
        RefreshAddAIButton();
        UpdateReadyButtonUI();
        RefreshAIDifficultyDropdownVisibility();
    }

    // Writes the current room data to every visible RoomSlotUI.
    private void RefreshRoomSlots()
    {
        RoomSlotUI[] activeSlots = GetActiveRoomSlots();

        if (activeSlots == null)
        {
            return;
        }

        for (int playerId = 0; playerId < activeSlots.Length && playerId < LocalPlayerCount; playerId++)
        {
            RoomSlotUI slot = activeSlots[playerId];

            if (slot == null)
            {
                continue;
            }

            slot.slotIndex = playerId;

            if (slot.manager == null)
            {
                slot.manager = this;
            }

            RefreshRoomSlot(playerId);
        }
    }

    // Refreshes one slot only, so removing an AI does not disturb other slot visuals.
    private void RefreshRoomSlot(int playerId)
    {
        RoomSlotUI[] activeSlots = GetActiveRoomSlots();

        if (activeSlots == null || playerId < 0 || playerId >= activeSlots.Length || playerId >= LocalPlayerCount)
        {
            return;
        }

        RoomSlotUI slot = activeSlots[playerId];

        if (slot == null)
        {
            return;
        }

        slot.slotIndex = playerId;

        if (slot.manager == null)
        {
            slot.manager = this;
        }

        PlayerSetupData data = roomPlayers[playerId];

        if (data == null)
        {
            if (onlinePhotonFlowActive)
            {
                slot.SetOnlineEmpty();
            }
            else
            {
                slot.SetEmpty();
            }

            return;
        }

        if (onlinePhotonFlowActive)
        {
            slot.SetOnlinePlayer(data.displayName, data.isReady);
            return;
        }

        if (data.isAI)
        {
            data.aiDifficulty = globalAIDifficulty;
            slot.SetAI(data.displayName, globalAIDifficulty, true);
        }
        else
        {
            bool ready = IsLocalPlayerSlot(playerId) ? localReady : roomReady[playerId];
            slot.SetHuman(data.displayName, ready);
        }
    }

    // Counts AI players currently occupying any non-local slot.
    private int CountAIPlayers()
    {
        int count = 0;

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (IsLocalPlayerSlot(playerId))
            {
                continue;
            }

            if (roomPlayers[playerId] != null && roomPlayers[playerId].isAI)
            {
                count += 1;
            }
        }

        return count;
    }

    // Optional helper kept for older experiments. Current Start Game keeps empty slots empty.
    private void FillEmptySlotsWithAI()
    {
        int aiNumber = CountAIPlayers() + 1;

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (IsLocalPlayerSlot(playerId))
            {
                continue;
            }

            if (roomPlayers[playerId] != null)
            {
                continue;
            }

            roomPlayers[playerId] = new PlayerSetupData(
                playerId,
                "Player " + aiNumber,
                true,
                globalAIDifficulty
            );
            roomReady[playerId] = true;

            aiNumber += 1;
        }

        RefreshRoomUI();
    }

    // Returns true when every room slot has a player or AI assigned.
    private bool IsRoomFull()
    {
        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (roomPlayers[playerId] == null)
            {
                return false;
            }
        }

        return true;
    }

    // Enables Add AI only while an empty non-local slot exists.
    private void RefreshAddAIButton()
    {
        if (onlinePhotonFlowActive)
        {
            if (addAIButton != null)
            {
                addAIButton.gameObject.SetActive(false);
                addAIButton.interactable = false;
            }

            return;
        }

        bool roomFull = !HasEmptyNonLocalSlot();

        if (addAIButton != null)
        {
            addAIButton.gameObject.SetActive(true);
            addAIButton.interactable = !roomFull;
        }

        if (roomFull)
        {
            ShowRoomFullMessage();
        }
    }

    // Creates the shared difficulty dropdown options and syncs it to the current global value.
    private void ConfigureAIDifficultyDropdown()
    {
        ConfigureDifficultyDropdown(aiDifficultyDropdown);
        ConfigureDifficultyDropdown(aiPrototypeDifficultyDropdown);
    }

    private void ConfigureDifficultyDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
        dropdown.SetValueWithoutNotify((int)globalAIDifficulty);
        dropdown.RefreshShownValue();
    }

    // Handles the shared dropdown changing. All current and future AI players use this value.
    private void OnAIDifficultyDropdownChanged(int optionIndex)
    {
        AIDifficulty selectedDifficulty = AIDifficulty.Easy;

        if (optionIndex == (int)AIDifficulty.Medium)
        {
            selectedDifficulty = AIDifficulty.Medium;
        }
        else if (optionIndex == (int)AIDifficulty.Hard)
        {
            selectedDifficulty = AIDifficulty.Hard;
        }

        SetGlobalAIDifficulty(selectedDifficulty);
    }

    // Shows the shared AI difficulty dropdown only after at least one AI has joined the room.
    private void RefreshAIDifficultyDropdownVisibility()
    {
        if (onlinePhotonFlowActive)
        {
            if (aiDifficultyDropdownRoot != null)
            {
                aiDifficultyDropdownRoot.SetActive(false);
            }

            if (aiDifficultyDropdown != null)
            {
                aiDifficultyDropdown.interactable = false;
            }

            return;
        }

        if (aiPrototypePanel != null && aiPrototypePanel.activeSelf)
        {
            if (aiPrototypeDifficultyDropdownRoot != null)
            {
                aiPrototypeDifficultyDropdownRoot.SetActive(true);
            }

            if (aiPrototypeDifficultyDropdown != null)
            {
                aiPrototypeDifficultyDropdown.interactable = true;
            }
        }

        bool hasAI = CountAIPlayers() > 0;

        if (aiDifficultyDropdownRoot != null)
        {
            aiDifficultyDropdownRoot.SetActive(hasAI);
        }

        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.interactable = hasAI;
        }
    }

    // Updates only the global Ready button visuals. The slot ready text is owned by RoomSlotUI.
    private void UpdateReadyButtonUI()
    {
        int localSlot = GetLocalPlayerSlotIndex();
        bool hasLocalHuman = roomPlayers[localSlot] != null && !roomPlayers[localSlot].isAI;

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(hasLocalHuman);
            readyButton.interactable = hasLocalHuman;
        }

        if (readyButtonText != null)
        {
            readyButtonText.text = localReady ? "Unready" : "Ready";

            if (!cachedReadyButtonTextColor)
            {
                readyButtonNormalTextColor = readyButtonText.color;
                cachedReadyButtonTextColor = true;
            }

            readyButtonText.color = localReady ? readyButtonReadyTextColor : readyButtonNormalTextColor;
        }
    }

    // Keeps the local slot's displayed name synced while the local user edits UsernameInput.
    private void OnUsernameChanged(string ignored)
    {
        RefreshLocalRoomPlayer();
        RefreshRoomSlot(GetLocalPlayerSlotIndex());
        UpdateReadyButtonUI();
        UpdateAIPrototypeUI();
    }

    // Start requires a local human and every occupied slot to be ready.
    private bool CanStartOnlineAIPrototypeGame()
    {
        int localSlot = GetLocalPlayerSlotIndex();

        if (roomPlayers[localSlot] == null || roomPlayers[localSlot].isAI || !localReady)
        {
            return false;
        }

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            PlayerSetupData player = roomPlayers[playerId];

            if (player == null)
            {
                continue;
            }

            if (player.isAI)
            {
                roomReady[playerId] = true;
                player.isReady = true;
                continue;
            }

            if (IsLocalPlayerSlot(playerId))
            {
                player.isReady = localReady;
                continue;
            }

            if (!roomReady[playerId])
            {
                return false;
            }
        }

        return true;
    }

    // Returns a safe local slot index even if the legacy Inspector value is outside the room range.
    private int GetLocalPlayerSlotIndex()
    {
        if (hasRuntimeLocalSlotOverride)
        {
            return Mathf.Clamp(runtimeLocalPlayerSlotIndex, 0, LocalPlayerCount - 1);
        }

        return Mathf.Clamp(legacyLocalPlayerSlotIndex, 0, LocalPlayerCount - 1);
    }

    // Centralizes local slot checks so future online mode can move the local player out of slot 0.
    private bool IsLocalPlayerSlot(int slotIndex)
    {
        return slotIndex == GetLocalPlayerSlotIndex();
    }

    // Checks whether Add AI can place another AI without touching the local player's slot.
    private bool HasEmptyNonLocalSlot()
    {
        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            if (IsLocalPlayerSlot(playerId))
            {
                continue;
            }

            if (roomPlayers[playerId] == null)
            {
                return true;
            }
        }

        return false;
    }

    // Logs and displays the full-room message without closing the room panel.
    private void ShowRoomFullMessage()
    {
        string message = "Room is full.";
        ShowLobbyMessage(message);
    }

    // Saves names, AI flags, and mode data for GameScene to read later.
    private void SaveRoomSetupToPlayerPrefs()
    {
        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            PlayerSetupData data = roomPlayers[playerId];

            if (data == null)
            {
                PlayerPrefs.DeleteKey(GetPlayerNameKey(playerId));
                PlayerPrefs.SetInt("PlayerIsAI_" + playerId, 0);
                PlayerPrefs.SetInt("PlayerReady_" + playerId, 0);
                PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, AIDifficulty.Easy.ToString());
                continue;
            }

            PlayerPrefs.SetString(GetPlayerNameKey(playerId), data.displayName);
            PlayerPrefs.SetInt("PlayerIsAI_" + playerId, data.isAI ? 1 : 0);

            bool savedReady = IsLocalPlayerSlot(playerId) ? localReady : roomReady[playerId];
            PlayerPrefs.SetInt("PlayerReady_" + playerId, savedReady ? 1 : 0);

            if (data.isAI)
            {
                data.aiDifficulty = globalAIDifficulty;
                PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, globalAIDifficulty.ToString());
            }
            else
            {
                PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, AIDifficulty.Easy.ToString());
            }
        }

        PlayerPrefs.SetString("GameMode", OnlineAIPrototypeMode);
        PlayerPrefs.Save();
    }

    // Shows or clears the old mixed-room status text only while the legacy combined panel is in use.
    private void ShowPlaceholderMessage(string message)
    {
        if (!IsLegacyCombinedRoomPrototypeActive())
        {
            return;
        }

        if (placeholderMessageText != null)
        {
            placeholderMessageText.text = message;
        }
    }

    // Shows lobby feedback through legacy status text and the optional toast UI.
    private void ShowLobbyMessage(string message)
    {
        Debug.Log(message);
        ShowPlaceholderMessage(message);
        ShowToast(message);
    }

    // Shows a normal auto-hide toast message.
    public void ShowToast(string message)
    {
        ShowToastInternal(message, false);
    }

    // Shows a persistent toast for long-running connection and room-code join operations.
    public void ShowPersistentToast(string message)
    {
        ShowToastInternal(message, true);
    }

    // Clears any currently displayed persistent toast.
    public void ClearPersistentToast()
    {
        if (!persistentToastVisible)
        {
            return;
        }

        if (toastHideCoroutine != null)
        {
            StopCoroutine(toastHideCoroutine);
            toastHideCoroutine = null;
        }

        HideToastVisuals();
        persistentToastVisible = false;
    }

    // Shows toast UI directly when a root or TMP text is assigned, with SendMessage compatibility for old ToastMessage objects.
    private void ShowToastInternal(string message, bool persistent)
    {
        GameObject root = toastMessageRoot != null ? toastMessageRoot : toastMessage;
        TMP_Text text = GetToastText(root);

        if (root != null)
        {
            root.SetActive(true);
        }

        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.text = message;
        }

        if (toastMessage != null)
        {
            string[] methodNames =
            {
                "ShowToast",
                "ShowMessage",
                "Show",
                "Display"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                toastMessage.SendMessage(methodNames[i], message, SendMessageOptions.DontRequireReceiver);
            }
        }

        if (root == null && text == null && toastMessage == null)
        {
            Debug.Log("ModeSelectScene toast UI reference is missing.");
            return;
        }

        if (toastHideCoroutine != null)
        {
            StopCoroutine(toastHideCoroutine);
            toastHideCoroutine = null;
        }

        persistentToastVisible = persistent;

        if (persistent)
        {
            return;
        }

        toastHideCoroutine = StartCoroutine(HideToastAfterDelay(root, text));
    }

    // Finds an explicit TMP_Text first, then falls back to a TMP_Text on or under the toast root.
    private TMP_Text GetToastText(GameObject root)
    {
        if (toastMessageText != null)
        {
            return toastMessageText;
        }

        if (root == null)
        {
            return null;
        }

        TMP_Text directText = root.GetComponent<TMP_Text>();

        if (directText != null)
        {
            return directText;
        }

        return root.GetComponentInChildren<TMP_Text>(true);
    }

    // Hides the toast after a short delay so lobby errors do not stay on screen forever.
    private IEnumerator HideToastAfterDelay(GameObject root, TMP_Text text)
    {
        yield return new WaitForSeconds(toastDisplaySeconds);
        persistentToastVisible = false;
        HideToastVisuals(root, text);
        toastHideCoroutine = null;
    }

    private void HideToastVisuals()
    {
        GameObject root = toastMessageRoot != null ? toastMessageRoot : toastMessage;
        TMP_Text text = GetToastText(root);
        HideToastVisuals(root, text);
    }

    private void HideToastVisuals(GameObject root, TMP_Text text)
    {
        if (root != null)
        {
            root.SetActive(false);
        }
        else if (text != null)
        {
            text.gameObject.SetActive(false);
        }
    }

    // Safely shows or hides an optional panel and logs a warning if it is missing.
    private void SetPanelActive(GameObject panel, bool active, string panelName)
    {
        if (panel == null)
        {
            Debug.LogWarning(panelName + " is not assigned.");
            return;
        }

        panel.SetActive(active);
    }

    // Returns the local display name used by Photon room properties and lobby UI.
    public string GetOnlineDisplayName()
    {
        if (onlineUsernameInput != null && !string.IsNullOrWhiteSpace(onlineUsernameInput.text))
        {
            return onlineUsernameInput.text.Trim();
        }

        return "Player";
    }

    public string GetAIPrototypeDisplayName()
    {
        if (aiPrototypeUsernameInput != null && !string.IsNullOrWhiteSpace(aiPrototypeUsernameInput.text))
        {
            return aiPrototypeUsernameInput.text.Trim();
        }

        return "Player";
    }

    public string GetOnlineJoinRoomCode()
    {
        TMP_InputField input = onlineRoomCodeInput;

        if (input == null || string.IsNullOrWhiteSpace(input.text))
        {
            return string.Empty;
        }

        return input.text.Trim();
    }

    public void SetOnlineRoomCodeDisplay(string roomCode)
    {
        if (onlineRoomCodeDisplayText != null)
        {
            onlineRoomCodeDisplayText.text = "Room Code: " + (string.IsNullOrWhiteSpace(roomCode) ? "----" : roomCode);
        }
    }

    // Lets the Photon lobby layer drive the existing room slot UI without changing local/AI flows.
    public void ApplyOnlineRoomSnapshot(PlayerSetupData[] snapshot, int localSlot, bool localPlayerReady)
    {
        onlinePhotonFlowActive = true;
        hasRuntimeLocalSlotOverride = true;
        runtimeLocalPlayerSlotIndex = Mathf.Clamp(localSlot, 0, LocalPlayerCount - 1);
        localReady = localPlayerReady;

        for (int playerId = 0; playerId < LocalPlayerCount; playerId++)
        {
            roomPlayers[playerId] = null;
            roomReady[playerId] = false;

            if (snapshot == null || playerId >= snapshot.Length || snapshot[playerId] == null)
            {
                continue;
            }

            PlayerSetupData source = snapshot[playerId];
            PlayerSetupData copiedData = new PlayerSetupData(
                source.playerId,
                source.displayName,
                source.isAI,
                source.aiDifficulty
            );

            copiedData.isReady = source.isReady;
            roomPlayers[playerId] = copiedData;
            roomReady[playerId] = copiedData.isReady;
        }

        RefreshRoomUI();
        UpdateOnlineReadyButtonUI(localPlayerReady);
    }

    // Clears Photon-driven room UI state and returns this panel to its existing local prototype behavior.
    public void ClearOnlineRoomSnapshot()
    {
        ResetRoomSetup();
        UpdateOnlineReadyButtonUI(false);
        SetOnlineRoomCodeDisplay("----");
    }

    // Shared Photon/local lobby feedback entrypoint.
    public void ShowOnlineLobbyMessage(string message)
    {
        ShowLobbyMessage(message);
    }

    // Shared Photon/local legacy status text entrypoint.
    public void ShowOnlinePlaceholderMessage(string message)
    {
        ShowPlaceholderMessage(message);
    }

    private bool ShouldUsePhotonOnlineFlow()
    {
        return onlinePhotonFlowActive && photonRoomLobbyManager != null;
    }

    private bool IsAIPrototypePanelActive()
    {
        return aiPrototypePanel != null && aiPrototypePanel.activeSelf;
    }

    private bool IsOnlineModePanelActive()
    {
        return onlineModePanel != null && onlineModePanel.activeSelf;
    }

    private bool IsLegacyCombinedRoomPrototypeActive()
    {
        return onlineAIModePanel != null && onlineAIModePanel.activeSelf;
    }

    private void UpdateAIPrototypeUI()
    {
        if (aiPrototypeReadyButton != null)
        {
            aiPrototypeReadyButton.gameObject.SetActive(true);
            aiPrototypeReadyButton.interactable = true;
        }

        TMP_Text readyLabel = aiPrototypeReadyButtonText;

        if (readyLabel != null)
        {
            readyLabel.text = aiPrototypeReady ? "Unready" : "Ready";
            CacheReadyButtonTextColorIfNeeded(readyLabel);
            readyLabel.color = aiPrototypeReady ? readyButtonReadyTextColor : readyButtonNormalTextColor;
        }

        if (aiPrototypeStartButton != null)
        {
            aiPrototypeStartButton.interactable = aiPrototypeReady;
        }
    }

    private void UpdateOnlineReadyButtonUI(bool isReady)
    {
        TMP_Text readyLabel = onlineReadyButtonText;

        if (readyLabel != null)
        {
            readyLabel.text = isReady ? "Unready" : "Ready";
            CacheReadyButtonTextColorIfNeeded(readyLabel);
            readyLabel.color = isReady ? readyButtonReadyTextColor : readyButtonNormalTextColor;
        }
    }

    private void CacheReadyButtonTextColorIfNeeded(TMP_Text readyLabel)
    {
        if (cachedReadyButtonTextColor || readyLabel == null)
        {
            return;
        }

        readyButtonNormalTextColor = readyLabel.color;
        cachedReadyButtonTextColor = true;
    }

    private void SaveAIPrototypeSetupToPlayerPrefs()
    {
        string humanName = GetAIPrototypeDisplayName();
        PlayerPrefs.SetString("PlayerName_0", humanName);
        PlayerPrefs.SetInt("PlayerIsAI_0", 0);
        PlayerPrefs.SetInt("PlayerReady_0", aiPrototypeReady ? 1 : 0);
        PlayerPrefs.SetString("PlayerAIDifficulty_0", AIDifficulty.Easy.ToString());

        for (int playerId = 1; playerId < LocalPlayerCount; playerId++)
        {
            PlayerPrefs.SetString("PlayerName_" + playerId, "AI " + playerId);
            PlayerPrefs.SetInt("PlayerIsAI_" + playerId, 1);
            PlayerPrefs.SetInt("PlayerReady_" + playerId, 1);
            PlayerPrefs.SetString("PlayerAIDifficulty_" + playerId, globalAIDifficulty.ToString());
        }

        PlayerPrefs.SetString("GameMode", OnlineAIPrototypeMode);
        PlayerPrefs.Save();
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

    private void BindOptionalInputListener(TMP_InputField inputField)
    {
        if (inputField == null || inputField == usernameInput)
        {
            return;
        }

        inputField.onValueChanged.RemoveListener(OnUsernameChanged);
        inputField.onValueChanged.AddListener(OnUsernameChanged);
    }

    private void UnbindOptionalInputListener(TMP_InputField inputField)
    {
        if (inputField == null || inputField == usernameInput)
        {
            return;
        }

        inputField.onValueChanged.RemoveListener(OnUsernameChanged);
    }

    private RoomSlotUI[] GetActiveRoomSlots()
    {
        if (onlinePhotonFlowActive && onlineRoomSlots != null && onlineRoomSlots.Length > 0)
        {
            return onlineRoomSlots;
        }

        return roomSlots;
    }
}
