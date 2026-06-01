/*
 * File: TileTargetingManager.cs
 *
 * Purpose:
 * Handles land/tile targeting cards such as Take Over and Freeze Claim. It
 * highlights valid TowerBuildArea targets, tracks the selected tile, and applies
 * ownership or freeze effects only after Confirm.
 *
 * Notes:
 * This manager reuses the shared dark overlay and targeting UI, but it does not
 * replace GateTargetingManager. Gate cards continue to use their own manager.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileTargetingManager : MonoBehaviour
{
    public enum TileTargetingMode
    {
        None,
        TakeOver,
        FreezeClaim
    }

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
    public Color normalColor = Color.white;
    public Color dimColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Build Areas")]
    public Transform buildAreasParent;
    public Color validHighlightColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.1f, 1f);
    public int validHighlightSortingOrder = 900;
    public int selectedSortingOrder = 1000;

    private readonly List<TowerBuildArea> highlightedAreas = new List<TowerBuildArea>();
    private readonly List<TowerBuildArea> validTargets = new List<TowerBuildArea>();
    private readonly Dictionary<TowerBuildArea, Color> originalColors = new Dictionary<TowerBuildArea, Color>();
    private readonly Dictionary<TowerBuildArea, int> originalSortingOrders = new Dictionary<TowerBuildArea, int>();
    private TowerBuildArea selectedTile;
    private TileTargetingMode currentMode = TileTargetingMode.None;
    private bool isTargeting = false;

    private void Awake()
    {
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

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTileAtMouse();
        }
    }

    public bool BeginTakeOverTargeting()
    {
        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        return BeginTargeting(TileTargetingMode.TakeOver, "No land available to take over.");
    }

    public bool BeginFreezeClaimTargeting()
    {
        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        return BeginTargeting(TileTargetingMode.FreezeClaim, "No land available to freeze.");
    }

    private bool BeginTargeting(TileTargetingMode mode, string noTargetMessage)
    {
        currentMode = mode;
        selectedTile = null;
        highlightedAreas.Clear();
        validTargets.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();

        List<TowerBuildArea> allAreas = GetBuildAreas();
        int currentPlayerId = GetCurrentPlayerId();

        foreach (TowerBuildArea buildArea in allAreas)
        {
            if (IsValidTargetForMode(buildArea, currentPlayerId, mode))
            {
                validTargets.Add(buildArea);
            }
        }

        if (validTargets.Count == 0)
        {
            ShowToast(noTargetMessage);
            currentMode = TileTargetingMode.None;
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);

        foreach (TowerBuildArea buildArea in validTargets)
        {
            if (buildArea == null)
            {
                continue;
            }

            highlightedAreas.Add(buildArea);
            StoreOriginalVisual(buildArea);
            ApplyTileVisual(buildArea, validHighlightColor, validHighlightSortingOrder, false);
        }

        return true;
    }

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

        if (currentMode == TileTargetingMode.TakeOver)
        {
            ConfirmTakeOver();
        }
        else if (currentMode == TileTargetingMode.FreezeClaim)
        {
            ConfirmFreezeClaim();
        }
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

    private void ConfirmTakeOver()
    {
        if (selectedTile == null)
        {
            ShowToast("Choose a land first.");
            return;
        }

        ResolveTakeOver(GetCurrentPlayerId(), selectedTile, true);
    }

    private void ConfirmFreezeClaim()
    {
        if (selectedTile == null)
        {
            ShowToast("Choose a land first.");
            return;
        }

        ResolveFreezeClaim(GetCurrentPlayerId(), selectedTile, true);
    }

    public bool ResolveTakeOver(int playerId, TowerBuildArea buildArea)
    {
        return ResolveTakeOver(playerId, buildArea, false);
    }

    public bool ResolveTakeOver(int playerId, TowerBuildArea buildArea, bool consumePendingCard)
    {
        if (buildArea == null)
        {
            ShowToast("Choose a land first.");
            return false;
        }

        if (buildArea.isFrozenOrSealed)
        {
            ShowToast("This land is frozen.");
            return false;
        }

        if (!IsValidTakeOverTarget(buildArea, playerId))
        {
            ShowToast("Choose an opponent-owned land.");
            return false;
        }

        PlayerResource currentPlayer = GetPlayerResource(playerId);

        if (currentPlayer == null)
        {
            ShowToast("Player resource is missing.");
            return false;
        }

        int takeOverCost = GetTakeOverCost(buildArea);

        if (!currentPlayer.CanAfford(takeOverCost) || !currentPlayer.SpendMoney(takeOverCost))
        {
            ShowToast("Not enough gold to take over this land.");
            return false;
        }

        if (consumePendingCard)
        {
            if (cardDrawManager == null || !cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
            {
                currentPlayer.AddMoney(takeOverCost);
                ShowToast("Could not play this card.");
                return false;
            }
        }

        int previousOwnerId = buildArea.ownerPlayerId;
        string previousOwnerName = playerManager != null ? playerManager.GetPlayerDisplayName(previousOwnerId) : "opponent";

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            currentPlayer.AddMoney(takeOverCost);
            ShowToast("Could not play this card.");
            return false;
        }

        buildArea.RemoveCurrentTower();
        buildArea.ownerPlayerId = playerId;
        buildArea.isOwned = true;
        buildArea.inactiveForPlayerId = playerId;
        buildArea.activatesNextTurn = true;
        buildArea.RefreshOwnershipVisual(playerManager);

        RefreshPlayerUI(playerId);
        ShowToast(currentPlayer.GetDisplayName() + " used Take Over on " + previousOwnerName + "'s land.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        return true;
    }

    public bool ResolveFreezeClaim(int playerId, TowerBuildArea buildArea)
    {
        return ResolveFreezeClaim(playerId, buildArea, false);
    }

    public bool ResolveFreezeClaim(int playerId, TowerBuildArea buildArea, bool consumePendingCard)
    {
        if (!IsValidFreezeClaimTarget(buildArea))
        {
            if (buildArea != null && buildArea.isFrozenOrSealed)
            {
                ShowToast("This land is already frozen.");
            }
            else
            {
                ShowToast("Choose a valid land.");
            }

            return false;
        }

        buildArea.FreezeForPlayer(playerId);

        if (consumePendingCard)
        {
            if (cardDrawManager == null || !cardDrawManager.CanConsumeSelectedCardAfterSuccessfulTargeting())
            {
                buildArea.ClearFreeze();
                ShowToast("Could not play this card.");
                return false;
            }

            if (!cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
            {
                buildArea.ClearFreeze();
                ShowToast("Could not play this card.");
                return false;
            }
        }

        RefreshPlayerUI(playerId);
        PlayerResource currentPlayer = GetPlayerResource(playerId);
        string actorName = currentPlayer != null ? currentPlayer.GetDisplayName() : "Current player";
        ShowToast(actorName + " used Freeze Claim on a land tile.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        return true;
    }

    private void TrySelectTileAtMouse()
    {
        if (IsPointerOverTargetingControls())
        {
            return;
        }

        TowerBuildArea clickedTile = GetTileUnderMouse();

        if (clickedTile != null && clickedTile.isFrozenOrSealed)
        {
            ShowToast(currentMode == TileTargetingMode.FreezeClaim ? "This land is already frozen." : "This land is frozen.");
            return;
        }

        if (clickedTile == null || !validTargets.Contains(clickedTile))
        {
            ShowToast(currentMode == TileTargetingMode.FreezeClaim ? "Choose a valid land." : "Choose an opponent-owned land.");
            return;
        }

        if (selectedTile != null)
        {
            ApplyTileVisual(selectedTile, validHighlightColor, validHighlightSortingOrder, false);
        }

        selectedTile = clickedTile;
        ApplyTileVisual(selectedTile, selectedColor, selectedSortingOrder, true);

        Debug.Log($"Selected tile: {selectedTile.name}");
    }

    private TowerBuildArea GetTileUnderMouse()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return null;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );

        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(worldPosition.x, worldPosition.y));

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            TowerBuildArea buildArea = hit.GetComponent<TowerBuildArea>();

            if (buildArea == null)
            {
                buildArea = hit.GetComponentInParent<TowerBuildArea>();
            }

            if (buildArea != null)
            {
                return buildArea;
            }
        }

        return null;
    }

    private List<TowerBuildArea> GetBuildAreas()
    {
        List<TowerBuildArea> results = new List<TowerBuildArea>();

        if (buildAreasParent != null)
        {
            results.AddRange(buildAreasParent.GetComponentsInChildren<TowerBuildArea>(true));
        }
        else
        {
            results.AddRange(FindObjectsOfType<TowerBuildArea>());
        }

        return results;
    }

    private bool IsValidTakeOverTarget(TowerBuildArea buildArea, int currentPlayerId)
    {
        return buildArea != null && buildArea.CanBeTakenOverBy(currentPlayerId);
    }

    private bool IsValidFreezeClaimTarget(TowerBuildArea buildArea)
    {
        return buildArea != null && buildArea.CanBeFrozen();
    }

    private bool IsValidTargetForMode(TowerBuildArea buildArea, int currentPlayerId, TileTargetingMode mode)
    {
        if (mode == TileTargetingMode.TakeOver)
        {
            return IsValidTakeOverTarget(buildArea, currentPlayerId);
        }

        if (mode == TileTargetingMode.FreezeClaim)
        {
            return IsValidFreezeClaimTarget(buildArea);
        }

        return false;
    }

    private int GetTakeOverCost(TowerBuildArea buildArea)
    {
        if (buildArea == null)
        {
            return 0;
        }

        int normalTakeoverCost = Mathf.CeilToInt(buildArea.landPurchaseCost * 1.5f);
        return Mathf.CeilToInt(normalTakeoverCost / 2f);
    }

    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : 0;
    }

    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
    }

    private PlayerResource GetPlayerResource(int playerId)
    {
        return playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
    }

    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    private void RefreshPlayerUI(int playerId)
    {
        if (playerManager == null)
        {
            return;
        }

        playerManager.RefreshPlayerUI(playerId);
        playerManager.RefreshCurrentPlayerUI();
    }

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
                tilemap.color = active ? dimColor : normalColor;
            }
        }

        foreach (SpriteRenderer spriteRenderer in spritesToDim)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = active ? dimColor : normalColor;
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
        ClearHighlights();
        selectedTile = null;
        currentMode = TileTargetingMode.None;
        isTargeting = false;
        SetTargetingVisuals(false);
    }

    private void ClearHighlights()
    {
        foreach (TowerBuildArea target in highlightedAreas)
        {
            if (target != null)
            {
                target.HideHighlight();
                RestoreTileVisual(target);
            }
        }

        highlightedAreas.Clear();
        validTargets.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();
    }

    private void StoreOriginalVisual(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.areaVisualRenderer == null)
        {
            return;
        }

        if (!originalColors.ContainsKey(buildArea))
        {
            originalColors.Add(buildArea, buildArea.areaVisualRenderer.color);
        }

        if (!originalSortingOrders.ContainsKey(buildArea))
        {
            originalSortingOrders.Add(buildArea, buildArea.areaVisualRenderer.sortingOrder);
        }
    }

    private void ApplyTileVisual(TowerBuildArea buildArea, Color color, int sortingOrder, bool isSelected)
    {
        if (buildArea == null)
        {
            return;
        }

        if (buildArea.areaVisualRenderer == null)
        {
            if (isSelected)
            {
                Debug.LogWarning("Selected tile has no areaVisualRenderer.");
            }

            buildArea.ShowHighlight(color);
            return;
        }

        Color visibleColor = color;
        visibleColor.a = 1f;
        buildArea.areaVisualRenderer.color = visibleColor;
        buildArea.areaVisualRenderer.sortingOrder = sortingOrder;
    }

    private void RestoreTileVisual(TowerBuildArea buildArea)
    {
        if (buildArea == null || buildArea.areaVisualRenderer == null)
        {
            return;
        }

        if (originalColors.TryGetValue(buildArea, out Color originalColor))
        {
            buildArea.areaVisualRenderer.color = originalColor;
        }
        else
        {
            buildArea.RefreshOwnershipVisual(playerManager);
        }

        buildArea.RefreshOwnershipVisual(playerManager);

        if (originalSortingOrders.TryGetValue(buildArea, out int originalSortingOrder))
        {
            buildArea.areaVisualRenderer.sortingOrder = originalSortingOrder;
        }
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
