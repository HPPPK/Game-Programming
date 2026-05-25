/*
 * File: TurnSourceResolver.cs
 *
 * Purpose:
 * Chooses the active turn source for the current scene. AIPrototypeTurnManager
 * has priority when it exists and is enabled; otherwise the original TurnManager
 * remains the source for the normal local GameScene.
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
