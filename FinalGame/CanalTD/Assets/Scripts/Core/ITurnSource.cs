/*
 * File: ITurnSource.cs
 *
 * Purpose:
 * Provides a small shared turn-source API so gameplay systems can work with the
 * original TurnManager in GameScene or AIPrototypeTurnManager in GameScene_AIPrototype.
 */

public interface ITurnSource
{
    int CurrentPlayerId { get; }
    bool IsCurrentPlayerHuman { get; }
    bool CanHumanAct { get; }
    void EndCurrentTurn();
}
