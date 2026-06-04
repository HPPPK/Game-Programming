using UnityEngine;
using UnityEngine.UI;
public class TutorialHighlightController : MonoBehaviour
{
    private static readonly Vector2 DefaultWorldFramePadding = new Vector2(0.15f, 0.15f);
    private static readonly Vector2 DefaultWorldMessageOffsetAbove = new Vector2(0f, 0.06f);
    private static readonly Vector2 DefaultWorldMessageOffsetBelow = new Vector2(0f, -0.06f);
    private const float DefaultWorldBorderThickness = 0.08f;

    [Header("Highlight UI")]
    [SerializeField] private GameObject highlightRoot;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private RectTransform highlightArrow;
    [SerializeField] private GameObject overlayObject;

    [Header("Canvas / Camera")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Camera worldCamera;

    [Header("Message Follow")]
    [SerializeField] private RectTransform messagePanel;
    [SerializeField] private Vector2 uiMessageOffsetAbove = new Vector2(0f, 120f);
    [SerializeField] private Vector2 uiMessageOffsetBelow = new Vector2(0f, -120f);
    [SerializeField] private Vector2 worldMessageOffsetAbove = new Vector2(0f, 0.06f);
    [SerializeField] private Vector2 worldMessageOffsetBelow = new Vector2(0f, -0.06f);
    [SerializeField] private Vector2 worldMessageOffsetRight = new Vector2(0.06f, 0f);
    [SerializeField] private Vector2 worldMessageOffsetRightForLand = new Vector2(0.8f, 0f);
    [SerializeField] private float messageViewportTopLimit = 0.88f;
    [SerializeField] private float uiMessageGap = 18f;
    [SerializeField] private float worldMessageGap = 0.005f;
    [SerializeField] private float worldMessageGapForLand = 0.2f;

    [Header("Layout")]
    [SerializeField] private Vector2 uiFramePadding = new Vector2(12f, 12f);
    [SerializeField] private Vector2 worldFramePadding = new Vector2(0.15f, 0.15f);
    [SerializeField] private Vector2 minimumUIFrameSize = new Vector2(48f, 48f);
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private bool followTargetEveryFrame = true;
    [SerializeField] private bool showArrow = true;

    [Header("Border Visual")]
    [SerializeField] private Color borderColor = new Color(1f, 0.95f, 0.4f, 1f);
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private float uiBorderThickness = 6f;
    [SerializeField] private float worldBorderThickness = 0.08f;

    private GameObject currentTarget;
    private RectTransform[] dimPanels;
    private Image[] dimPanelImages;
    private RectTransform[] borderBars;
    private Image[] borderBarImages;
    private string currentTargetId;

    private void Awake()
    {
        EnsureReferences();
        SanitizeWorldSpaceSettings();
        BuildOverlayPanels();
        BuildBorderBars();
        HideHighlight();
    }

    private void LateUpdate()
    {
        if (!followTargetEveryFrame || currentTarget == null)
        {
            return;
        }

        UpdateHighlightVisual();
    }

    public void ShowHighlight(GameObject target, string targetId = null)
    {
        currentTarget = target;
        currentTargetId = targetId;
        UpdateHighlightVisual();
    }

    public void HideHighlight()
    {
        currentTarget = null;
        currentTargetId = null;

        if (highlightRoot != null)
        {
            highlightRoot.SetActive(false);
        }

        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }

        if (highlightFrame != null)
        {
            highlightFrame.gameObject.SetActive(false);
        }

        if (highlightArrow != null)
        {
            highlightArrow.gameObject.SetActive(false);
        }
    }

    private void UpdateHighlightVisual()
    {
        EnsureReferences();
        SanitizeWorldSpaceSettings();

        if (currentTarget == null || highlightFrame == null)
        {
            HideHighlight();
            return;
        }

        RectTransform canvasRect = targetCanvas != null ? targetCanvas.transform as RectTransform : null;

        if (canvasRect == null)
        {
            HideHighlight();
            return;
        }

        bool isUITarget = currentTarget.GetComponent<RectTransform>() != null;
        bool useWorldSpaceLayout = targetCanvas != null && targetCanvas.renderMode == RenderMode.WorldSpace;

        if (!TryGetTargetCanvasLocalRect(canvasRect, currentTarget, isUITarget, out Rect localRect))
        {
            HideHighlight();
            return;
        }

        Vector2 padding = useWorldSpaceLayout ? worldFramePadding : (isUITarget ? uiFramePadding : worldFramePadding);
        Vector2 localSize = localRect.size + padding;

        if (isUITarget && !useWorldSpaceLayout)
        {
            localSize.x = Mathf.Max(localSize.x, minimumUIFrameSize.x);
            localSize.y = Mathf.Max(localSize.y, minimumUIFrameSize.y);
        }

        Vector2 localCenter = localRect.center;
        Rect paddedRect = new Rect(localCenter - localSize * 0.5f, localSize);

        Debug.Log("Highlight target bounds = " + localRect);
        Debug.Log("Highlight frame rect = center:" + localCenter + " size:" + localSize);

        if (highlightRoot != null)
        {
            highlightRoot.SetActive(true);
        }

        if (overlayObject != null)
        {
            overlayObject.SetActive(true);
        }

        highlightFrame.gameObject.SetActive(true);
        highlightFrame.anchoredPosition = localCenter;
        highlightFrame.sizeDelta = localSize;

        BringForegroundElementsToFront();
        UpdateBorderBars(localSize, isUITarget, useWorldSpaceLayout);
        UpdateDimPanels(canvasRect.rect, paddedRect);
        UpdateMessagePanel(localRect, canvasRect, isUITarget, useWorldSpaceLayout);
        UpdateArrow(localRect, isUITarget, useWorldSpaceLayout);
    }

    private void EnsureReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (messagePanel == null)
        {
            TutorialMessageController controller = FindObjectOfType<TutorialMessageController>();
            if (controller != null)
            {
                messagePanel = controller.transform as RectTransform;
            }
        }
    }

    private void BuildOverlayPanels()
    {
        if (overlayObject == null)
        {
            return;
        }

        RectTransform overlayRect = overlayObject.transform as RectTransform;
        if (overlayRect == null)
        {
            return;
        }

        Image overlayImage = overlayObject.GetComponent<Image>();
        if (overlayImage != null)
        {
            overlayImage.enabled = false;
            overlayImage.sprite = null;
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = false;
        }

        dimPanels = new RectTransform[4];
        dimPanelImages = new Image[4];
        string[] names = { "DimTop", "DimBottom", "DimLeft", "DimRight" };

        for (int i = 0; i < names.Length; i++)
        {
            Transform existing = overlayRect.Find(names[i]);
            GameObject panelObject;

            if (existing != null)
            {
                panelObject = existing.gameObject;
            }
            else
            {
                panelObject = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelObject.transform.SetParent(overlayRect, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = overlayColor;
            panelImage.raycastTarget = false;

            dimPanels[i] = panelRect;
            dimPanelImages[i] = panelImage;
        }
    }

    private void BuildBorderBars()
    {
        if (highlightFrame == null)
        {
            return;
        }

        Image frameImage = highlightFrame.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.enabled = false;
            frameImage.sprite = null;
            frameImage.color = new Color(1f, 1f, 1f, 0f);
            frameImage.raycastTarget = false;
        }

        Outline frameOutline = highlightFrame.GetComponent<Outline>();
        if (frameOutline != null)
        {
            frameOutline.enabled = false;
        }

        borderBars = new RectTransform[4];
        borderBarImages = new Image[4];
        string[] names = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };

        for (int i = 0; i < names.Length; i++)
        {
            Transform existing = highlightFrame.Find(names[i]);
            GameObject barObject;

            if (existing != null)
            {
                barObject = existing.gameObject;
            }
            else
            {
                barObject = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barObject.transform.SetParent(highlightFrame, false);
            }

            RectTransform barRect = barObject.GetComponent<RectTransform>();
            Image barImage = barObject.GetComponent<Image>();
            barImage.color = borderColor;
            barImage.raycastTarget = false;

            borderBars[i] = barRect;
            borderBarImages[i] = barImage;
        }
    }

    private void UpdateBorderBars(Vector2 localSize, bool isUITarget, bool useWorldSpaceLayout)
    {
        if (borderBars == null || borderBars.Length < 4)
        {
            return;
        }

        float thickness = useWorldSpaceLayout
            ? Mathf.Max(0.03f, worldBorderThickness)
            : (isUITarget ? Mathf.Max(1f, uiBorderThickness) : Mathf.Max(0.05f, worldBorderThickness));
        float halfWidth = localSize.x * 0.5f;
        float halfHeight = localSize.y * 0.5f;

        SetBar(borderBars[0], new Vector2(0f, halfHeight - thickness * 0.5f), new Vector2(localSize.x, thickness));
        SetBar(borderBars[1], new Vector2(0f, -halfHeight + thickness * 0.5f), new Vector2(localSize.x, thickness));
        SetBar(borderBars[2], new Vector2(-halfWidth + thickness * 0.5f, 0f), new Vector2(thickness, localSize.y));
        SetBar(borderBars[3], new Vector2(halfWidth - thickness * 0.5f, 0f), new Vector2(thickness, localSize.y));
    }

    private void UpdateDimPanels(Rect canvasRect, Rect holeRect)
    {
        if (dimPanels == null || dimPanels.Length < 4)
        {
            return;
        }

        float canvasLeft = canvasRect.xMin;
        float canvasRight = canvasRect.xMax;
        float canvasTop = canvasRect.yMax;
        float canvasBottom = canvasRect.yMin;

        float holeLeft = holeRect.xMin;
        float holeRight = holeRect.xMax;
        float holeTop = holeRect.yMax;
        float holeBottom = holeRect.yMin;

        SetPanel(dimPanels[0], new Rect(canvasLeft, holeTop, canvasRect.width, Mathf.Max(0f, canvasTop - holeTop)));
        SetPanel(dimPanels[1], new Rect(canvasLeft, canvasBottom, canvasRect.width, Mathf.Max(0f, holeBottom - canvasBottom)));
        SetPanel(dimPanels[2], new Rect(canvasLeft, holeBottom, Mathf.Max(0f, holeLeft - canvasLeft), Mathf.Max(0f, holeTop - holeBottom)));
        SetPanel(dimPanels[3], new Rect(holeRight, holeBottom, Mathf.Max(0f, canvasRight - holeRight), Mathf.Max(0f, holeTop - holeBottom)));
    }

    private void UpdateMessagePanel(Rect targetRect, RectTransform canvasRect, bool isUITarget, bool useWorldSpaceLayout)
    {
        if (messagePanel == null)
        {
            return;
        }

        Vector2 targetCenter = targetRect.center;
        Vector2 desiredPosition;
        bool placeRight = useWorldSpaceLayout && ShouldPlaceMessageOnRight();
        bool placeAbove = false;

        if (placeRight)
        {
            float gap = ShouldUseLandMessageSpacing() ? worldMessageGapForLand : worldMessageGap;
            Vector2 rightOffset = ShouldUseLandMessageSpacing() ? worldMessageOffsetRightForLand : worldMessageOffsetRight;
            Rect anchorRect = targetRect;

            if (ShouldAnchorMessageToRadialMenu() &&
                TryGetRadialMenuCanvasLocalRect(canvasRect, out Rect radialMenuRect))
            {
                anchorRect = radialMenuRect;
            }

            desiredPosition = new Vector2(
                anchorRect.xMax + gap + rightOffset.x,
                anchorRect.center.y + rightOffset.y);
        }
        else
        {
            Vector2 offsetAbove = useWorldSpaceLayout ? worldMessageOffsetAbove : (isUITarget ? uiMessageOffsetAbove : worldMessageOffsetAbove);
            Vector2 offsetBelow = useWorldSpaceLayout ? worldMessageOffsetBelow : (isUITarget ? uiMessageOffsetBelow : worldMessageOffsetBelow);
            float gap = useWorldSpaceLayout ? worldMessageGap : uiMessageGap;

            Vector2 desiredAbove = new Vector2(
                targetCenter.x + offsetAbove.x,
                targetRect.yMax + gap + offsetAbove.y);
            Vector2 desiredBelow = new Vector2(
                targetCenter.x + offsetBelow.x,
                targetRect.yMin - gap + offsetBelow.y);

            Vector3 aboveViewport = RectTransformToViewport(canvasRect, desiredAbove);
            placeAbove = aboveViewport.y <= messageViewportTopLimit;
            desiredPosition = placeAbove ? desiredAbove : desiredBelow;
        }

        Vector2 anchoredPosition = desiredPosition;
        if (placeRight)
        {
            anchoredPosition.x += messagePanel.rect.width * 0.5f;
        }
        else
        {
            anchoredPosition.y += (placeAbove ? 1f : -1f) * (messagePanel.rect.height * 0.5f);
        }

        if (useWorldSpaceLayout)
        {
            messagePanel.anchoredPosition = anchoredPosition;
            return;
        }

        Vector2 clampedPosition = ClampPanelInsideCanvas(canvasRect.rect, anchoredPosition, messagePanel.rect.size);
        messagePanel.anchoredPosition = clampedPosition;
    }
    

    private void UpdateArrow(Rect targetRect, bool isUITarget, bool useWorldSpaceLayout)
    {
        if (highlightArrow == null)
        {
            return;
        }

        highlightArrow.gameObject.SetActive(showArrow);

        if (!showArrow || messagePanel == null)
        {
            return;
        }

        Vector2 from = messagePanel.anchoredPosition;
        Vector2 to = targetRect.center;
        Vector2 direction = (to - from).normalized;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        float gap = useWorldSpaceLayout ? 0.8f : (isUITarget ? 28f : 1.2f);
        Vector2 arrowPosition = to - direction * gap;
        highlightArrow.anchoredPosition = arrowPosition;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        highlightArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void BringForegroundElementsToFront()
    {
        if (overlayObject != null)
        {
            overlayObject.transform.SetAsLastSibling();
        }

        if (highlightFrame != null)
        {
            highlightFrame.SetAsLastSibling();
        }

        if (highlightArrow != null)
        {
            highlightArrow.SetAsLastSibling();
        }

        if (messagePanel != null)
        {
            messagePanel.SetAsLastSibling();
        }

        RadialTowerMenu radialMenu = FindObjectOfType<RadialTowerMenu>();
        if (radialMenu != null)
        {
            radialMenu.transform.SetAsLastSibling();
            GameObject menuRoot = radialMenu.root != null ? radialMenu.root : radialMenu.gameObject;

            if (menuRoot != null)
            {
                menuRoot.transform.SetAsLastSibling();

                if (messagePanel != null)
                {
                    messagePanel.SetAsLastSibling();
                }
            }
        }
    }

    private void SanitizeWorldSpaceSettings()
    {
        if (targetCanvas == null || targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        // GuideScene kept stale inspector values from older screen-space experiments.
        if (worldFramePadding.x > 1f || worldFramePadding.y > 1f)
        {
            worldFramePadding = DefaultWorldFramePadding;
        }

        if (Mathf.Abs(worldMessageOffsetAbove.y) > 20f || Mathf.Abs(worldMessageOffsetBelow.y) > 20f)
        {
            worldMessageOffsetAbove = DefaultWorldMessageOffsetAbove;
            worldMessageOffsetBelow = DefaultWorldMessageOffsetBelow;
        }

        if (worldBorderThickness > 1f)
        {
            worldBorderThickness = DefaultWorldBorderThickness;
        }
    }

    private Vector2 ClampPanelInsideCanvas(Rect canvasRect, Vector2 desiredPosition, Vector2 panelSize)
    {
        float halfWidth = panelSize.x * 0.5f;
        float halfHeight = panelSize.y * 0.5f;

        float x = Mathf.Clamp(desiredPosition.x, canvasRect.xMin + halfWidth, canvasRect.xMax - halfWidth);
        float y = Mathf.Clamp(desiredPosition.y, canvasRect.yMin + halfHeight, canvasRect.yMax - halfHeight);
        return new Vector2(x, y);
    }

    private bool ShouldPlaceMessageOnRight()
    {
        switch (currentTargetId)
        {
            case "ClaimableLand":
            case "PublicBuildArea":
            case "TowerBuildArea":
            case "BuiltTower":
            case "UpgradeButton":
            case "ConfirmButton":
            case "PlayerInfoPanel":
            case "PlayerNameDisplay":
            case "GoldDisplay":
            case "HPDisplay":
            case "CardCountDisplay":
            case "ScoreDisplay":
                return true;
            default:
                return false;
        }
    }

    private bool ShouldUseLandMessageSpacing()
    {
        return currentTargetId == "ClaimableLand" ||
               currentTargetId == "PublicBuildArea" ||
               currentTargetId == "TowerBuildArea" ||
               currentTargetId == "BuiltTower" ||
               currentTargetId == "UpgradeButton" ||
               currentTargetId == "ConfirmButton";
    }

    private bool ShouldAnchorMessageToRadialMenu()
    {
        return currentTargetId == "ClaimableLand" ||
               currentTargetId == "TowerBuildArea" ||
               currentTargetId == "BuiltTower" ||
               currentTargetId == "UpgradeButton" ||
               currentTargetId == "ConfirmButton";
    }

    private bool TryGetRadialMenuCanvasLocalRect(RectTransform canvasRect, out Rect localRect)
    {
        localRect = default;

        if (canvasRect == null)
        {
            return false;
        }

        RadialTowerMenu radialMenu = FindObjectOfType<RadialTowerMenu>();
        if (radialMenu == null)
        {
            return false;
        }

        GameObject menuObject = radialMenu.root != null ? radialMenu.root : radialMenu.gameObject;
        if (menuObject == null || !menuObject.activeInHierarchy)
        {
            return false;
        }

        RectTransform menuRect = menuObject.GetComponent<RectTransform>();
        if (menuRect == null)
        {
            menuRect = radialMenu.GetComponent<RectTransform>();
        }

        if (menuRect == null)
        {
            return false;
        }

        Vector3[] corners = new Vector3[4];
        menuRect.GetWorldCorners(corners);
        return TryBuildCanvasLocalRectFromWorldCorners(canvasRect, corners, out localRect);
    }

    private bool TryGetTargetCanvasLocalRect(RectTransform canvasRect, GameObject target, bool isUITarget, out Rect localRect)
    {
        localRect = default;

        if (canvasRect == null || target == null)
        {
            return false;
        }

        if (isUITarget)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            return TryBuildCanvasLocalRectFromWorldCorners(canvasRect, corners, out localRect);
        }

        if (!TryGetTargetWorldCorners(target, out Vector3[] worldCorners))
        {
            return false;
        }

        return TryBuildCanvasLocalRectFromWorldCorners(canvasRect, worldCorners, out localRect);
    }

    private bool TryBuildCanvasLocalRectFromWorldCorners(RectTransform canvasRect, Vector3[] worldCorners, out Rect localRect)
    {
        localRect = default;

        if (canvasRect == null || worldCorners == null || worldCorners.Length == 0)
        {
            return false;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 localPoint3 = canvasRect.InverseTransformPoint(worldCorners[i]);
            Vector2 localPoint = new Vector2(localPoint3.x, localPoint3.y);
            minX = Mathf.Min(minX, localPoint.x);
            minY = Mathf.Min(minY, localPoint.y);
            maxX = Mathf.Max(maxX, localPoint.x);
            maxY = Mathf.Max(maxY, localPoint.y);
        }

        localRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private bool TryGetTargetWorldCorners(GameObject target, out Vector3[] corners)
    {
        corners = null;

        if (target == null)
        {
            return false;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center + worldOffset;
            Vector3 extents = bounds.extents;
            corners = new[]
            {
                center + new Vector3(-extents.x, -extents.y, 0f),
                center + new Vector3(-extents.x, extents.y, 0f),
                center + new Vector3(extents.x, extents.y, 0f),
                center + new Vector3(extents.x, -extents.y, 0f)
            };
            return true;
        }

        Collider2D collider2D = target.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            Bounds bounds = collider2D.bounds;
            Vector3 center = bounds.center + worldOffset;
            Vector3 extents = bounds.extents;
            corners = new[]
            {
                center + new Vector3(-extents.x, -extents.y, 0f),
                center + new Vector3(-extents.x, extents.y, 0f),
                center + new Vector3(extents.x, extents.y, 0f),
                center + new Vector3(extents.x, -extents.y, 0f)
            };
            return true;
        }

        return false;
    }

    private Vector3 RectTransformToViewport(RectTransform canvasRect, Vector2 anchoredPosition)
    {
        if (canvasRect == null)
        {
            return Vector3.zero;
        }

        Vector3 world = canvasRect.TransformPoint(anchoredPosition);
        Camera cameraToUse = targetCanvas != null && targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
        return cameraToUse != null ? cameraToUse.WorldToViewportPoint(world) : Vector3.zero;
    }

    private void SetPanel(RectTransform panel, Rect rect)
    {
        if (panel == null)
        {
            return;
        }

        bool visible = rect.width > 0.001f && rect.height > 0.001f;
        panel.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = rect.center;
        panel.sizeDelta = rect.size;
    }

    private void SetBar(RectTransform bar, Vector2 anchoredPosition, Vector2 size)
    {
        if (bar == null)
        {
            return;
        }

        bar.anchorMin = new Vector2(0.5f, 0.5f);
        bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = anchoredPosition;
        bar.sizeDelta = size;
        bar.gameObject.SetActive(true);
    }
}
