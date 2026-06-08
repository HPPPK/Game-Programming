/*
 * File: TutorialData.cs
 *
 * Purpose:
 * Implements TutorialData for the tutorial layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TutorialData within the tutorial system.
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
 * - Verify TutorialData in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialPartId
{
    PlayerInfoAndScore,
    LandTowerGold,
    Cards,
    EndTurn,
    EnemyWave
}

public enum TutorialActionType
{
    None,
    SelectLand,
    SelectOwnedLand,
    InspectTower,
    SelectCard,
    BuyLand,
    BuildTower,
    UpgradeTower,
    SellTower,
    DrawCard,
    DiscardCard,
    PlayCard,
    OpenGate,
    LockGate,
    TakeOver,
    FreezeClaim,
    StealCard,
    TradeHands,
    Disrupt,
    PowerBoost,
    PlaceShockTrap,
    EndTurnClicked,
    WaveStarted,
    WaveCompleted
}

[Serializable]
public class TutorialTargetBinding
{
    public string targetId;
    public GameObject targetObject;
}

[Serializable]
public class TutorialStep
{
    public TutorialPartId partId;
    public string stepId;

    [TextArea(2, 6)]
    public string messageText;

    public string highlightTargetId;
    public GameObject highlightTargetObject;
    public bool requireExactTarget;
    public bool requiresPlayerAction;
    public TutorialActionType expectedActionType = TutorialActionType.None;
    public List<TutorialActionType> allowedActionTypes = new List<TutorialActionType>();
    public int tutorialEnemySpawnCount;
    public float fallbackAutoAdvanceSeconds;
    public bool hideTutorialUIOnEnter;

    [TextArea(1, 3)]
    public string blockedMessageOverride;
}

[Serializable]
public class TutorialPartDefinition
{
    public TutorialPartId partId;
    public string displayTitle;
    public List<TutorialStep> steps = new List<TutorialStep>();
}
