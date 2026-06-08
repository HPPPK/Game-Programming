/*
 * File: PlayerStatusPanelUI.cs
 *
 * Purpose:
 * Implements PlayerStatusPanelUI for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PlayerStatusPanelUI within the ui system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PlayerStatusPanelUI in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;

public class PlayerStatusPanelUI : MonoBehaviour
{
    public string playerName = "Player 1";
    public PlayerResource linkedResource;
    public CastleBase linkedCastle;
    public CardDrawManager cardDrawManager;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI cardNumText;
    public TextMeshProUGUI bloodNumText;
    public TextMeshProUGUI scoreText;
    public int gold = 5;

    [Header("Fallback Values")]
    public int defaultCardCount = 0;

    [Header("Name Display")]
    public int maxNameLength = 14;
    public int truncatedNameLength = 12;
    public float shortNameFontSize = 3f;
    public float mediumNameFontSize = 2.6f;
    public float longNameFontSize = 2.1f;
    public int mediumNameLength = 8;
    public int longNameLength = 12;

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (playerNameText != null)
        {
            string displayName = GetPanelDisplayName();
            // Keep normal names readable, then let TMP shrink slightly only if the text still needs help.
            playerNameText.enableAutoSizing = true;
            playerNameText.fontSizeMax = GetNameFontSize(displayName);
            playerNameText.fontSizeMin = 1.8f;
            playerNameText.fontSize = GetNameFontSize(displayName);
            playerNameText.text = displayName;
        }

        if (moneyText != null)
        {
            int money = linkedResource != null ? linkedResource.money : gold;
            moneyText.text = money.ToString();
        }

        if (cardNumText != null)
        {
            int cardCount = linkedResource != null ? linkedResource.GetDisplayedCardCount() : defaultCardCount;

            if (linkedResource == null && cardDrawManager != null)
            {
                cardCount = cardDrawManager.GetHandCardCount();
            }

            cardNumText.text = cardCount.ToString();
        }

        if (bloodNumText != null && linkedCastle != null)
        {
            bloodNumText.text = linkedCastle.currentHP.ToString();
        }

        if (scoreText != null)
        {
            int score = linkedResource != null ? linkedResource.score : 0;
            scoreText.text = linkedResource != null && linkedResource.isEliminated ? "已淘汰" : score.ToString();
        }
    }

    public void Bind(PlayerResource resource, CastleBase castle)
    {
        linkedResource = resource;
        linkedCastle = castle;

        if (linkedResource != null)
        {
            linkedResource.statusPanel = this;
        }

        if (linkedCastle != null)
        {
            linkedCastle.statusPanel = this;
        }

        Refresh();
    }

    private string GetPanelDisplayName()
    {
        string displayName = linkedResource != null ? linkedResource.GetDisplayName() : playerName;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Player";
        }

        if (displayName.Length > maxNameLength)
        {
            int visibleLength = Mathf.Clamp(truncatedNameLength, 1, displayName.Length);
            return displayName.Substring(0, visibleLength) + "...";
        }

        return displayName;
    }

    private float GetNameFontSize(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return shortNameFontSize;
        }

        if (displayName.Length > longNameLength)
        {
            return longNameFontSize;
        }

        if (displayName.Length > mediumNameLength)
        {
            return mediumNameFontSize;
        }

        return shortNameFontSize;
    }
}
