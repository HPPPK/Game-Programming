/*
 * File: OnlineCardSyncModels.cs
 *
 * Purpose:
 * Implements OnlineCardSyncModels for the networking layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for OnlineCardSyncModels within the networking system.
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
 * - Verify OnlineCardSyncModels in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using System;
using System.Collections.Generic;

public enum OnlineCardActionType
{
    None = 0,
    Draw = 1,
    Discard = 2,
    Play = 3
}

[Serializable]
public class OnlineCardRequestData
{
    public OnlineCardActionType actionType;
    public int actorPlayerId;
    public int currentTurnPlayerId;
    public string cardId;
    public string cardName;
    public string targetId;
    public int targetPlayerId = -1;
    public long timestamp;
}

[Serializable]
public class OnlineCardApplyData
{
    public OnlineCardActionType actionType;
    public int actorPlayerId;
    public string cardId;
    public string cardName;
    public string targetId;
    public int targetPlayerId = -1;
    public long timestamp;
    public int newHandCount;
    public int remainingDeckCount;
    public int actorNewGold = -1;
    public bool accepted;
    public string rejectReason;
    public bool targetDisrupted;
    public string targetTileId;
    public int previousOwnerPlayerId = -1;
    public int newOwnerPlayerId = -1;
    public bool tileFrozen;
    public int frozenByPlayerId = -1;
    public bool frozenUntilPlayerNextTurn;
    public bool removeTowerFromTile;
    public string targetGateId;
    public bool previousGateOpen;
    public bool newGateOpen;
    public bool previousGateLocked;
    public bool newGateLocked;
    public string previousGateState;
    public string newGateState;
    public List<OnlinePlayerHandCountState> affectedPlayerHandCounts = new List<OnlinePlayerHandCountState>();
}

[Serializable]
public class OnlinePrivateHandStateData
{
    public int ownerPlayerId;
    public List<string> cardIds = new List<string>();
    public int handCount;
    public int remainingDeckCount;
}

[Serializable]
public class OnlinePlayerHandCountState
{
    public int playerId;
    public int handCount;
}

[Serializable]
public class OnlineCardPublicSnapshot
{
    public int remainingDeckCount;
    public List<OnlinePlayerHandCountState> playerHandCounts = new List<OnlinePlayerHandCountState>();
}
