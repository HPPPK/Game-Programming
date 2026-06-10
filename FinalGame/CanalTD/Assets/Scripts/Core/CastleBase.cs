/*
 * File: CastleBase.cs
 *
 * Purpose:
 * Implements CastleBase for the core layer of CanalTD and supports the playable vertical slice of the project.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
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

    /// <summary>
    /// Sets up castle base when this scene object starts running.
    /// </summary>
    void Start()
    {
        currentHP = maxHP;
        LoadPlayerNameFromPlayerPrefs();
        SyncPlayerNameFromOwner();
        Debug.Log(GetDisplayName() + " base ready. HP = " + currentHP);
    }

    /// <summary>
    /// Returns current HP from the current scene or gameplay state.
    /// </summary>
    public int GetCurrentHP()
    {
        return currentHP;
    }

    /// <summary>
    /// Returns max HP from the current scene or gameplay state.
    /// </summary>
    public int GetMaxHP()
    {
        return maxHP;
    }

    /// <summary>
    /// Returns owner player ID from the current scene or gameplay state.
    /// </summary>
    public int GetOwnerPlayerId()
    {
        return ownerResource != null ? ownerResource.playerId : ownerPlayerId;
    }

    /// <summary>
    /// Handles take damage for this gameplay system.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (eliminationHandled || ownerResource != null && ownerResource.isEliminated)
        {
            return;
        }

        if (PhotonOnlineWaveCombatSyncManager.ShouldBlockLocalCastleDamage())
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

        PhotonOnlineWaveCombatSyncManager.NotifyCastleDamagedByMaster(this, damage);

        if (currentHP <= 0)
        {
            Debug.Log(GetDisplayName() + " GAME OVER");
            OnGameOver();
        }
    }

    /// <summary>
    /// Applies online health state to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOnlineHealthState(int syncedCurrentHP)
    {
        currentHP = Mathf.Clamp(syncedCurrentHP, 0, maxHP);

        if (statusPanel != null)
        {
            statusPanel.Refresh();
        }
    }

    /// <summary>
    /// Returns display name from the current scene or gameplay state.
    /// </summary>
    public string GetDisplayName()
    {
        if (ownerResource != null)
        {
            return ownerResource.GetDisplayName();
        }

        return playerName;
    }

    /// <summary>
    /// Handles sync player name from owner for this gameplay system.
    /// </summary>
    public void SyncPlayerNameFromOwner()
    {
        if (ownerResource != null)
        {
            playerName = ownerResource.GetDisplayName();
        }
    }

    // Loads a saved name only when this castle does not have a PlayerResource supplying the display name.
    /// <summary>
    /// Loads player name from player prefs from saved settings, room data, or scene references.
    /// </summary>
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

    /// <summary>
    /// Handles castle defeat by notifying the game flow that this player lost.
    /// </summary>
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
