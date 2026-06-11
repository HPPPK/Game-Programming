/*
 * File: RoomSlotUI.cs
 *
 * Purpose:
 * Implements RoomSlotUI for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for RoomSlotUI within the ui system.
 * - Update the owning object state and react to gameplay events during play.
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
 * - Verify RoomSlotUI in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomSlotUI : MonoBehaviour
{
    [Header("Slot")]
    public int slotIndex;
    public ModeSelectSceneManager manager;

    [Header("Character Visual")]
    public GameObject characterVisualRoot;
    public SimpleFrameAnimator characterAnimator;

    [Header("Texts")]
    public TMP_Text slotNameText;
    public TMP_Text slotTypeText;
    public TMP_Text readyText;

    [Header("Buttons")]
    public Button removeButton;

    [Header("Background")]
    public Image slotBackgroundImage;

    /// <summary>
    /// Finds and stores room slot UI references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }

    }

    /// <summary>
    /// Removes room slot UI listeners and temporary references before destruction.
    /// </summary>
    private void OnDestroy()
    {
        if (removeButton != null)
        {
            removeButton.onClick.RemoveListener(OnRemoveButtonClicked);
        }

    }

    // Renders this slot as empty and hides interaction/character visuals.
    /// <summary>
    /// Sets empty and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetEmpty()
    {
        if (slotNameText != null)
        {
            slotNameText.text = "Empty";
        }

        if (slotTypeText != null)
        {
            slotTypeText.text = "Waiting...";
        }

        if (readyText != null)
        {
            readyText.text = "";
        }

        SetCharacterVisible(false);
        SetTypeTextVisible(true);
        SetRemoveButtonVisible(false);
    }

    // Renders the local human slot. Local humans cannot be removed, but they can toggle ready.
    /// <summary>
    /// Sets human and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetHuman(string playerName, bool isReady)
    {
        if (slotNameText != null)
        {
            slotNameText.text = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        }

        if (slotTypeText != null)
        {
            slotTypeText.text = "Player";
        }

        SetCharacterVisible(true);
        SetTypeTextVisible(true);
        SetRemoveButtonVisible(false);
        SetReady(isReady);
    }

    // Renders an AI slot. Difficulty is displayed here but controlled by one shared lobby dropdown.
    /// <summary>
    /// Sets AI and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetAI(string aiName, AIDifficulty difficulty, bool isReady)
    {
        if (slotNameText != null)
        {
            slotNameText.text = string.IsNullOrWhiteSpace(aiName) ? "AI Player" : aiName;
        }

        if (slotTypeText != null)
        {
            slotTypeText.text = "AI " + difficulty;
        }

        SetCharacterVisible(true);
        SetTypeTextVisible(true);
        SetRemoveButtonVisible(true);
        SetReady(true);
    }

    // Renders an online-mode empty slot using only the final product fields.
    /// <summary>
    /// Sets online empty and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetOnlineEmpty()
    {
        if (slotNameText != null)
        {
            slotNameText.text = "Empty";
        }

        if (readyText != null)
        {
            readyText.text = "Waiting";
        }

        SetCharacterVisible(false);
        SetTypeTextVisible(false);
        SetRemoveButtonVisible(false);
    }

    // Renders one online player slot with only name and ready/waiting state.
    /// <summary>
    /// Sets online player and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetOnlinePlayer(string playerName, bool isReady)
    {
        if (slotNameText != null)
        {
            slotNameText.text = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        }

        if (readyText != null)
        {
            readyText.text = isReady ? "Ready" : "Waiting";
        }

        SetCharacterVisible(true);
        SetTypeTextVisible(false);
        SetRemoveButtonVisible(false);
    }

    // Updates the ready text for occupied slots.
    /// <summary>
    /// Sets ready and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetReady(bool isReady)
    {
        if (readyText != null)
        {
            readyText.text = isReady ? "Ready" : "Not Ready";
        }
    }

    // Shows or hides this slot's remove button object.
    /// <summary>
    /// Sets remove button visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetRemoveButtonVisible(bool visible)
    {
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(visible);
            removeButton.interactable = visible;
        }
    }

    /// <summary>
    /// Sets type text visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetTypeTextVisible(bool visible)
    {
        if (slotTypeText != null)
        {
            slotTypeText.gameObject.SetActive(visible);
        }
    }

    // Shows or hides this slot's character visual and starts/stops frame animation.
    /// <summary>
    /// Sets character visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetCharacterVisible(bool visible)
    {
        if (characterVisualRoot != null)
        {
            characterVisualRoot.SetActive(visible);
        }

        if (characterAnimator == null)
        {
            return;
        }

        if (visible)
        {
            characterAnimator.Play();
        }
        else
        {
            characterAnimator.Stop();
        }
    }

    // Removes this slot only; it does not remove the last AI globally.
    /// <summary>
    /// Responds to on remove button clicked and updates the affected gameplay or UI systems.
    /// </summary>
    private void OnRemoveButtonClicked()
    {
        if (manager == null)
        {
            Debug.LogWarning("RoomSlotUI manager is not assigned.");
            return;
        }

        manager.RemoveSlot(slotIndex);
    }

}
