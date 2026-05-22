/*
 * File: CastleBase.cs
 *
 * Purpose:
 * This script represents one player's castle/base health. Enemy arrival nodes
 * call TakeDamage() on this script, and the base tracks whether its HP has
 * reached zero.
 *
 * Runtime behavior:
 * - Start() initializes currentHP to maxHP.
 * - TakeDamage() subtracts HP, clamps it to zero, and triggers game-over behavior.
 * - OnGameOver() currently logs the loss and turns the castle SpriteRenderer gray.
 *
 * Inspector setup:
 * - playerName is used in debug messages.
 * - maxHP controls the starting health.
 * Dependency notes:
 * - CastleEndNode calls TakeDamage() when an enemy reaches that endpoint.
 * - CastleHealthBar reads currentHP and maxHP to update the visual HP display.
 */
using UnityEngine;

public class CastleBase : MonoBehaviour
{
    public string playerName = "Player";
    public int maxHP = 20;
    public int currentHP = 20;
    public int ownerPlayerId = 0;
    public PlayerResource ownerResource;
    public PlayerStatusPanelUI statusPanel;
    private bool eliminationHandled = false;

    void Start()
    {
        currentHP = maxHP;
        SyncPlayerNameFromOwner();
        Debug.Log(GetDisplayName() + " base ready. HP = " + currentHP);
    }

    public void TakeDamage(int damage)
    {
        if (eliminationHandled || ownerResource != null && ownerResource.isEliminated)
        {
            return;
        }

        currentHP -= damage;

        if (currentHP < 0)
        {
            currentHP = 0;
        }

        Debug.Log(GetDisplayName() + " base took " + damage + " damage. HP = " + currentHP);

        if (ownerResource != null)
        {
            ownerResource.AddScore(-damage);
        }

        if (statusPanel != null)
        {
            statusPanel.Refresh();
        }

        if (currentHP <= 0)
        {
            Debug.Log(GetDisplayName() + " GAME OVER");
            OnGameOver();
        }
    }

    public string GetDisplayName()
    {
        if (ownerResource != null)
        {
            return ownerResource.GetDisplayName();
        }

        return playerName;
    }

    public void SyncPlayerNameFromOwner()
    {
        if (ownerResource != null)
        {
            playerName = ownerResource.GetDisplayName();
        }
    }

    void OnGameOver()
    {
        if (eliminationHandled)
        {
            return;
        }

        eliminationHandled = true;
        Debug.Log(">>> " + GetDisplayName() + " LOSE <<<");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        PlayerManager manager = ownerResource != null ? ownerResource.playerManager : FindObjectOfType<PlayerManager>();

        if (manager != null)
        {
            manager.EliminatePlayer(ownerPlayerId);
        }
    }
}
