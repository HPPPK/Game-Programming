/*
 * File: OnlineBuildSyncData.cs
 *
 * Purpose:
 * Implements OnlineBuildSyncData for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for OnlineBuildSyncData within the networking system.
 * - Update the owning object state and react to gameplay events during play.
 * - Keep multiplayer state aligned while respecting online lifecycle guards and scene context.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Photon room/player state, network events, and authoritative sync payloads when online mode is active.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Sends, applies, or guards online sync operations without changing project-level Photon settings.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify OnlineBuildSyncData in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System;
using System.Collections.Generic;

[Serializable]
public enum OnlineBuildActionType
{
    BuyLand,
    BuildTower,
    UpgradeTower,
    SellTower
}

[Serializable]
public class OnlineBuildRequestData
{
    public OnlineBuildActionType actionType;
    public int actorPlayerId = -1;
    public int currentTurnPlayerId = -1;
    public string buildAreaId = string.Empty;
    public string towerId = string.Empty;
    public int towerTypeId = -1;
}

[Serializable]
public class OnlineBuildApplyData
{
    public OnlineBuildActionType actionType;
    public int actorPlayerId = -1;
    public int currentTurnPlayerId = -1;
    public string buildAreaId = string.Empty;
    public string towerId = string.Empty;
    public int towerTypeId = -1;
    public int ownerPlayerId = -1;
    public int level = 0;
    public int newGold = -1;
    public bool accepted = false;
    public string rejectReason = string.Empty;
}

[Serializable]
public class OnlineBuildAreaState
{
    public string buildAreaId = string.Empty;
    public bool isOwned = false;
    public int ownerPlayerId = -1;
    public bool isFrozen = false;
    public int frozenByPlayerId = -1;
    public bool frozenUntilPlayerNextTurn = false;
    public bool towerExists = false;
    public int towerOwnerPlayerId = -1;
    public int towerTypeId = -1;
    public int towerLevel = 0;
}

[Serializable]
public class OnlineBuildSnapshot
{
    public List<OnlineBuildAreaState> areas = new List<OnlineBuildAreaState>();
}
