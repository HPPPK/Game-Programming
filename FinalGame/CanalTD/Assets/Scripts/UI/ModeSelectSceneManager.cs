/*
 * File: ModeSelectSceneManager.cs
 *
 * Purpose:
 * Controls the pre-game ModeSelectScene for CanalTD. It lets the player choose
 * the local four-player setup, stores player display names, and leaves clear
 * placeholders for future AI and online modes.
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeSelectSceneManager : MonoBehaviour
{
    [Header("Scenes")]
    public string homeSceneName = "HomeScene";
    public string gameSceneName = "GameScene";
    public string aiPrototypeSceneName = "GameScene_AIPrototype";

    [Header("Player Names")]
    public TMP_InputField[] playerNameInputs;
    public TMP_InputField usernameInput;

    [Header("Mode Panels")]
    public GameObject localModePanel;
    public GameObject onlineAIModePanel;

    [Header("Online / AI Room Prototype")]
    public RoomSlotUI[] roomSlots;
    public TMP_Text placeholderMessageText;
    public GameObject toastMessageRoot;
    public TMP_Text toastMessageText;
    public float toastDisplaySeconds = 2f;
    public GameObject toastMessage;
    public Button addAIButton;
    public Button readyButton;
    public TMP_Text readyButtonText;
    public Color readyButtonNormalTextColor = Color.white;
    public Color readyButtonReadyTextColor = new Color(0.45f, 1f, 0.45f, 1f);
    public TMP_Dropdown aiDifficultyDropdown;
    public GameObject aiDifficultyDropdownRoot;
    public int localPlayerSlotIndex = 0;

    private const int LocalPlayerCount = 4;
    private const string LocalFourPlayerMode = "Local4Player";
    private const string OnlineAIPrototypeMode = "OnlineAIPrototype";
    private readonly PlayerSetupData[] roomPlayers = new PlayerSetupData[LocalPlayerCount];
    private readonly bool[] roomReady = new bool[LocalPlayerCount];
    private AIDifficulty globalAIDifficulty = AIDifficulty.Easy;
    private bool localReady;
    private bool cachedReadyButtonTextColor;
    private bool readyButtonListenerBound;
    private Coroutine toastHideCoroutine;

    private void Awake()
    {
        ConfigureAIDifficultyDropdown();

        if (readyButtonText != null)
        {
            readyButtonNormalTextColor = readyButtonText.color;
            cachedReadyButtonTextColor = true;
        }

        BindReadyButtonListenerOnce();

        if (usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener(OnUsernameChanged);
        }

        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyDropdownChanged);
        }
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
    }

    private void Start()
    {
        // Start with both setup panels hidden so only the main mode buttons are visible.
        ClosePanels();
        ResetRoomSetup();
        UpdateReadyButtonUI();
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

    // Shows local four-player options and hides online/AI placeholder options.
    public void OpenLocalModePanel()
    {
        SetPanelActive(localModePanel, true, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
    }

    // Shows the future online/AI panel and hides local setup options.
    public void OpenOnlineAIModePanel()
    {
        SetPanelActive(onlineAIModePanel, true, "Online/AI mode panel");
        SetPanelActive(localModePanel, false, "Local mode panel");
        ResetRoomSetup();
    }

    // Hides both mode panels. This is useful when a close button should return to the clean mode choice screen.
    public void ClosePanels()
    {
        SetPanelActive(localModePanel, false, "Local mode panel");
        SetPanelActive(onlineAIModePanel, false, "Online/AI mode panel");
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

    // Placeholder for a later AI-player setup flow.
    public void AddAIPlayerPlaceholder()
    {
        AddAIPlayer();
    }

    // Placeholder for a later online multiplayer room flow.
    public void CreateRoomPlaceholder()
    {
        StartMatchmakingPlaceholder();
    }

    // Returns to the HomeScene without changing saved player names.
    public void BackToHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    // Adds the next available AI player into the first non-local empty slot.
    public void AddAIPlayer()
    {
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

    // Starts the online/AI prototype flow after checking room ready state.
    // Empty slots remain empty and are skipped by AIPrototypeTurnManager.
    public void StartOnlineAIPrototypeGame()
    {
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

    // Placeholder for future real matchmaking. This button does not enter GameScene yet.
    public void StartMatchmakingPlaceholder()
    {
        string message = "Online matchmaking is planned but not implemented yet.";
        Debug.Log(message);
        ShowPlaceholderMessage(message);
    }

    // Toggles the local human ready state using localPlayerSlotIndex, not a hard-coded slot.
    public void ToggleLocalReady()
    {
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

        RefreshRoomUI();
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

    // Resets the room to one local player in localPlayerSlotIndex and every other slot empty.
    private void ResetRoomSetup()
    {
        globalAIDifficulty = AIDifficulty.Easy;
        localReady = false;
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

    // Updates the local slot from UsernameInput, using a stable fallback when empty.
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
        if (roomSlots == null)
        {
            return;
        }

        for (int playerId = 0; playerId < roomSlots.Length && playerId < LocalPlayerCount; playerId++)
        {
            RoomSlotUI slot = roomSlots[playerId];

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
        if (roomSlots == null || playerId < 0 || playerId >= roomSlots.Length || playerId >= LocalPlayerCount)
        {
            return;
        }

        RoomSlotUI slot = roomSlots[playerId];

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
            slot.SetEmpty();
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
        bool roomFull = !HasEmptyNonLocalSlot();

        if (addAIButton != null)
        {
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
        if (aiDifficultyDropdown == null)
        {
            return;
        }

        aiDifficultyDropdown.ClearOptions();
        aiDifficultyDropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
        aiDifficultyDropdown.SetValueWithoutNotify((int)globalAIDifficulty);
        aiDifficultyDropdown.RefreshShownValue();
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

    // Returns a safe local slot index even if the Inspector value is outside the room range.
    private int GetLocalPlayerSlotIndex()
    {
        return Mathf.Clamp(localPlayerSlotIndex, 0, LocalPlayerCount - 1);
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

    // Shows or clears a simple placeholder message for planned online features.
    private void ShowPlaceholderMessage(string message)
    {
        if (placeholderMessageText != null)
        {
            placeholderMessageText.text = message;
        }
    }

    // Shows lobby feedback through placeholder text and the optional toast UI.
    private void ShowLobbyMessage(string message)
    {
        Debug.Log(message);
        ShowPlaceholderMessage(message);
        ShowToast(message);
    }

    // Shows toast UI directly when a root or TMP text is assigned, with SendMessage compatibility for old ToastMessage objects.
    private void ShowToast(string message)
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

        if (root != null)
        {
            root.SetActive(false);
        }
        else if (text != null)
        {
            text.gameObject.SetActive(false);
        }

        toastHideCoroutine = null;
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
}