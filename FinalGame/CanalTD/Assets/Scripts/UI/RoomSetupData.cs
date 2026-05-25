/*
 * File: RoomSetupData.cs
 *
 * Purpose:
 * Stores lightweight pre-game room slot data for the ModeSelectScene prototype.
 * This data is saved to PlayerPrefs before GameScene loads.
 */

[System.Serializable]
public class PlayerSetupData
{
    public int playerId;
    public string displayName;
    public bool isAI;
    public AIDifficulty aiDifficulty;
    public bool isReady;

    public PlayerSetupData(int playerId, string displayName, bool isAI, AIDifficulty aiDifficulty)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        this.isAI = isAI;
        this.aiDifficulty = aiDifficulty;
        isReady = isAI;
    }
}
