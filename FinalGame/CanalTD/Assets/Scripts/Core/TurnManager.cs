/*
 * CanalTD - TurnManager
 *
 * Purpose:
 * Controls the active player turn and per-turn card action permissions for the main match flow.
 *
 * Attached GameObject:
 * Configured in the Unity scene / Inspector as part of the core gameplay manager setup.
 *
 * Main responsibilities:
 * - Track current player, round/turn state, and card action flags.
 * - Reset draw, play, discard, and gate-action flags at turn boundaries.
 * - Provide permission checks used by card, gate, tower, and UI systems.
 * - Apply authoritative online turn updates when online mode is active.
 *
 * Inputs:
 * - Button/UI calls for turn progression and player actions.
 * - Runtime calls from card, gate, tower, AI, and online synchronization systems.
 *
 * Outputs / effects:
 * - Updates turn state and card action availability.
 * - Triggers UI feedback through connected scene managers.
 *
 * Authorship / assistance:
 * Game design, Unity implementation, integration, and final documentation were developed by Jingyu Pan for an individual coursework submission. AI assistance was used as disclosed in the project documentation.
 *
 * Testing notes:
 * - Manually verify turn start/end, draw/play/discard/gate flags, Disrupt card blocking, UI feedback, and invalid-action feedback in Local and AI modes.
 */
using System.Reflection;
using UnityEngine;

public class TurnManager : MonoBehaviour, ITurnSource
{
    public static TurnManager Instance;

    [Header("Player")]
    public int currentPlayerId = 0;

    [Header("Legacy Action Points Compatibility")]
    public int maxAP = 2;
    public int currentAP = 2;

    [Header("Turn Limits")]
    public bool hasDrawnCard = false;
    public bool hasPlayedCard = false;
    public bool hasChangedGate = false;
    public bool hasDiscardedCard = false;
    public bool cardActionsBlockedThisTurn = false;

    [Header("UI")]
    public MonoBehaviour toastMessage;
    public PresentTheNumberUI presentTheNumberUI;

    public int CurrentPlayerId
    {
        get { return currentPlayerId; }
    }

    public bool IsCurrentPlayerHuman
    {
        get { return true; }
    }

    public bool CanHumanAct
    {
        get { return true; }
    }

    /// <summary>
    /// Finds and stores turn manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Sets up turn manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        if (FindObjectOfType<GamePhaseManager>() == null)
        {
            StartTurn();
        }
    }

    /// <summary>
    /// Starts turn and enables its related gameplay flow.
    /// </summary>
    public void StartTurn()
    {
        StartTurn(false);
    }

    /// <summary>
    /// Starts turn and enables its related gameplay flow.
    /// </summary>
    public void StartTurn(bool disruptedThisTurn)
    {
        ClearExpiredFreezeClaimsForPlayer(currentPlayerId);

        currentAP = maxAP;
        cardActionsBlockedThisTurn = disruptedThisTurn;

        hasDrawnCard = false;
        hasPlayedCard = false;
        hasChangedGate = false;
        hasDiscardedCard = false;

        Debug.Log("Start turn. Card actions blocked=" + cardActionsBlockedThisTurn);
    }

    /// <summary>
    /// Clears expired freeze claims for player and removes its temporary gameplay or visual effect.
    /// </summary>
    private void ClearExpiredFreezeClaimsForPlayer(int playerId)
    {
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>();
        int clearedCount = 0;

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null ||
                !buildArea.isFrozenOrSealed ||
                !buildArea.frozenUntilPlayerNextTurn ||
                buildArea.frozenByPlayerId != playerId)
            {
                continue;
            }

            buildArea.ClearFreeze();
            clearedCount++;
        }

        if (clearedCount > 0)
        {
            Debug.Log("Cleared expired Freeze Claim tiles for playerId=" + playerId + ", count=" + clearedCount);
        }
    }

    /// <summary>
    /// Handles end turn for this gameplay system.
    /// </summary>
    public void EndTurn()
    {
        EndCurrentTurn();
    }

    /// <summary>
    /// Handles end current turn for this gameplay system.
    /// </summary>
    public void EndCurrentTurn()
    {
        ITurnSource activeTurnSource = TurnSourceResolver.GetActiveTurnSource(this);

        if (!object.ReferenceEquals(activeTurnSource, this))
        {
            activeTurnSource.EndCurrentTurn();
            return;
        }

        StartTurn();
    }

    /// <summary>
    /// Checks whether draw card is allowed before enabling that action.
    /// </summary>
    public bool CanDrawCard()
    {
        return !cardActionsBlockedThisTurn && !hasDrawnCard;
    }

    /// <summary>
    /// Attempts to consume draw and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumeDraw()
    {
        if (cardActionsBlockedThisTurn)
        {
            ShowToast("You cannot use cards while disrupted.");
            return false;
        }

        if (hasDrawnCard)
        {
            ShowToast("You already drew this turn.");
            return false;
        }

        hasDrawnCard = true;
        return true;
    }

    /// <summary>
    /// Applies authoritative draw consumed to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyAuthoritativeDrawConsumed()
    {
        hasDrawnCard = true;
    }

    /// <summary>
    /// Checks whether play card is allowed before enabling that action.
    /// </summary>
    public bool CanPlayCard()
    {
        return !cardActionsBlockedThisTurn && !hasPlayedCard;
    }

    /// <summary>
    /// Attempts to consume play card and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumePlayCard()
    {
        if (cardActionsBlockedThisTurn)
        {
            ShowToast("You cannot use cards while disrupted.");
            return false;
        }

        if (hasPlayedCard)
        {
            ShowToast("You can only play one card per turn.");
            return false;
        }

        hasPlayedCard = true;
        return true;
    }

    /// <summary>
    /// Applies authoritative play consumed to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyAuthoritativePlayConsumed(int apCost)
    {
        hasPlayedCard = true;
    }

    /// <summary>
    /// Checks whether change gate is allowed before enabling that action.
    /// </summary>
    public bool CanChangeGate()
    {
        return !hasChangedGate;
    }

    /// <summary>
    /// Attempts to consume gate change and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumeGateChange()
    {
        if (hasChangedGate)
        {
            ShowToast("You already changed a gate this turn.");
            return false;
        }

        hasChangedGate = true;
        return true;
    }

    /// <summary>
    /// Applies authoritative gate change consumed to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyAuthoritativeGateChangeConsumed()
    {
        hasChangedGate = true;
    }

    /// <summary>
    /// Checks whether discard card is allowed before enabling that action.
    /// </summary>
    public bool CanDiscardCard()
    {
        return !cardActionsBlockedThisTurn && !hasDiscardedCard;
    }

    /// <summary>
    /// Attempts to consume discard and returns false if rules, resources, or references block it.
    /// </summary>
    public bool TryConsumeDiscard()
    {
        if (cardActionsBlockedThisTurn)
        {
            ShowToast("You cannot use cards while disrupted.");
            return false;
        }

        if (hasDiscardedCard)
        {
            ShowToast("You already discarded this turn.");
            return false;
        }

        hasDiscardedCard = true;
        return true;
    }

    /// <summary>
    /// Applies authoritative discard consumed to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyAuthoritativeDiscardConsumed()
    {
        hasDiscardedCard = true;
    }

    /// <summary>
    /// Compatibility method for older callers. Final card rules no longer use AP.
    /// </summary>
    public bool HasEnoughAP(int cost)
    {
        return true;
    }

    /// <summary>
    /// Checks the current state to decide whether card actions blocked this turn is true.
    /// </summary>
    public bool IsCardActionsBlockedThisTurn()
    {
        return cardActionsBlockedThisTurn;
    }

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();

        if (cardManager != null)
        {
            cardManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryCallToastMethod(string message)
    {
        if (toastMessage == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "ShowMessage",
            "Show",
            "ShowToast",
            "Display",
            "ShowWarningMessage"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = toastMessage.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null
            );

            if (method == null)
            {
                continue;
            }

            method.Invoke(toastMessage, new object[] { message });
            return true;
        }

        return false;
    }
}
