/*
 * File: ShockTrapTargetingManager.cs
 *
 * Purpose:
 * Implements ShockTrapTargetingManager for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ShockTrapTargetingManager within the card system.
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
 * - Verify ShockTrapTargetingManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
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

    /// <summary>
    /// Registers trap so later callbacks, lookups, or sync messages can use it.
    /// </summary>
    public static void RegisterTrap(Transform routeNode, ShockTrap trap)
    {
        if (routeNode == null || trap == null)
        {
            return;
        }

        occupiedRouteNodes[routeNode] = trap;
    }

    /// <summary>
    /// Unregisters trap so old callbacks or duplicate listeners cannot fire.
    /// </summary>
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

    /// <summary>
    /// Finds and stores shock trap targeting manager references before scene gameplay begins.
    /// </summary>
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

    /// <summary>
    /// Subscribes shock trap targeting manager to the events it needs while enabled.
    /// </summary>
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

    /// <summary>
    /// Unsubscribes shock trap targeting manager from events so disabled objects stop receiving callbacks.
    /// </summary>
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

    /// <summary>
    /// Sets up shock trap targeting manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        ExitTargetingMode();
    }

    /// <summary>
    /// Checks shock trap targeting manager input, timing, animation, or UI state once per frame.
    /// </summary>
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

    /// <summary>
    /// Handles begin shock trap targeting for card state, hand state, or targeting.
    /// </summary>
    public bool BeginShockTrapTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.PlaceShockTrap, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

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
        UpdateConfirmButtonState();
        SetRouteNodeVisualsActive(true);
        CreateRouteNodeHighlights();
        EnsurePreview();
        ShowToast("Choose a path position.");
        Debug.Log("Shock Trap targeting mode ON. RouteNodes = " + routeNodes.Count);
        return true;
    }

    /// <summary>
    /// Confirms selection and applies the selected action if it is valid.
    /// </summary>
    public void ConfirmSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (selectedNode == null)
        {
            ShowToast("Choose a path position first.");
            return;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            cardDrawManager != null)
        {
            GameObject pendingCardPrefab = cardDrawManager.GetPendingCardPrefabForTargeting();

            if (pendingCardPrefab == null)
            {
                ShowToast("Could not play this card.");
                return;
            }

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                selectedNode.gameObject.name,
                -1
            );

            if (requested)
            {
                ExitWithoutConsumingCard();
            }

            return;
        }

        ResolveShockTrapPlacement(playerManager != null ? playerManager.GetCurrentPlayerId() : 0, selectedNode, true);
    }

    /// <summary>
    /// Looks up the target for shock trap placement and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveShockTrapPlacement(int playerId, PathNode node)
    {
        /// <summary>
        /// Handles resolve shock trap placement for shock trap targeting manager.
        /// </summary>
        return ResolveShockTrapPlacement(playerId, node, false);
    }

    /// <summary>
    /// Looks up the target for shock trap placement and applies the resolved gameplay result.
    /// </summary>
    public bool ResolveShockTrapPlacement(int playerId, PathNode node, bool consumePendingCard)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.PlaceShockTrap, node != null ? node.gameObject : null))
        {
            return false;
        }

        if (node == null)
        {
            ShowToast("Choose a path position first.");
            return false;
        }

        bool trapAlreadyHere = IsRouteNodeOccupied(node);
        Debug.Log("Active traps count before placement: " + GetActiveTrapCount());
        Debug.Log("Trap already here? " + trapAlreadyHere);

        if (!IsValidRouteNode(node))
        {
            ShowToast("Trap already placed here.");
            return false;
        }

        if (shockTrapPrefab == null)
        {
            ShowToast("Shock Trap prefab is missing.");
            return false;
        }

        if (consumePendingCard)
        {
            if (cardDrawManager == null)
            {
                ShowToast("Card manager is missing.");
                return false;
            }

            if (!cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
            {
                ShowToast("Could not play this card.");
                return false;
            }
        }

        PlayerResource currentPlayer = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        Vector3 trapPosition = node.transform.position;
        GameObject trapObject = Instantiate(shockTrapPrefab, trapPosition, Quaternion.identity);
        ShockTrap shockTrap = trapObject.GetComponent<ShockTrap>();

        if (shockTrap == null)
        {
            shockTrap = trapObject.AddComponent<ShockTrap>();
        }

        shockTrap.Initialize(playerId, currentPlayer, node.transform);
        shockTrap.ApplyOwnerVisual(playerManager);
        EnsurePlacedTrapVisual(trapObject);
        Debug.Log("Shock Trap placed at node: " + node.name);

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            UnregisterTrap(node.transform, shockTrap);
            Destroy(trapObject);
            ShowToast("Could not play this card.");
            return false;
        }

        Debug.Log("Active traps count after placement: " + GetActiveTrapCount());
        string actorName = currentPlayer != null ? currentPlayer.GetDisplayName() : "Current player";
        ShowToast(actorName + " used Shock Trap on the path.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        TutorialManager.Instance?.NotifyCardPlayed(TutorialActionType.PlaceShockTrap, node != null ? node.gameObject : null);

        return true;
    }

    /// <summary>
    /// Checks whether place shock trap at node is allowed before enabling that action.
    /// </summary>
    public bool CanPlaceShockTrapAtNode(PathNode node)
    {
        return IsValidRouteNode(node) && shockTrapPrefab != null;
    }

    /// <summary>
    /// Applies shock trap placement from online to gameplay data and updates visible feedback.
    /// </summary>
    public bool ApplyShockTrapPlacementFromOnline(int playerId, PathNode node)
    {
        if (node == null || shockTrapPrefab == null)
        {
            return false;
        }

        if (IsRouteNodeOccupied(node))
        {
            return true;
        }

        if (!IsValidRouteNode(node))
        {
            return false;
        }

        PlayerResource currentPlayer = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        GameObject trapObject = Instantiate(shockTrapPrefab, node.transform.position, Quaternion.identity);
        ShockTrap shockTrap = trapObject.GetComponent<ShockTrap>();

        if (shockTrap == null)
        {
            shockTrap = trapObject.AddComponent<ShockTrap>();
        }

        shockTrap.Initialize(playerId, currentPlayer, node.transform);
        shockTrap.ApplyOwnerVisual(playerManager);
        EnsurePlacedTrapVisual(trapObject);
        Debug.Log("Shock Trap placed online at node: " + node.name);
        return true;
    }

    /// <summary>
    /// Checks whether selection is allowed before enabling that action.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether targeting is true.
    /// </summary>
    public bool IsTargeting()
    {
        return isTargeting;
    }

    /// <summary>
    /// Handles exit without consuming card for card state, hand state, or targeting.
    /// </summary>
    public void ExitWithoutConsumingCard()
    {
        ExitTargetingMode();
    }

    /// <summary>
    /// Decides whether should block online action should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    /// <summary>
    /// Handles scan route nodes for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Attempts to select hovered node and returns false if rules, resources, or references block it.
    /// </summary>
    private void TrySelectHoveredNode()
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

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
        UpdateConfirmButtonState();
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.SelectTarget, selectedNode != null ? selectedNode.gameObject : null);
        Debug.Log("Selected RouteNode: " + selectedNode.name);
    }

    /// <summary>
    /// Returns nearest route node used by card handling or target selection.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether valid route node is true.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether route node occupied is true.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether too close to active trap is true.
    /// </summary>
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

    /// <summary>
    /// Updates hover node so the display or cached state matches current gameplay data.
    /// </summary>
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

    /// <summary>
    /// Creates route node highlights and configures it for the current scene or interaction.
    /// </summary>
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

    /// <summary>
    /// Handles highlight node for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Ensures node highlight exists or is initialized before the flow continues.
    /// </summary>
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

    /// <summary>
    /// Creates generated highlight object and configures it for the current scene or interaction.
    /// </summary>
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

    /// <summary>
    /// Returns generated highlight sprite used by card handling or target selection.
    /// </summary>
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

    /// <summary>
    /// Handles restore node highlight for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Handles restore all node highlights for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Ensures preview exists or is initialized before the flow continues.
    /// </summary>
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

    /// <summary>
    /// Updates preview so the display or cached state matches current gameplay data.
    /// </summary>
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
            ApplyCurrentPlayerPreviewSprite();
            Color previewColor = color;
            previewColor.a = Mathf.Clamp01(color.a);
            previewRenderer.color = previewColor;
        }
    }

    /// <summary>
    /// Applies current player preview sprite to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyCurrentPlayerPreviewSprite()
    {
        if (playerManager == null || previewRenderer == null)
        {
            return;
        }

        Sprite previewSprite = playerManager.GetShockTrapSpriteForPlayer(playerManager.GetCurrentPlayerId());

        if (previewSprite != null)
        {
            previewRenderer.sprite = previewSprite;
        }
    }

    /// <summary>
    /// Returns mouse world position used by card handling or target selection.
    /// </summary>
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
            /// <summary>
            /// Handles vector3 for shock trap targeting manager.
            /// </summary>
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );
    }

    /// <summary>
    /// Sets targeting visuals and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetTargetingVisuals(bool active)
    {
        if (active && BuildTowerManager.Instance != null)
        {
            BuildTowerManager.Instance.HideBuildInteractionUI();
        }

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

    /// <summary>
    /// Sets normal gameplay UI enabled and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Sets overlay raycast blocking and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether pointer over targeting controls is true.
    /// </summary>
    private bool IsPointerOverTargetingControls()
    {
        /// <summary>
        /// Handles is pointer over button for shock trap targeting manager.
        /// </summary>
        return IsPointerOverButton(confirmButton) || IsPointerOverButton(cancelButton);
    }

    /// <summary>
    /// Checks the current state to decide whether pointer over button is true.
    /// </summary>
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

    /// <summary>
    /// Looks up the target for normal gameplay UI and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Handles exit targeting mode for card state, hand state, or targeting.
    /// </summary>
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
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// Updates confirm button state so the display or cached state matches current gameplay data.
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null)
        {
            return;
        }

        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialGameplayActive)
        {
            confirmButton.interactable = true;
            return;
        }

        confirmButton.interactable = isTargeting && selectedNode != null;
    }

    /// <summary>
    /// Sets route node visuals active and immediately updates the related state, UI, or visuals.
    /// </summary>
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

    /// <summary>
    /// Ensures placed trap visual exists or is initialized before the flow continues.
    /// </summary>
    private void EnsurePlacedTrapVisual(GameObject trapObject)
    {
        SpriteRenderer renderer = trapObject != null ? trapObject.GetComponentInChildren<SpriteRenderer>() : null;

        if (renderer == null)
        {
            Debug.LogWarning("ShockTrap prefab has no SpriteRenderer.");
            return;
        }

        renderer.sortingOrder = trapSortingOrder;
    }

    /// <summary>
    /// Returns active trap count used by card handling or target selection.
    /// </summary>
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

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
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
