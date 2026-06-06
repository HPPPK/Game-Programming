/*
 * File: PhotonLobbyPropertyKeys.cs
 *
 * Purpose:
 * Stores the Photon custom property keys used by the minimum room/lobby flow.
 * Keeping these keys centralized reduces typo risk when room/player properties
 * are written in one callback and read back in another.
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
}
