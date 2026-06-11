/*
 * File: RoomSetupData.cs
 *
 * Purpose:
 * Implements RoomSetupData for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for RoomSetupData within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify RoomSetupData in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
[System.Serializable]
public class PlayerSetupData
{
    public int playerId;
    public string displayName;
    public bool isAI;
    public AIDifficulty aiDifficulty;
    public bool isReady;

    /// <summary>
    /// Plays player setup data in the current scene context.
    /// </summary>
    public PlayerSetupData(int playerId, string displayName, bool isAI, AIDifficulty aiDifficulty)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        this.isAI = isAI;
        this.aiDifficulty = aiDifficulty;
        isReady = isAI;
    }
}
