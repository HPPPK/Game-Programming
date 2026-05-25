/*
 * File: RoomSlotUI.cs
 *
 * Purpose:
 * Displays one Online / AI room slot. ModeSelectSceneManager owns the room data;
 * this script only renders slot state and forwards button clicks for this slot.
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

    private void Awake()
    {
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }

    }

    private void OnDestroy()
    {
        if (removeButton != null)
        {
            removeButton.onClick.RemoveListener(OnRemoveButtonClicked);
        }

    }

    // Renders this slot as empty and hides interaction/character visuals.
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
        SetRemoveButtonVisible(false);
    }

    // Renders the local human slot. Local humans cannot be removed, but they can toggle ready.
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
        SetRemoveButtonVisible(false);
        SetReady(isReady);
    }

    // Renders an AI slot. Difficulty is displayed here but controlled by one shared lobby dropdown.
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
        SetRemoveButtonVisible(true);
        SetReady(true);
    }

    // Updates the ready text for occupied slots.
    public void SetReady(bool isReady)
    {
        if (readyText != null)
        {
            readyText.text = isReady ? "Ready" : "Not Ready";
        }
    }

    // Shows or hides this slot's remove button object.
    public void SetRemoveButtonVisible(bool visible)
    {
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(visible);
            removeButton.interactable = visible;
        }
    }

    // Shows or hides this slot's character visual and starts/stops frame animation.
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
