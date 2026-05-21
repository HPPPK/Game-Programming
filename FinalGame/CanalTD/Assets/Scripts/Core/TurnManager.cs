using System.Reflection;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Player")]
    public int currentPlayerId = 0;

    [Header("Action Points")]
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (FindObjectOfType<GamePhaseManager>() == null)
        {
            StartTurn();
        }
    }

    public void StartTurn()
    {
        StartTurn(false);
    }

    public void StartTurn(bool disruptedThisTurn)
    {
        currentAP = maxAP;
        cardActionsBlockedThisTurn = disruptedThisTurn;

        if (disruptedThisTurn)
        {
            maxAP = 1;
            currentAP = 1;
        }
        else
        {
            maxAP = 2;
            currentAP = maxAP;
        }

        hasDrawnCard = false;
        hasPlayedCard = false;
        hasChangedGate = false;
        hasDiscardedCard = false;

        Debug.Log("Start turn. AP = " + currentAP + " / " + maxAP);
    }

    public void EndTurn()
    {
        StartTurn();
    }

    public bool CanDrawCard()
    {
        return !cardActionsBlockedThisTurn && !hasDrawnCard;
    }

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

    public bool CanPlayCard()
    {
        return !cardActionsBlockedThisTurn && !hasPlayedCard && HasEnoughAP(1);
    }

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

        if (!HasEnoughAP(1))
        {
            ShowToast("Not enough AP.");
            return false;
        }

        currentAP -= 1;
        hasPlayedCard = true;
        return true;
    }

    public bool CanChangeGate()
    {
        return !hasChangedGate;
    }

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

    public bool CanDiscardCard()
    {
        return !cardActionsBlockedThisTurn && !hasDiscardedCard;
    }

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

    public bool HasEnoughAP(int cost)
    {
        return currentAP >= cost;
    }

    public bool IsCardActionsBlockedThisTurn()
    {
        return cardActionsBlockedThisTurn;
    }

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
