/*
 * File: PlayerStatusPanelUI.cs
 *
 * Purpose:
 * This script updates one manually arranged player status panel. It does not
 * create, rename, move, or restructure UI objects. It only writes values into
 * the TextMeshProUGUI references assigned in the Inspector.
 *
 * Expected panel objects:
 * - MoneyText displays gold.
 * - cardNum displays the current hand card count.
 * - BloodNum displays the linked castle HP.
 *
 * Inspector setup:
 * - Attach this script to each PlayerStatusPanel object.
 * - Drag MoneyText into moneyText.
 * - Drag cardNum into cardNumText.
 * - Drag BloodNum into bloodNumText.
 * - Drag the matching CastleBase into linkedCastle.
 * - Drag CardDrawManager only for the current playable player panel.
 *
 * Single-player sandbox note:
 * - Only the main player's panel needs CardDrawManager for real card count.
 * - Other panels can leave cardDrawManager empty and keep the default card count.
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
            int cardCount = linkedResource != null ? linkedResource.GetHandCardCount() : defaultCardCount;

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