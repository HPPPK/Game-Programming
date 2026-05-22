/*
 * File: GateOwnershipManager.cs
 *
 * Purpose:
 * Validates whether the current player is allowed to control a gate. It checks
 * linked TowerBuildArea ownership, public/claimable land rules, and temporary
 * inactive land states caused by cards such as Take Over.
 *
 * Notes:
 * This script does not animate gates directly. It only answers ownership and
 * permission questions for gate-targeting gameplay.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GateOwnershipManager : MonoBehaviour
{
    [Header("Player")]
    public int currentPlayerId = 0;
    public PlayerManager playerManager;

    [Header("Build Areas")]
    public List<TowerBuildArea> buildAreas = new List<TowerBuildArea>();

    [Header("Feedback")]
    public MonoBehaviour toastMessage;

    [Header("Fallback")]
    public bool allowUnlinkedGates = false;

    public bool CanPlayerControlGate(int playerId, GameObject gate)
    {
        playerId = GetEffectivePlayerId(playerId);

        if (gate == null)
        {
            return false;
        }

        bool foundLinkedArea = false;

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null || !buildArea.ControlsGate(gate))
            {
                continue;
            }

            foundLinkedArea = true;

            if (buildArea.CanControlGate(playerId))
            {
                return true;
            }
        }

        return !foundLinkedArea && allowUnlinkedGates;
    }

    public string GetGateBlockReason(int playerId, GameObject gate)
    {
        playerId = GetEffectivePlayerId(playerId);

        if (gate == null)
        {
            return "This gate is not controllable.";
        }

        bool foundLinkedArea = false;
        bool hasUnownedClaimableArea = false;
        bool hasOtherPlayerClaimableArea = false;
        bool hasInactiveOwnedArea = false;

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null || !buildArea.ControlsGate(gate))
            {
                continue;
            }

            foundLinkedArea = true;

            if (buildArea.CanControlGate(playerId))
            {
                return "";
            }

            if (buildArea.IsClaimable() && buildArea.IsOwnedBy(playerId) && buildArea.IsInactiveForPlayer(playerId))
            {
                hasInactiveOwnedArea = true;
            }
            else if (buildArea.IsClaimable() && buildArea.IsUnowned())
            {
                hasUnownedClaimableArea = true;
            }
            else if (buildArea.IsClaimable() && buildArea.IsOwnedByOtherPlayer(playerId))
            {
                hasOtherPlayerClaimableArea = true;
            }
        }

        if (!foundLinkedArea)
        {
            return allowUnlinkedGates ? "" : "This gate is not controllable.";
        }

        if (hasInactiveOwnedArea)
        {
            return "This land activates next turn.";
        }

        if (hasUnownedClaimableArea)
        {
            return "Buy this land before controlling this gate.";
        }

        if (hasOtherPlayerClaimableArea)
        {
            return "This gate is controlled by another player.";
        }

        return "This gate is not controllable.";
    }

    private int GetEffectivePlayerId(int fallbackPlayerId)
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerId();
        }

        return fallbackPlayerId;
    }

    public void ShowToast(string message)
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
