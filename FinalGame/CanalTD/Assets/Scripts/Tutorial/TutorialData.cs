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
