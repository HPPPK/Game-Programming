/*
 * File: GateOwnershipManager.cs
 *
 * Purpose:
 * Implements GateOwnershipManager for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GateOwnershipManager within the card system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Handle card usage, targeting, hand state, or card-driven map interactions.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Card selections, targeting choices, turn permissions, and player hand data.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Applies card outcomes, targeting results, hand count changes, or card-related restrictions.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify GateOwnershipManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
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
        if (fallbackPlayerId >= 0)
        {
            return fallbackPlayerId;
        }

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
