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
    public CastleBase linkedCastle;
    public CardDrawManager cardDrawManager;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI cardNumText;
    public TextMeshProUGUI bloodNumText;
    public int gold = 5;

    [Header("Fallback Values")]
    public int defaultCardCount = 0;

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (moneyText != null)
        {
            moneyText.text = gold.ToString();
        }

        if (cardNumText != null)
        {
            int cardCount = defaultCardCount;

            if (cardDrawManager != null)
            {
                cardCount = cardDrawManager.GetHandCardCount();
            }

            cardNumText.text = cardCount.ToString();
        }

        if (bloodNumText != null && linkedCastle != null)
        {
            bloodNumText.text = linkedCastle.currentHP.ToString();
        }
    }
}
