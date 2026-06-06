using System;
using System.Collections.Generic;

[Serializable]
public enum OnlineBuildActionType
{
    BuyLand,
    BuildTower,
    UpgradeTower,
    SellTower
}

[Serializable]
public class OnlineBuildRequestData
{
    public OnlineBuildActionType actionType;
    public int actorPlayerId = -1;
    public int currentTurnPlayerId = -1;
    public string buildAreaId = string.Empty;
    public string towerId = string.Empty;
    public int towerTypeId = -1;
}

[Serializable]
public class OnlineBuildApplyData
{
    public OnlineBuildActionType actionType;
    public int actorPlayerId = -1;
    public int currentTurnPlayerId = -1;
    public string buildAreaId = string.Empty;
    public string towerId = string.Empty;
    public int towerTypeId = -1;
    public int ownerPlayerId = -1;
    public int level = 0;
    public int newGold = -1;
    public bool accepted = false;
    public string rejectReason = string.Empty;
}

[Serializable]
public class OnlineBuildAreaState
{
    public string buildAreaId = string.Empty;
    public bool isOwned = false;
    public int ownerPlayerId = -1;
    public bool towerExists = false;
    public int towerOwnerPlayerId = -1;
    public int towerTypeId = -1;
    public int towerLevel = 0;
}

[Serializable]
public class OnlineBuildSnapshot
{
    public List<OnlineBuildAreaState> areas = new List<OnlineBuildAreaState>();
}
