using UnityEngine;
using UnityEngine.EventSystems;

public class CursorToolManager : MonoBehaviour
{
    public static CursorToolManager Instance;

    [Header("Cursor Textures")]
    [Tooltip("Cursor textures must be imported as Texture Type Cursor, or a compatible Default texture with Read/Write Enabled, Alpha Is Transparency enabled, Generate Mip Maps disabled, and RGBA32 format.")]
    public Texture2D normalCursor;
    [Tooltip("Cursor textures must be imported as Texture Type Cursor, or a compatible Default texture with Read/Write Enabled, Alpha Is Transparency enabled, Generate Mip Maps disabled, and RGBA32 format.")]
    public Texture2D hammerCursor;

    [Header("Hot Spots")]
    public Vector2 normalHotSpot = Vector2.zero;
    public Vector2 hammerHotSpot = Vector2.zero;

    public bool isHammerMode = false;

    [Header("Mode UI")]
    public GameObject darkOverlay;
    public GameObject targetingUI;

    private enum CursorState
    {
        Default,
        Hammer
    }

    private CursorState currentCursorState = CursorState.Default;
    private bool hasAppliedCursor = false;
    private bool warnedMissingHammerCursor = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureEventSystem();
        ApplyNormalCursor();
        SetModeUI(false);
    }

    public void ToggleHammerTool()
    {
        if (isHammerMode)
        {
            ExitToolMode();
        }
        else
        {
            EnterHammerMode();
        }
    }

    public void EnterHammerMode()
    {
        if (isHammerMode && hasAppliedCursor && currentCursorState == CursorState.Hammer)
        {
            return;
        }

        isHammerMode = true;
        ApplyHammerCursor();

        if (GateTargetingManager.Instance != null)
        {
            bool enteredTargetMode = GateTargetingManager.Instance.EnterGateTargetMode(GateActionType.OpenGate);

            if (!enteredTargetMode)
            {
                ExitToolMode();
            }
        }
        else
        {
            SetModeUI(true);
        }
    }

    public void ExitToolMode()
    {
        if (!isHammerMode && hasAppliedCursor && currentCursorState == CursorState.Default)
        {
            SetModeUI(false);
            return;
        }

        isHammerMode = false;
        ApplyNormalCursor();
        SetModeUI(false);
    }

    private void SetModeUI(bool active)
    {
        ResolveModeUI();

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(active);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(active);
        }
    }

    private void ResolveModeUI()
    {
        if (darkOverlay == null)
        {
            darkOverlay = FindSceneObject("DarkOverlay");
        }

        if (targetingUI == null)
        {
            targetingUI = FindSceneObject("TargetingUI");
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];

            if (obj.name == objectName && obj.scene.IsValid())
            {
                return obj;
            }
        }

        return null;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Debug.Log("Created missing EventSystem for UI button clicks.");
    }

    private void ApplyNormalCursor()
    {
        if (hasAppliedCursor && currentCursorState == CursorState.Default)
        {
            return;
        }

        Cursor.SetCursor(normalCursor, normalHotSpot, CursorMode.Auto);
        currentCursorState = CursorState.Default;
        hasAppliedCursor = true;
    }

    private void ApplyHammerCursor()
    {
        if (hammerCursor == null)
        {
            if (!warnedMissingHammerCursor)
            {
                Debug.LogWarning(
                    "Hammer cursor texture is not assigned. Falling back to the default cursor. " +
                    "Cursor texture import settings should be: Texture Type Cursor or compatible Default texture, " +
                    "Read/Write Enabled true, Alpha Is Transparency true, Generate Mip Maps false, Format RGBA32."
                );

                warnedMissingHammerCursor = true;
            }

            ApplyNormalCursor();
            return;
        }

        if (hasAppliedCursor && currentCursorState == CursorState.Hammer)
        {
            return;
        }

        // Unity cursor texture import requirements:
        // Texture Type: Cursor, or a compatible Default texture
        // Read/Write Enabled: true
        // Alpha Is Transparency: true
        // Generate Mip Maps: false
        // Format: RGBA32
        Cursor.SetCursor(hammerCursor, hammerHotSpot, CursorMode.Auto);
        currentCursorState = CursorState.Hammer;
        hasAppliedCursor = true;
    }
}
