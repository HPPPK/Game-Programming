/*
 * File: TutorialActionGate.cs
 *
 * Purpose:
 * Implements TutorialActionGate for the tutorial layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tutorial scene objects, guide overlays, or scripted onboarding helpers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TutorialActionGate within the tutorial system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify TutorialActionGate in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialActionGate : MonoBehaviour
{
    public static TutorialActionGate Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private MonoBehaviour toastMessage;

    [Header("Behavior")]
    [SerializeField] private bool guideSceneOnly = true;
    [SerializeField] private string guideSceneName = "GuideScene";
    [SerializeField] private string defaultBlockedMessage = "Follow the tutorial step first.";

    private void Awake()
    {
        Instance = this;

        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutorialManager>();
        }
    }

    public bool IsActionAllowed(TutorialActionType actionType, GameObject target)
    {
        if (!IsGateActive())
        {
            return true;
        }

        return tutorialManager != null && tutorialManager.IsActionAllowed(actionType, target);
    }

    public void NotifyBlockedAction(TutorialActionType actionType)
    {
        if (!IsGateActive())
        {
            return;
        }

        string message = tutorialManager != null
            ? tutorialManager.GetBlockedActionMessage(actionType)
            : defaultBlockedMessage;

        if (TryCallToastMethod(message))
        {
            return;
        }

        Debug.Log(message);
    }

    public static bool BlockIfNotAllowed(TutorialActionType actionType, GameObject target)
    {
        if (Instance == null || Instance.IsActionAllowed(actionType, target))
        {
            return false;
        }

        Instance.NotifyBlockedAction(actionType);
        return true;
    }

    public bool IsGateActive()
    {
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutorialManager>();
        }

        if (tutorialManager == null || !tutorialManager.IsTutorialGameplayActive)
        {
            return false;
        }

        if (!guideSceneOnly)
        {
            return true;
        }

        return SceneManager.GetActiveScene().name == guideSceneName;
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
