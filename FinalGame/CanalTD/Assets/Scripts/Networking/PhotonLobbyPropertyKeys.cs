/*
 * File: PhotonLobbyPropertyKeys.cs
 *
 * Purpose:
 * Implements PhotonLobbyPropertyKeys for the networking layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PhotonLobbyPropertyKeys within the networking system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PhotonLobbyPropertyKeys in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
public static class PhotonLobbyPropertyKeys
{
    public const string PlayerId = "playerId";
    public const string PlayerName = "playerName";
    public const string Ready = "ready";
    public const string IsAI = "isAI";
    public const string SlotIndex = "slotIndex";
    public const string MatchMode = "matchMode";
    public const string RoomCode = "roomCode";
    public const string RoomKind = "roomKind";
    public const string CurrentTurnPlayerId = "currentTurnPlayerId";
    public const string CurrentRound = "currentRound";
    public const string OnlineGameActive = "onlineGameActive";
    public const string OnlineBuildSnapshot = "onlineBuildSnapshot";
    public const string OnlineCardPublicSnapshot = "onlineCardPublicSnapshot";
}
