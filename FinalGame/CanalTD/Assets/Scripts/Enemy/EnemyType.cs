/*
 * File: EnemyType.cs
 *
 * Purpose:
 * Implements EnemyType for the enemy layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyType within the enemy system.
 * - Update the owning object state and react to gameplay events during play.
 * - Support enemy spawning, pathing, targeting, combat, or wave pressure behaviour.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Moves enemies, adjusts combat results, or influences castle pressure and wave outcomes.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemyType in the scene or prefab where it is used and confirm the main happy path still works.
 */
public enum EnemyType
{
    Normal,
    Tank,
    Fast,
    Splitter,
    Boss
}
