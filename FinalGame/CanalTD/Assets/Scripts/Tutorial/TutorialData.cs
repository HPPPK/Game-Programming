using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialPartId
{
    PlayerInfoAndScore,
    LandTowerGold,
    Cards,
    EndTurn,
    EnemyWave,
    PlayersTurnsScoring
}

public enum TutorialActionType
{
    None,
    SelectLand,
    SelectOwnedLand,
    InspectTower,
    SelectCard,
    SelectTarget,
    PrepareUpgradeTower,
    PrepareSellTower,
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
    public string tutorialCardId;
    public List<string> tutorialHandCardIds = new List<string>();
    public bool prepareTutorialCardOnEnter;
    public bool resetTurnActionsOnEnter;

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
