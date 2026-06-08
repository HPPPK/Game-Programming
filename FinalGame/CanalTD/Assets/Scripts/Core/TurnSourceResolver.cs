/*
 * File: TurnSourceResolver.cs
 *
 * Purpose:
 * Implements TurnSourceResolver for the core layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TurnSourceResolver within the core system.
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
 * - Verify TurnSourceResolver in the scene or prefab where it is used and confirm the main happy path still works.
 */
public static class TurnSourceResolver
{
    public static ITurnSource GetActiveTurnSource(TurnManager fallbackTurnManager = null)
    {
        AIPrototypeTurnManager aiTurnManager = AIPrototypeTurnManager.ActiveInstance;

        if (aiTurnManager != null && aiTurnManager.isActiveAndEnabled)
        {
            return aiTurnManager;
        }

        if (fallbackTurnManager != null)
        {
            return fallbackTurnManager;
        }

        return TurnManager.Instance;
    }

    public static bool IsAIPrototypeActive()
    {
        AIPrototypeTurnManager aiTurnManager = AIPrototypeTurnManager.ActiveInstance;
        return aiTurnManager != null && aiTurnManager.isActiveAndEnabled;
    }
}
