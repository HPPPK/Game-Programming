/*
 * File: GlobalCursorManager.cs
 *
 * Purpose:
 * Replaces the system cursor with a UI cursor image that persists across scenes.
 */
using UnityEngine;
using UnityEngine.UI;

public class GlobalCursorManager : MonoBehaviour
{
    public static GlobalCursorManager Instance { get; private set; }

    [Header("Cursor UI")]
    public RectTransform cursorImage;
    public Image cursorGraphic;

    private void Awake()
    {
        // Keep exactly one global cursor object across scene loads.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Cursor.visible = false;
        MakeCursorIgnoreRaycasts();
    }

    private void Update()
    {
        Cursor.visible = false;

        if (cursorImage != null)
        {
            cursorImage.position = Input.mousePosition;
        }
    }

    // Ensures the cursor image never blocks buttons, sliders, or other UI clicks.
    private void MakeCursorIgnoreRaycasts()
    {
        if (cursorGraphic == null && cursorImage != null)
        {
            cursorGraphic = cursorImage.GetComponent<Image>();
        }

        if (cursorGraphic != null)
        {
            cursorGraphic.raycastTarget = false;
        }
    }
}
