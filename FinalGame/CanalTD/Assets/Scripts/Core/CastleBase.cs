/*
 * File: CastleBase.cs
 *
 * Purpose:
 * Implements CastleBase for the core layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CastleBase within the core system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CastleBase in the scene or prefab where it is used and confirm the main happy path still works.
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
        LoadPlayerNameFromPlayerPrefs();
        SyncPlayerNameFromOwner();
        Debug.Log(GetDisplayName() + " base ready. HP = " + currentHP);
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }

    public int GetMaxHP()
    {
        return maxHP;
    }

    public int GetOwnerPlayerId()
    {
        return ownerResource != null ? ownerResource.playerId : ownerPlayerId;
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

    // Loads a saved name only when this castle does not have a PlayerResource supplying the display name.
    private void LoadPlayerNameFromPlayerPrefs()
    {
        if (ownerResource != null)
        {
            return;
        }

        string key = "PlayerName_" + ownerPlayerId;

        if (!PlayerPrefs.HasKey(key))
        {
            return;
        }

        string savedName = PlayerPrefs.GetString(key);

        if (!string.IsNullOrWhiteSpace(savedName))
        {
            playerName = savedName.Trim();
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
            int eliminatedPlayerId = ownerResource != null ? ownerResource.playerId : ownerPlayerId;
            manager.EliminatePlayer(eliminatedPlayerId);
        }
    }
}
