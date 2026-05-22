/*
 * File: ShockTrapTargetingManager.cs
 *
 * Purpose:
 * Controls the Shock Trap targeting flow. It scans RouteNodes, creates visible
 * node highlights, previews the trap on the selected route node, and places the
 * trap only after the player presses Confirm.
 *
 * Notes:
 * This manager is separate from GateTargetingManager because Shock Trap targets
 * path nodes instead of gates. Cancel restores visuals and does not consume the
 * card.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ShockTrapTargetingManager : MonoBehaviour
{
    [Header("Managers")]
    public PlayerManager playerManager;
    public CardDrawManager cardDrawManager;
    public Camera targetCamera;
    public MonoBehaviour toastMessage;

    [Header("UI")]
    public GameObject darkOverlay;
    public GameObject targetingUI;
    public CanvasGroup normalGameplayUI;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Map Dimming")]
    public Tilemap[] tilemapsToDim;
    public SpriteRenderer[] spritesToDim;
    public Color normalMapColor = Color.white;
    public Color dimMapColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Route Nodes")]
    public Transform routeNodesParent;
    public float nodePickRadius = 0.75f;
    public float minDistanceBetweenTraps = 1.0f;
    public GameObject nodeHighlightPrefab;

    [Header("Trap Prefabs")]
    public GameObject shockTrapPrefab;
    public GameObject previewTrapPrefab;
    public Color validPreviewColor = new Color(0.4f, 1f, 1f, 0.8f);
    public Color selectedPreviewColor = new Color(1f, 0.9f, 0.1f, 1f);
    public Color nodeHighlightColor = new Color(1f, 0.9f, 0.1f, 1f);
    public int nodeHighlightSortingOrder = 1000;
    public int trapSortingOrder = 1100;

    private static readonly Dictionary<Transform, ShockTrap> occupiedRouteNodes = new Dictionary<Transform, ShockTrap>();
    private static Sprite generatedHighlightSprite;
    private readonly List<PathNode> routeNodes = new List<PathNode>();
    private readonly Dictionary<SpriteRenderer, Color> originalNodeColors = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<SpriteRenderer, int> originalNodeSortingOrders = new Dictionary<SpriteRenderer, int>();
    private readonly Dictionary<PathNode, GameObject> runtimeNodeHighlights = new Dictionary<PathNode, GameObject>();
    private GameObject previewTrap;
    private SpriteRenderer previewRenderer;
    private PathNode hoveredNode;
    private PathNode selectedNode;
    private bool isTargeting = false;

    public static void RegisterTrap(Transform routeNode, ShockTrap trap)
    {
        if (routeNode == null || trap == null)
        {
            return;
        }

        occupiedRouteNodes[routeNode] = trap;
    }

    public static void UnregisterTrap(Transform routeNode, ShockTrap trap)
    {
        if (routeNode == null)
        {
            return;
        }

        if (occupiedRouteNodes.TryGetValue(routeNode, out ShockTrap existingTrap) &&
            (existingTrap == null || existingTrap == trap))
        {
            occupiedRouteNodes.Remove(routeNode);
        }
    }

    private void Awake()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelSelection);
        }
    }

    private void OnDisable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmSelection);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelSelection);
        }
    }

    private void Start()
    {
        ExitTargetingMode();
    }

    private void Update()
    {
        if (!isTargeting)
        {
            return;
        }

        PathNode nearestNode = GetNearestRouteNode(GetMouseWorldPosition());
        UpdateHoverNode(nearestNode);

        if (selectedNode != null)
        {
            UpdatePreview(selectedNode.transform.position, selectedPreviewColor);
        }
        else if (hoveredNode != null)
        {
            UpdatePreview(hoveredNode.transform.position, validPreviewColor);
        }
        else if (previewTrap != null)
        {
            previewTrap.SetActive(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectHoveredNode();
        }
    }

    public bool BeginShockTrapTargeting()
    {
        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.ExitHammerMode();
        }

        selectedNode = null;
        hoveredNode = null;
        routeNodes.Clear();
        originalNodeColors.Clear();
        originalNodeSortingOrders.Clear();
        runtimeNodeHighlights.Clear();

        ScanRouteNodes();

        if (routeNodes.Count == 0)
        {
            ShowToast("No valid trap position.");
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);
        SetRouteNodeVisualsActive(true);
        CreateRouteNodeHighlights();
        EnsurePreview();
        ShowToast("Choose a path position.");
        Debug.Log("Shock Trap targeting mode ON. RouteNodes = " + routeNodes.Count);
        return true;
    }

    public void ConfirmSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        if (selectedNode == null)
        {
            ShowToast("Choose a path position first.");
            return;
        }

        bool trapAlreadyHere = IsRouteNodeOccupied(selectedNode);
        Debug.Log("Active traps count before placement: " + GetActiveTrapCount());
        Debug.Log("Trap already here? " + trapAlreadyHere);

        if (!IsValidRouteNode(selectedNode))
        {
            ShowToast("Trap already placed here.");
            return;
        }

        if (shockTrapPrefab == null)
        {
            ShowToast("Shock Trap prefab is missing.");
            return;
        }

        if (cardDrawManager == null)
        {
            ShowToast("Card manager is missing.");
            return;
        }

        if (!cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
        {
            ShowToast("Could not play this card.");
            return;
        }

        int currentPlayerId = playerManager != null ? playerManager.GetCurrentPlayerId() : 0;
        PlayerResource currentPlayer = playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
        Vector3 trapPosition = selectedNode.transform.position;
        GameObject trapObject = Instantiate(shockTrapPrefab, trapPosition, Quaternion.identity);
        ShockTrap shockTrap = trapObject.GetComponent<ShockTrap>();

        if (shockTrap == null)
        {
            shockTrap = trapObject.AddComponent<ShockTrap>();
        }

        shockTrap.Initialize(currentPlayerId, currentPlayer, selectedNode.transform);
        EnsurePlacedTrapVisual(trapObject);
        Debug.Log("Shock Trap placed at node: " + selectedNode.name);

        if (!cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            UnregisterTrap(selectedNode.transform, shockTrap);
            Destroy(trapObject);
            ShowToast("Could not play this card.");
            return;
        }

        Debug.Log("Active traps count after placement: " + GetActiveTrapCount());
        ShowToast("Shock trap placed.");
        ExitTargetingMode();
    }

    public void CancelSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        ExitTargetingMode();

        if (cardDrawManager != null)
        {
            cardDrawManager.CancelPendingCard();
        }
    }

    public bool IsTargeting()
    {
        return isTargeting;
    }

    public void ExitWithoutConsumingCard()
    {
        ExitTargetingMode();
    }

    private void ScanRouteNodes()
    {
        if (routeNodesParent != null)
        {
            routeNodes.AddRange(routeNodesParent.GetComponentsInChildren<PathNode>(true));
            return;
        }

        PathNode[] allNodes = FindObjectsOfType<PathNode>();

        foreach (PathNode node in allNodes)
        {
            if (node != null && node.GetComponent<CastleEndNode>() == null && node.GetComponent<EnemySpawner>() == null)
            {
                routeNodes.Add(node);
            }
        }
    }

    private void TrySelectHoveredNode()
    {
        if (IsPointerOverTargetingControls())
        {
            return;
        }

        if (hoveredNode == null)
        {
            ShowToast("Place trap on the path.");
            return;
        }

        if (!IsValidRouteNode(hoveredNode))
        {
            ShowToast("Trap already placed here.");
            return;
        }

        selectedNode = hoveredNode;
        UpdatePreview(selectedNode.transform.position, selectedPreviewColor);
        HighlightNode(selectedNode, selectedPreviewColor);
        Debug.Log("Selected RouteNode: " + selectedNode.name);
    }

    private PathNode GetNearestRouteNode(Vector3 worldPosition)
    {
        PathNode nearestNode = null;
        float nearestDistance = float.MaxValue;

        foreach (PathNode node in routeNodes)
        {
            if (node == null)
            {
                continue;
            }

            float distance = Vector2.Distance(worldPosition, node.transform.position);

            if (distance <= nodePickRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestNode = node;
            }
        }

        return nearestNode;
    }

    private bool IsValidRouteNode(PathNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (node.GetComponent<CastleEndNode>() != null || node.GetComponent<TowerBuildArea>() != null)
        {
            return false;
        }

        if (IsRouteNodeOccupied(node))
        {
            return false;
        }

        if (IsTooCloseToActiveTrap(node.transform.position))
        {
            return false;
        }

        return true;
    }

    private bool IsRouteNodeOccupied(PathNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (!occupiedRouteNodes.TryGetValue(node.transform, out ShockTrap trap))
        {
            return false;
        }

        if (trap == null || !trap.BlocksPlacement())
        {
            occupiedRouteNodes.Remove(node.transform);
            return false;
        }

        return true;
    }

    private bool IsTooCloseToActiveTrap(Vector3 worldPosition)
    {
        ShockTrap[] traps = FindObjectsOfType<ShockTrap>();

        foreach (ShockTrap trap in traps)
        {
            if (trap != null &&
                trap.BlocksPlacement() &&
                Vector2.Distance(worldPosition, trap.transform.position) < minDistanceBetweenTraps)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateHoverNode(PathNode node)
    {
        if (hoveredNode == node || selectedNode != null)
        {
            return;
        }

        if (hoveredNode != null)
        {
            HighlightNode(hoveredNode, nodeHighlightColor);
        }

        hoveredNode = node;

        if (hoveredNode != null)
        {
            HighlightNode(hoveredNode, nodeHighlightColor);
        }
    }

    private void CreateRouteNodeHighlights()
    {
        int createdCount = 0;

        foreach (PathNode node in routeNodes)
        {
            if (node == null || !IsValidRouteNode(node))
            {
                continue;
            }

            if (EnsureNodeHighlight(node) != null)
            {
                HighlightNode(node, nodeHighlightColor);
                createdCount++;
            }
        }

        Debug.Log("Route node highlights created: " + createdCount);
    }

    private void HighlightNode(PathNode node, Color highlightColor)
    {
        SpriteRenderer renderer = EnsureNodeHighlight(node);

        if (renderer == null)
        {
            return;
        }

        if (!originalNodeColors.ContainsKey(renderer))
        {
            originalNodeColors[renderer] = renderer.color;
            originalNodeSortingOrders[renderer] = renderer.sortingOrder;
        }

        Color color = highlightColor;
        color.a = 1f;
        renderer.color = color;
        renderer.sortingOrder = nodeHighlightSortingOrder;
    }

    private SpriteRenderer EnsureNodeHighlight(PathNode node)
    {
        if (node == null)
        {
            return null;
        }

        SpriteRenderer existingRenderer = node.GetComponentInChildren<SpriteRenderer>();

        if (existingRenderer != null)
        {
            return existingRenderer;
        }

        if (runtimeNodeHighlights.TryGetValue(node, out GameObject existingHighlight) && existingHighlight != null)
        {
            return existingHighlight.GetComponent<SpriteRenderer>();
        }

        GameObject highlightObject = nodeHighlightPrefab != null
            ? Instantiate(nodeHighlightPrefab, node.transform.position, Quaternion.identity, node.transform)
            : CreateGeneratedHighlightObject(node.transform);

        highlightObject.name = "ShockTrapNodeHighlight";
        SpriteRenderer renderer = highlightObject.GetComponentInChildren<SpriteRenderer>();
        runtimeNodeHighlights[node] = highlightObject;
        return renderer;
    }

    private GameObject CreateGeneratedHighlightObject(Transform parent)
    {
        GameObject highlightObject = new GameObject("ShockTrapNodeHighlight");
        highlightObject.transform.SetParent(parent);
        highlightObject.transform.localPosition = Vector3.zero;
        highlightObject.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer renderer = highlightObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetGeneratedHighlightSprite();
        return highlightObject;
    }

    private Sprite GetGeneratedHighlightSprite()
    {
        if (generatedHighlightSprite != null)
        {
            return generatedHighlightSprite;
        }

        Texture2D texture = new Texture2D(16, 16);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= 7f ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        generatedHighlightSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        return generatedHighlightSprite;
    }

    private void RestoreNodeHighlight(PathNode node)
    {
        SpriteRenderer renderer = node != null ? node.GetComponentInChildren<SpriteRenderer>() : null;

        if (renderer == null)
        {
            return;
        }

        if (originalNodeColors.TryGetValue(renderer, out Color color))
        {
            renderer.color = color;
        }

        if (originalNodeSortingOrders.TryGetValue(renderer, out int sortingOrder))
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void RestoreAllNodeHighlights()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in originalNodeColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }

        foreach (KeyValuePair<SpriteRenderer, int> entry in originalNodeSortingOrders)
        {
            if (entry.Key != null)
            {
                entry.Key.sortingOrder = entry.Value;
            }
        }

        originalNodeColors.Clear();
        originalNodeSortingOrders.Clear();

        foreach (GameObject highlight in runtimeNodeHighlights.Values)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }

        runtimeNodeHighlights.Clear();
    }

    private void EnsurePreview()
    {
        if (previewTrap != null)
        {
            previewTrap.SetActive(true);
            return;
        }

        if (previewTrapPrefab != null)
        {
            previewTrap = Instantiate(previewTrapPrefab);
        }
        else if (shockTrapPrefab != null)
        {
            previewTrap = Instantiate(shockTrapPrefab);
        }
        else
        {
            previewTrap = new GameObject("ShockTrapPreview");
        }

        previewTrap.name = "ShockTrapPreview";
        ShockTrap trap = previewTrap.GetComponent<ShockTrap>();

        if (trap != null)
        {
            trap.enabled = false;
        }

        foreach (Collider2D collider in previewTrap.GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }

        ShockTrap previewShockTrap = previewTrap.GetComponent<ShockTrap>();

        if (previewShockTrap != null)
        {
            previewShockTrap.countsAsActiveTrap = false;
        }

        previewRenderer = previewTrap.GetComponentInChildren<SpriteRenderer>();
    }

    private void UpdatePreview(Vector3 position, Color color)
    {
        EnsurePreview();

        if (previewTrap == null)
        {
            return;
        }

        previewTrap.transform.position = position;
        previewTrap.SetActive(true);

        if (previewRenderer != null)
        {
            previewRenderer.color = color;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        return targetCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );
    }

    private void SetTargetingVisuals(bool active)
    {
        SetNormalGameplayUIEnabled(!active);

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(active);
            SetOverlayRaycastBlocking(active);

            if (active)
            {
                darkOverlay.transform.SetAsLastSibling();
            }
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(active);

            if (active)
            {
                targetingUI.transform.SetAsLastSibling();
            }
        }

        foreach (Tilemap tilemap in tilemapsToDim)
        {
            if (tilemap != null)
            {
                tilemap.color = active ? dimMapColor : normalMapColor;
            }
        }

        foreach (SpriteRenderer spriteRenderer in spritesToDim)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = active ? dimMapColor : normalMapColor;
            }
        }
    }

    private void SetNormalGameplayUIEnabled(bool enabled)
    {
        ResolveNormalGameplayUI();

        if (normalGameplayUI == null)
        {
            return;
        }

        normalGameplayUI.interactable = enabled;
        normalGameplayUI.blocksRaycasts = enabled;
    }

    private void SetOverlayRaycastBlocking(bool blocking)
    {
        if (darkOverlay == null)
        {
            return;
        }

        Image overlayImage = darkOverlay.GetComponent<Image>();

        if (overlayImage != null)
        {
            overlayImage.raycastTarget = blocking;
        }
    }

    private bool IsPointerOverTargetingControls()
    {
        return IsPointerOverButton(confirmButton) || IsPointerOverButton(cancelButton);
    }

    private bool IsPointerOverButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return false;
        }

        Camera uiCamera = null;
        Canvas canvas = button.GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, uiCamera);
    }

    private void ResolveNormalGameplayUI()
    {
        if (normalGameplayUI != null)
        {
            return;
        }

        CanvasGroup[] canvasGroups = FindObjectsOfType<CanvasGroup>(true);

        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup != null && canvasGroup.name == "NormalGameplayUI")
            {
                normalGameplayUI = canvasGroup;
                return;
            }
        }
    }

    private void ExitTargetingMode()
    {
        if (previewTrap != null)
        {
            Destroy(previewTrap);
        }

        SetRouteNodeVisualsActive(false);
        RestoreAllNodeHighlights();
        previewTrap = null;
        previewRenderer = null;
        hoveredNode = null;
        selectedNode = null;
        routeNodes.Clear();
        isTargeting = false;
        SetTargetingVisuals(false);
    }

    private void SetRouteNodeVisualsActive(bool active)
    {
        if (routeNodesParent == null)
        {
            return;
        }

        foreach (Transform node in routeNodesParent)
        {
            if (node == null)
            {
                continue;
            }

            Transform visual = node.Find("Visual");

            if (visual != null)
            {
                visual.gameObject.SetActive(active);
            }
        }
    }

    private void EnsurePlacedTrapVisual(GameObject trapObject)
    {
        SpriteRenderer renderer = trapObject != null ? trapObject.GetComponentInChildren<SpriteRenderer>() : null;

        if (renderer == null)
        {
            Debug.LogWarning("ShockTrap prefab has no SpriteRenderer.");
            return;
        }

        renderer.sortingOrder = trapSortingOrder;
        Color color = renderer.color;
        color.a = 1f;
        renderer.color = color;
    }

    private int GetActiveTrapCount()
    {
        int count = 0;
        List<Transform> staleNodes = new List<Transform>();

        foreach (KeyValuePair<Transform, ShockTrap> entry in occupiedRouteNodes)
        {
            if (entry.Key == null || entry.Value == null || !entry.Value.BlocksPlacement())
            {
                staleNodes.Add(entry.Key);
                continue;
            }

            count++;
        }

        foreach (Transform staleNode in staleNodes)
        {
            occupiedRouteNodes.Remove(staleNode);
        }

        return count;
    }

    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        if (cardDrawManager != null)
        {
            cardDrawManager.ShowWarningMessage(message);
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
