/*
 * File: TowerTargetingManager.cs
 *
 * Purpose:
 * Implements TowerTargetingManager for the card layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TowerTargetingManager within the card system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify TowerTargetingManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TowerTargetingManager : MonoBehaviour
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

    [Header("Tower Targets")]
    public Transform towersParent;
    public Color validHighlightColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.1f, 1f);
    public int validHighlightSortingOrder = 900;
    public int selectedSortingOrder = 1000;

    private readonly List<CannonTower> validTargets = new List<CannonTower>();
    private readonly Dictionary<CannonTower, SpriteRenderer> targetRenderers = new Dictionary<CannonTower, SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<SpriteRenderer, int> originalSortingOrders = new Dictionary<SpriteRenderer, int>();
    private CannonTower selectedTower;
    private bool isTargeting = false;

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

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTowerAtMouse();
        }
    }

    public bool BeginPowerBoostTargeting()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.PowerBoost, null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        selectedTower = null;
        validTargets.Clear();
        targetRenderers.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();

        int currentPlayerId = playerManager != null ? playerManager.GetCurrentPlayerId() : 0;
        List<CannonTower> towers = GetTowers();
        Debug.Log("Tower scan count = " + towers.Count);

        foreach (CannonTower tower in towers)
        {
            if (tower != null)
            {
                Debug.Log("Tower found: " + tower.name + ", ownerPlayerId = " + tower.ownerPlayerId);
            }

            if (IsValidPowerBoostTarget(tower, currentPlayerId))
            {
                validTargets.Add(tower);
            }
        }

        if (validTargets.Count == 0)
        {
            ShowToast("No tower available to boost.");
            return false;
        }

        isTargeting = true;
        SetTargetingVisuals(true);

        foreach (CannonTower tower in validTargets)
        {
            SpriteRenderer renderer = GetTargetRenderer(tower);

            if (renderer == null)
            {
                Debug.LogWarning("Tower target has no SpriteRenderer: " + tower.name);
                continue;
            }

            targetRenderers[tower] = renderer;
            StoreOriginalVisual(renderer);
            ApplyTargetVisual(renderer, validHighlightColor, validHighlightSortingOrder);
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

        if (selectedTower == null)
        {
            ShowToast("Choose a tower first.");
            return;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null &&
            cardDrawManager != null)
        {
            GameObject pendingCardPrefab = cardDrawManager.GetPendingCardPrefabForTargeting();
            string targetId = selectedTower.transform.parent != null
                ? selectedTower.transform.parent.name + "_tower"
                : selectedTower.gameObject.name;

            if (pendingCardPrefab == null)
            {
                ShowToast("Could not play this card.");
                return;
            }

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                targetId,
                -1
            );

            if (requested)
            {
                ExitWithoutConsumingCard();
            }

            return;
        }

        ResolvePowerBoost(playerManager != null ? playerManager.GetCurrentPlayerId() : 0, selectedTower, true);
    }

    public bool ResolvePowerBoost(int playerId, CannonTower tower)
    {
        return ResolvePowerBoost(playerId, tower, false);
    }

    public bool ResolvePowerBoost(int playerId, CannonTower tower, bool consumePendingCard)
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.PowerBoost, tower != null ? tower.gameObject : null))
        {
            return false;
        }

        if (!IsValidPowerBoostTarget(tower, playerId))
        {
            ShowToast("Choose one of your towers.");
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

        tower.ApplyPowerBoostForNextWave();

        if (consumePendingCard && !cardDrawManager.ConsumeSelectedCardAfterSuccessfulTargeting())
        {
            tower.boostPendingForNextWave = false;
            ShowToast("Could not play this card.");
            return false;
        }

        PlayerResource currentPlayer = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;
        string actorName = currentPlayer != null ? currentPlayer.GetDisplayName() : "Current player";
        ShowToast(actorName + " used Power Boost on their tower.");

        if (consumePendingCard)
        {
            ExitTargetingMode();
        }

        return true;
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

    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }

    private void TrySelectTowerAtMouse()
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (IsPointerOverTargetingControls())
        {
            return;
        }

        CannonTower clickedTower = GetTowerUnderMouse();
        int currentPlayerId = playerManager != null ? playerManager.GetCurrentPlayerId() : 0;

        Debug.Log("Current player id = " + currentPlayerId);

        if (clickedTower == null)
        {
            ShowToast("Choose one of your towers.");
            return;
        }

        Debug.Log("Clicked tower owner id = " + clickedTower.ownerPlayerId);

        if (clickedTower.ownerPlayerId != currentPlayerId)
        {
            ShowToast("You can only boost your own tower.");
            return;
        }

        if (!validTargets.Contains(clickedTower))
        {
            ShowToast("Choose one of your towers.");
            return;
        }

        if (selectedTower != null && targetRenderers.ContainsKey(selectedTower))
        {
            ApplyTargetVisual(targetRenderers[selectedTower], validHighlightColor, validHighlightSortingOrder);
        }

        selectedTower = clickedTower;

        if (targetRenderers.TryGetValue(selectedTower, out SpriteRenderer selectedRenderer))
        {
            ApplyTargetVisual(selectedRenderer, selectedColor, selectedSortingOrder);
        }

        Debug.Log("Selected tower: " + selectedTower.name);
    }

    private CannonTower GetTowerUnderMouse()
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
        CannonTower firstTower = null;
        CannonTower firstOwnedTower = null;
        int currentPlayerId = playerManager != null ? playerManager.GetCurrentPlayerId() : 0;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            Debug.Log("Clicked object: " + hit.gameObject.name);

            CannonTower tower = hit.GetComponent<CannonTower>();

            if (tower == null)
            {
                tower = hit.GetComponentInParent<CannonTower>();
            }

            if (tower == null)
            {
                tower = hit.GetComponentInChildren<CannonTower>();
            }

            if (tower != null)
            {
                Debug.Log("Found tower on clicked object: " + tower.name);

                if (firstTower == null)
                {
                    firstTower = tower;
                }

                if (tower.ownerPlayerId == currentPlayerId)
                {
                    if (validTargets.Contains(tower))
                    {
                        return tower;
                    }

                    if (firstOwnedTower == null)
                    {
                        firstOwnedTower = tower;
                    }
                }
            }
        }

        return firstOwnedTower != null ? firstOwnedTower : firstTower;
    }

    private List<CannonTower> GetTowers()
    {
        List<CannonTower> towers = new List<CannonTower>();

        if (towersParent != null)
        {
            towers.AddRange(towersParent.GetComponentsInChildren<CannonTower>(true));
        }
        else
        {
            towers.AddRange(Object.FindObjectsOfType<CannonTower>());
        }

        return towers;
    }

    private bool IsValidPowerBoostTarget(CannonTower tower, int currentPlayerId)
    {
        return tower != null &&
            tower.ownerPlayerId == currentPlayerId &&
            !tower.boostPendingForNextWave &&
            !tower.boostActive &&
            !tower.IsDisabledByFreeze();
    }

    private SpriteRenderer GetTargetRenderer(CannonTower tower)
    {
        if (tower == null)
        {
            return null;
        }

        if (tower.towerSpriteRenderer != null)
        {
            return tower.towerSpriteRenderer;
        }

        return tower.GetComponentInChildren<SpriteRenderer>();
    }

    private void StoreOriginalVisual(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (!originalColors.ContainsKey(renderer))
        {
            originalColors[renderer] = renderer.color;
        }

        if (!originalSortingOrders.ContainsKey(renderer))
        {
            originalSortingOrders[renderer] = renderer.sortingOrder;
        }
    }

    private void ApplyTargetVisual(SpriteRenderer renderer, Color color, int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        Color visibleColor = color;
        visibleColor.a = 1f;
        renderer.color = visibleColor;
        renderer.sortingOrder = sortingOrder;
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
        RestoreTargetVisuals();
        selectedTower = null;
        isTargeting = false;
        SetTargetingVisuals(false);
    }

    private void RestoreTargetVisuals()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in originalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }

        foreach (KeyValuePair<SpriteRenderer, int> entry in originalSortingOrders)
        {
            if (entry.Key != null)
            {
                entry.Key.sortingOrder = entry.Value;
            }
        }

        validTargets.Clear();
        targetRenderers.Clear();
        originalColors.Clear();
        originalSortingOrders.Clear();
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
