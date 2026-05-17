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
        currentAP = maxAP;
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
        return !hasDrawnCard;
    }

    public bool TryConsumeDraw()
    {
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
        return !hasPlayedCard && HasEnoughAP(1);
    }

    public bool TryConsumePlayCard()
    {
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
        return !hasDiscardedCard;
    }

    public bool TryConsumeDiscard()
    {
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
