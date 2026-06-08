/*
 * File: OnlineTurnPermissionManager.cs
 *
 * Purpose:
 * Implements OnlineTurnPermissionManager for the networking layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for OnlineTurnPermissionManager within the networking system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Keep multiplayer state aligned while respecting online lifecycle guards and scene context.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Photon room/player state, network events, and authoritative sync payloads when online mode is active.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Sends, applies, or guards online sync operations without changing project-level Photon settings.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify OnlineTurnPermissionManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Retest both single-client and multi-client online flows after editing this script.
 */
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OnlineTurnPermissionManager
{
    private const float BlockedToastCooldownSeconds = 1f;
    private const string BlockedTurnMessage = "Wait for your turn.";
    private const string LoadingMessage = "Online turn system is still loading.";

    private static float lastBlockedToastTime = -10f;
    private static string lastBlockedToastSceneName = string.Empty;
    private static int lastAnnouncedTurnPlayerId = int.MinValue;
    private static int lastAnnouncedRound = int.MinValue;
    private static string lastAnnouncedSceneName = string.Empty;

    public static bool CanLocalPlayerAct(bool showBlockedToast = true)
    {
        if (!PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            return true;
        }

        PhotonOnlineGameSceneManager manager = PhotonOnlineGameSceneManager.Instance;

        if (manager == null || !manager.HasLiveOnlineMatchSession())
        {
            return false;
        }

        if (!manager.HasAuthoritativeTurnStateReady)
        {
            if (showBlockedToast)
            {
                TryShowBlockedToast(manager, LoadingMessage);
            }

            return false;
        }

        if (manager.LocalPlayerId < 0 ||
            manager.CurrentTurnPlayerId < 0 ||
            manager.LocalPlayerId != manager.CurrentTurnPlayerId)
        {
            if (showBlockedToast)
            {
                TryShowBlockedToast(manager, BlockedTurnMessage);
            }

            return false;
        }

        return true;
    }

    public static bool ShouldBlockLocalGameplayAction(bool showBlockedToast = true)
    {
        return !CanLocalPlayerAct(showBlockedToast);
    }

    public static void NotifyTurnStateApplied(PhotonOnlineGameSceneManager manager, int currentTurnPlayerId, int currentRound)
    {
        if (manager == null || !manager.HasLiveOnlineMatchSession())
        {
            ResetRuntimeState();
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != lastAnnouncedSceneName)
        {
            lastAnnouncedSceneName = sceneName;
            lastAnnouncedTurnPlayerId = int.MinValue;
            lastAnnouncedRound = int.MinValue;
        }

        if (currentTurnPlayerId == lastAnnouncedTurnPlayerId &&
            currentRound == lastAnnouncedRound)
        {
            return;
        }

        lastAnnouncedTurnPlayerId = currentTurnPlayerId;
        lastAnnouncedRound = currentRound;

        string message = currentTurnPlayerId == manager.LocalPlayerId
            ? "Your Turn"
            : manager.GetOnlineTurnOwnerDisplayName(currentTurnPlayerId) + "'s Turn";

        manager.ShowOnlineToast(message);
    }

    public static void ResetRuntimeState()
    {
        lastBlockedToastTime = -10f;
        lastBlockedToastSceneName = string.Empty;
        lastAnnouncedTurnPlayerId = int.MinValue;
        lastAnnouncedRound = int.MinValue;
        lastAnnouncedSceneName = string.Empty;
    }

    private static void TryShowBlockedToast(PhotonOnlineGameSceneManager manager, string message)
    {
        if (manager == null)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != lastBlockedToastSceneName)
        {
            lastBlockedToastSceneName = sceneName;
            lastBlockedToastTime = -10f;
        }

        if (Time.unscaledTime - lastBlockedToastTime < BlockedToastCooldownSeconds)
        {
            return;
        }

        lastBlockedToastTime = Time.unscaledTime;
        manager.ShowOnlineToast(message);
    }
}
