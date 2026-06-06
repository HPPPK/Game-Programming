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
