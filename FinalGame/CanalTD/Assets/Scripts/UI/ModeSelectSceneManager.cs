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

    /// <summary>
    /// Finds and stores mode select scene manager references before scene gameplay begins.
    /// </summary>
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

    /// <summary>
    /// Removes mode select scene manager listeners and temporary references before destruction.
    /// </summary>
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

    /// <summary>
    /// Sets up mode select scene manager when this scene object starts running.
    /// </summary>
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
    /// <summary>
    /// Handles bind ready button listener once for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Handles open local mode panel for UI display, input, or player feedback.
    /// </summary>
    public void OpenLocalModePanel()
    {
        SetPanelActive(localModePanel, true, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
    }

    // Opens the final AI prototype setup panel with one human and fixed AI opponents.
    /// <summary>
    /// Handles open AI prototype panel for UI display, input, or player feedback.
    /// </summary>
    public void OpenAIPrototypePanel()
    {
        SetPanelActive(aiPrototypePanel, true, "AI prototype panel");
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
        ResetAIPrototypeSetup();
    }

    // Opens the final Photon online panel and lets the Photon manager auto-enter a private room.
    /// <summary>
    /// Handles open online mode panel for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Handles open online AI mode panel for UI display, input, or player feedback.
    /// </summary>
    public void OpenOnlineAIModePanel()
    {
        OpenOnlineModePanel();
    }

    // Hides both mode panels. This is useful when a close button should return to the clean mode choice screen.
    /// <summary>
    /// Handles close panels for UI display, input, or player feedback.
    /// </summary>
    public void ClosePanels()
    {
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
        SetPanelActive(onlineModePanel, false, "Online mode panel");
    }

    // Saves four local player names into PlayerPrefs, then loads the gameplay scene.
    /// <summary>
    /// Starts local four player game and enables its related gameplay flow.
    /// </summary>
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
    /// <summary>
    /// Adds AI player placeholder to the relevant list, UI, hand, deck, or gameplay state.
    /// </summary>
    public void AddAIPlayerPlaceholder()
    {
        AddAIPlayer();
    }

    // Compatibility wrapper for older mixed-room prototype buttons.
    [System.Obsolete("Use OpenOnlineModePanel() and the room-code join flow from the Online panel.")]
    /// <summary>
    /// Creates room placeholder and configures it for the current scene or interaction.
    /// </summary>
    public void CreateRoomPlaceholder()
    {
        OpenOnlineModePanel();
    }

    /// <summary>
    /// Handles close AI prototype panel for UI display, input, or player feedback.
    /// </summary>
    public void CloseAIPrototypePanel()
    {
        SetPanelActive(aiPrototypePanel, false, "AI prototype panel");
    }

    /// <summary>
    /// Handles close online mode panel for UI display, input, or player feedback.
    /// </summary>
    public void CloseOnlineModePanel()
    {
        SetPanelActive(onlineModePanel, false, "Online mode panel");

        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.HandleOnlinePanelClosed();
        }
    }

    // Returns to the HomeScene without changing saved player names.
    /// <summary>
    /// Handles back to home for UI display, input, or player feedback.
    /// </summary>
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Adds the next available AI player into the first non-local empty slot.
    // This remains only for the legacy combined room prototype.
    /// <summary>
    /// Adds AI player to the relevant list, UI, hand, deck, or gameplay state.
    /// </summary>
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
    /// <summary>
    /// Removes AI player from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
    public void RemoveAIPlayer(int playerId)
    {
        RemoveSlot(playerId);
    }

    // Removes the clicked slot only. The local human slot cannot be removed.
    /// <summary>
    /// Removes slot from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
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
    /// <summary>
    /// Removes last AI player from the scene, list, UI, hand, deck, or gameplay state.
    /// </summary>
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
    /// <summary>
    /// Starts online AI prototype game and enables its related gameplay flow.
    /// </summary>
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

    /// <summary>
    /// Starts AI prototype game and enables its related gameplay flow.
    /// </summary>
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

    /// <summary>
    /// Starts online mode game and enables its related gameplay flow.
    /// </summary>
    public void StartOnlineModeGame()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.TryStartOnlineMatch();
        }
    }

    /// <summary>
    /// Handles join online room by code for UI display, input, or player feedback.
    /// </summary>
    public void JoinOnlineRoomByCode()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.OnClickJoinRoom();
        }
    }

    // Routes old shared Ready button bindings to the formal panel-specific flow when needed.
    /// <summary>
    /// Handles toggle local ready for UI display, input, or player feedback.
    /// </summary>
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

    /// <summary>
    /// Handles toggle AI prototype ready for UI display, input, or player feedback.
    /// </summary>
    public void ToggleAIPrototypeReady()
    {
        aiPrototypeReady = !aiPrototypeReady;
        UpdateAIPrototypeUI();
        ShowPlaceholderMessage("");
    }

    /// <summary>
    /// Handles toggle online ready for UI display, input, or player feedback.
    /// </summary>
    public void ToggleOnlineReady()
    {
        if (photonRoomLobbyManager != null)
        {
            photonRoomLobbyManager.ToggleReady();
        }
    }

    // Compatibility entry for older slot buttons. Difficulty is now global, not per-slot.
    /// <summary>
    /// Handles cycle AI difficulty for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Sets AI difficulty and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetAIDifficulty(int slotIndex, AIDifficulty difficulty)
    {
        SetGlobalAIDifficulty(difficulty);
    }

    // Sets the shared AI difficulty and refreshes every AI slot display.
    /// <summary>
    /// Sets global AI difficulty and immediately updates the related state, UI, or visuals.
    /// </summary>
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
    /// <summary>
    /// Handles toggle ready for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Returns input player name used to update UI text, layout, or feedback.
    /// </summary>
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
    /// <summary>
    /// Returns player name key used to update UI text, layout, or feedback.
    /// </summary>
    private string GetPlayerNameKey(int playerId)
    {
        return "PlayerName_" + playerId;
    }

    // Resets the legacy mixed room prototype to one local player and every other slot empty.
    /// <summary>
    /// Resets room setup for a new turn, wave, player, scene, or match state.
    /// </summary>
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

    /// <summary>
    /// Resets AI prototype setup for a new turn, wave, player, scene, or match state.
    /// </summary>
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
    /// <summary>
    /// Refreshes local room player from the latest gameplay data.
    /// </summary>
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
    /// <summary>
    /// Refreshes room UI from the latest gameplay data.
    /// </summary>
    public void RefreshRoomUI()
    {
        RefreshRoomSlots();
        RefreshAddAIButton();
        UpdateReadyButtonUI();
        RefreshAIDifficultyDropdownVisibility();
    }

    // Writes the current room data to every visible RoomSlotUI.
    /// <summary>
    /// Refreshes room slots from the latest gameplay data.
    /// </summary>
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
    /// <summary>
    /// Refreshes room slot from the latest gameplay data.
    /// </summary>
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
    /// <summary>
    /// Handles count AI players for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Handles fill empty slots with AI for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Checks the current state to decide whether room full is true.
    /// </summary>
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
    /// <summary>
    /// Refreshes add AI button from the latest gameplay data.
    /// </summary>
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
    /// <summary>
    /// Handles configure AI difficulty dropdown for UI display, input, or player feedback.
    /// </summary>
    private void ConfigureAIDifficultyDropdown()
    {
        ConfigureDifficultyDropdown(aiDifficultyDropdown);
        ConfigureDifficultyDropdown(aiPrototypeDifficultyDropdown);
    }

    /// <summary>
    /// Handles configure difficulty dropdown for UI display, input, or player feedback.
    /// </summary>
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
    /// <summary>
    /// Responds to on AI difficulty dropdown changed and updates the affected gameplay or UI systems.
    /// </summary>
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
    /// <summary>
    /// Refreshes AI difficulty dropdown visibility from the latest gameplay data.
    /// </summary>
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
    /// <summary>
    /// Updates ready button UI so the display or cached state matches current gameplay data.
    /// </summary>
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
    /// <summary>
    /// Responds to on username changed and updates the affected gameplay or UI systems.
    /// </summary>
    private void OnUsernameChanged(string ignored)
    {
        RefreshLocalRoomPlayer();
        RefreshRoomSlot(GetLocalPlayerSlotIndex());
        UpdateReadyButtonUI();
        UpdateAIPrototypeUI();
    }

    // Start requires a local human and every occupied slot to be ready.
    /// <summary>
    /// Checks whether start online AI prototype game is allowed before enabling that action.
    /// </summary>
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
    /// <summary>
    /// Returns local player slot index used to update UI text, layout, or feedback.
    /// </summary>
    private int GetLocalPlayerSlotIndex()
    {
        if (hasRuntimeLocalSlotOverride)
        {
            return Mathf.Clamp(runtimeLocalPlayerSlotIndex, 0, LocalPlayerCount - 1);
        }

        return Mathf.Clamp(legacyLocalPlayerSlotIndex, 0, LocalPlayerCount - 1);
    }

    // Centralizes local slot checks so future online mode can move the local player out of slot 0.
    /// <summary>
    /// Checks the current state to decide whether local player slot is true.
    /// </summary>
    private bool IsLocalPlayerSlot(int slotIndex)
    {
        return slotIndex == GetLocalPlayerSlotIndex();
    }

    // Checks whether Add AI can place another AI without touching the local player's slot.
    /// <summary>
    /// Checks whether empty non local slot is present before the code depends on it.
    /// </summary>
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
    /// <summary>
    /// Shows room full message with the correct current context.
    /// </summary>
    private void ShowRoomFullMessage()
    {
        string message = "Room is full.";
        ShowLobbyMessage(message);
    }

    // Saves names, AI flags, and mode data for GameScene to read later.
    /// <summary>
    /// Saves room setup to player prefs so it persists after the current UI or scene update.
    /// </summary>
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
    /// <summary>
    /// Shows placeholder message with the correct current context.
    /// </summary>
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
    /// <summary>
    /// Shows lobby message with the correct current context.
    /// </summary>
    private void ShowLobbyMessage(string message)
    {
        Debug.Log(message);
        ShowPlaceholderMessage(message);
        ShowToast(message);
    }

    // Shows a normal auto-hide toast message.
    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    public void ShowToast(string message)
    {
        ShowToastInternal(message, false);
    }

    // Shows a persistent toast for long-running connection and room-code join operations.
    /// <summary>
    /// Shows persistent toast with the correct current context.
    /// </summary>
    public void ShowPersistentToast(string message)
    {
        ShowToastInternal(message, true);
    }

    // Clears any currently displayed persistent toast.
    /// <summary>
    /// Clears persistent toast and removes its temporary gameplay or visual effect.
    /// </summary>
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
    /// <summary>
    /// Shows toast internal with the correct current context.
    /// </summary>
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
    /// <summary>
    /// Returns toast text used to update UI text, layout, or feedback.
    /// </summary>
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
    /// <summary>
    /// Hides toast after delay and clears temporary visual state.
    /// </summary>
    private IEnumerator HideToastAfterDelay(GameObject root, TMP_Text text)
    {
        /// <summary>
        /// Handles wait for seconds for mode select scene manager.
        /// </summary>
        yield return new WaitForSeconds(toastDisplaySeconds);
        persistentToastVisible = false;
        HideToastVisuals(root, text);
        toastHideCoroutine = null;
    }

    /// <summary>
    /// Hides toast visuals and clears temporary visual state.
    /// </summary>
    private void HideToastVisuals()
    {
        GameObject root = toastMessageRoot != null ? toastMessageRoot : toastMessage;
        TMP_Text text = GetToastText(root);
        HideToastVisuals(root, text);
    }

    /// <summary>
    /// Hides toast visuals and clears temporary visual state.
    /// </summary>
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
    /// <summary>
    /// Sets panel active and immediately updates the related state, UI, or visuals.
    /// </summary>
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
    /// <summary>
    /// Returns online display name used to update UI text, layout, or feedback.
    /// </summary>
    public string GetOnlineDisplayName()
    {
        if (onlineUsernameInput != null && !string.IsNullOrWhiteSpace(onlineUsernameInput.text))
        {
            return onlineUsernameInput.text.Trim();
        }

        return "Player";
    }

    /// <summary>
    /// Returns AI prototype display name used to update UI text, layout, or feedback.
    /// </summary>
    public string GetAIPrototypeDisplayName()
    {
        if (aiPrototypeUsernameInput != null && !string.IsNullOrWhiteSpace(aiPrototypeUsernameInput.text))
        {
            return aiPrototypeUsernameInput.text.Trim();
        }

        return "Player";
    }

    /// <summary>
    /// Returns online join room code used to update UI text, layout, or feedback.
    /// </summary>
    public string GetOnlineJoinRoomCode()
    {
        TMP_InputField input = onlineRoomCodeInput;

        if (input == null || string.IsNullOrWhiteSpace(input.text))
        {
            return string.Empty;
        }

        return input.text.Trim();
    }

    /// <summary>
    /// Sets online room code display and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetOnlineRoomCodeDisplay(string roomCode)
    {
        if (onlineRoomCodeDisplayText != null)
        {
            onlineRoomCodeDisplayText.text = "Room Code: " + (string.IsNullOrWhiteSpace(roomCode) ? "----" : roomCode);
        }
    }

    // Lets the Photon lobby layer drive the existing room slot UI without changing local/AI flows.
    /// <summary>
    /// Applies online room snapshot to gameplay data and updates visible feedback.
    /// </summary>
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
    /// <summary>
    /// Clears online room snapshot and removes its temporary gameplay or visual effect.
    /// </summary>
    public void ClearOnlineRoomSnapshot()
    {
        ResetRoomSetup();
        UpdateOnlineReadyButtonUI(false);
        SetOnlineRoomCodeDisplay("----");
    }

    // Shared Photon/local lobby feedback entrypoint.
    /// <summary>
    /// Shows online lobby message with the correct current context.
    /// </summary>
    public void ShowOnlineLobbyMessage(string message)
    {
        ShowLobbyMessage(message);
    }

    // Shared Photon/local legacy status text entrypoint.
    /// <summary>
    /// Shows online placeholder message with the correct current context.
    /// </summary>
    public void ShowOnlinePlaceholderMessage(string message)
    {
        ShowPlaceholderMessage(message);
    }

    /// <summary>
    /// Decides whether should use Photon online flow should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldUsePhotonOnlineFlow()
    {
        return onlinePhotonFlowActive && photonRoomLobbyManager != null;
    }

    /// <summary>
    /// Checks the current state to decide whether AI prototype panel active is true.
    /// </summary>
    private bool IsAIPrototypePanelActive()
    {
        return aiPrototypePanel != null && aiPrototypePanel.activeSelf;
    }

    /// <summary>
    /// Checks the current state to decide whether online mode panel active is true.
    /// </summary>
    private bool IsOnlineModePanelActive()
    {
        return onlineModePanel != null && onlineModePanel.activeSelf;
    }

    /// <summary>
    /// Checks the current state to decide whether legacy combined room prototype active is true.
    /// </summary>
    private bool IsLegacyCombinedRoomPrototypeActive()
    {
        return onlineAIModePanel != null && onlineAIModePanel.activeSelf;
    }

    /// <summary>
    /// Updates AI prototype UI so the display or cached state matches current gameplay data.
    /// </summary>
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

    /// <summary>
    /// Updates online ready button UI so the display or cached state matches current gameplay data.
    /// </summary>
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

    /// <summary>
    /// Caches ready button text color if needed so later code can use it without scanning the scene again.
    /// </summary>
    private void CacheReadyButtonTextColorIfNeeded(TMP_Text readyLabel)
    {
        if (cachedReadyButtonTextColor || readyLabel == null)
        {
            return;
        }

        readyButtonNormalTextColor = readyLabel.color;
        cachedReadyButtonTextColor = true;
    }

    /// <summary>
    /// Saves AI prototype setup to player prefs so it persists after the current UI or scene update.
    /// </summary>
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

    /// <summary>
    /// Handles bind optional button for UI display, input, or player feedback.
    /// </summary>
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

    /// <summary>
    /// Handles unbind optional button for UI display, input, or player feedback.
    /// </summary>
    private void UnbindOptionalButton(ref bool boundFlag, Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || !boundFlag)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        boundFlag = false;
    }

    /// <summary>
    /// Handles bind optional input listener for UI display, input, or player feedback.
    /// </summary>
    private void BindOptionalInputListener(TMP_InputField inputField)
    {
        if (inputField == null || inputField == usernameInput)
        {
            return;
        }

        inputField.onValueChanged.RemoveListener(OnUsernameChanged);
        inputField.onValueChanged.AddListener(OnUsernameChanged);
    }

    /// <summary>
    /// Handles unbind optional input listener for UI display, input, or player feedback.
    /// </summary>
    private void UnbindOptionalInputListener(TMP_InputField inputField)
    {
        if (inputField == null || inputField == usernameInput)
        {
            return;
        }

        inputField.onValueChanged.RemoveListener(OnUsernameChanged);
    }

    /// <summary>
    /// Returns active room slots used to update UI text, layout, or feedback.
    /// </summary>
    private RoomSlotUI[] GetActiveRoomSlots()
    {
        if (onlinePhotonFlowActive && onlineRoomSlots != null && onlineRoomSlots.Length > 0)
        {
            return onlineRoomSlots;
        }

        return roomSlots;
    }
}
