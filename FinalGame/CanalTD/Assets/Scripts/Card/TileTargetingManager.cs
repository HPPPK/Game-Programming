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
        TakeOver
    }

    [Header("Managers")]
    public PlayerManager playerManager;
    public CardDrawManager cardDrawManager;
    public Camera targetCamera;
    public MonoBehaviour toastMessage;

    [Header("UI")]
    public GameObject darkOverlay;
    public GameObject targetingUI;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Map Dimming")]
    public Tilemap[] tilemapsToDim;
    public SpriteRenderer[] spritesToDim;
    public Color normalColor = Color.white;
    public Color dimColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Build Areas")]
    public Transform buildAreasParent;
    public Color validHighlightColor = new Color(1f, 0.85f, 0.15f, 0.65f);
    public Color selectedColor = new Color(0.25f, 1f, 0.45f, 1f);
    public Color unavailableColor = Color.red;

    private readonly List<TowerBuildArea> highlightedAreas = new List<TowerBuildArea>();
    private readonly List<TowerBuildArea> validTargets = new List<TowerBuildArea>();
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
        currentMode = TileTargetingMode.TakeOver;
        selectedTile = null;
        highlightedAreas.Clear();
        validTargets.Clear();

        List<TowerBuildArea> allAreas = GetBuildAreas();
        int currentPlayerId = GetCurrentPlayerId();
        PlayerResource currentPlayer = GetCurrentPlayerResource();

        foreach (TowerBuildArea buildArea in allAreas)
        {
            if (IsValidTakeOverTarget(buildArea, currentPlayerId))
            {
                validTargets.Add(buildArea);
            }
        }

        if (validTargets.Count == 0)
        {
            ShowToast("No land available to take over.");
            currentMode = TileTargetingMode.None;
            return false;
        }

        bool hasAffordableTarget = false;

        foreach (TowerBuildArea target in validTargets)
        {
            if (target != null && currentPlayer != null && currentPlayer.CanAfford(GetTakeOverCost(target)))
            {
                hasAffordableTarget = true;
                break;
            }
        }

        if (!hasAffordableTarget)
        {
            ShowToast("Not enough gold to take over any land.");
            currentMode = TileTargetingMode.None;
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);

        foreach (TowerBuildArea buildArea in allAreas)
        {
            if (buildArea == null)
            {
                continue;
            }

            highlightedAreas.Add(buildArea);
            buildArea.ShowHighlight(validTargets.Contains(buildArea) ? validHighlightColor : unavailableColor);
        }

        return true;
    }

    public void ConfirmSelection()
    {
        if (!isTargeting)
        {
            return;
        }

        if (currentMode == TileTargetingMode.TakeOver)
        {
            ConfirmTakeOver();
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

        int currentPlayerId = GetCurrentPlayerId();

        if (!IsValidTakeOverTarget(selectedTile, currentPlayerId))
        {
            ShowToast("Choose an opponent-owned land.");
            return;
        }

        PlayerResource currentPlayer = GetCurrentPlayerResource();

        if (currentPlayer == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        int takeOverCost = GetTakeOverCost(selectedTile);

        if (!currentPlayer.CanAfford(takeOverCost))
        {
            ShowToast("Not enough gold to take over this land.");
            return;
        }

        if (cardDrawManager == null)
        {
            ShowToast("Card manager is missing.");
            return;
        }

        if (!cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            ShowToast("Could not play this card.");
            return;
        }

        if (!currentPlayer.SpendMoney(takeOverCost))
        {
            ShowToast("Not enough gold to take over this land.");
            return;
        }

        selectedTile.RemoveCurrentTower();
        selectedTile.ownerPlayerId = currentPlayerId;
        selectedTile.isOwned = true;
        selectedTile.inactiveForPlayerId = currentPlayerId;
        selectedTile.activatesNextTurn = true;
        selectedTile.RefreshOwnershipVisual(playerManager);

        if (playerManager != null)
        {
            playerManager.RefreshCurrentPlayerUI();
        }

        ShowToast("Land taken over.");
        ExitTargetingMode();
    }

    private void TrySelectTileAtMouse()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TowerBuildArea clickedTile = GetTileUnderMouse();

        if (clickedTile == null || !validTargets.Contains(clickedTile))
        {
            ShowToast("Choose an opponent-owned land.");
            return;
        }

        if (selectedTile != null)
        {
            selectedTile.ShowHighlight(validTargets.Contains(selectedTile) ? validHighlightColor : unavailableColor);
        }

        selectedTile = clickedTile;
        selectedTile.ShowHighlight(selectedColor);
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
        return buildArea != null &&
            buildArea.areaType == BuildAreaType.Claimable &&
            buildArea.isOwned &&
            buildArea.ownerPlayerId != currentPlayerId &&
            !buildArea.isFrozenOrSealed;
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

    private void SetTargetingVisuals(bool active)
    {
        if (darkOverlay != null)
        {
            darkOverlay.SetActive(active);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(active);
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
            }
        }

        highlightedAreas.Clear();
        validTargets.Clear();
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
