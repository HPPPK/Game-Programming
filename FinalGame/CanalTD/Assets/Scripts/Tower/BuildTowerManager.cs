using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Reflection;

public class BuildTowerManager : MonoBehaviour
{
    [Header("Player")]
    public int currentPlayerId = 0;
    public PlayerResource currentPlayerResource;
    public PlayerManager playerManager;

    [Header("Tower")]
    public GameObject cannonTowerPrefab;
    public int cannonTowerCost = 6;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Camera")]
    public Camera targetCamera;

    [Header("Toast Message")]
    public MonoBehaviour toastMessage;
    public GameObject toastMessageObject;
    public TextMeshProUGUI toastText;
    public float toastDuration = 1.2f;

    private Coroutine toastCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBuildTowerAtMouse();
        }
    }

    private void TryBuildTowerAtMouse()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (targetCamera == null)
        {
            ShowToast("Camera is missing.");
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera)
        );

        Vector2 clickPosition = new Vector2(worldPosition.x, worldPosition.y);
        Collider2D[] hits = Physics2D.OverlapPointAll(clickPosition);

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        TowerBuildArea buildArea = null;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            buildArea = hit.GetComponent<TowerBuildArea>();

            if (buildArea == null)
            {
                buildArea = hit.GetComponentInParent<TowerBuildArea>();
            }

            if (buildArea != null)
            {
                break;
            }
        }

        if (buildArea == null)
        {
            return;
        }

        HandleBuildAreaClick(buildArea);
    }

    private void HandleBuildAreaClick(TowerBuildArea buildArea)
    {
        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast("You cannot build during enemy wave.");
            return;
        }
        if (buildArea == null)
        {
            return;
        }

        PlayerResource activePlayerResource = GetCurrentPlayerResource();

        if (activePlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsUnowned())
        {
            TryPurchaseLand(buildArea);
            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsOwnedByOtherPlayer(GetCurrentPlayerId()))
        {
            ShowToast("This land belongs to another player.");
            return;
        }

        TryBuildTower(buildArea);
    }

    private void TryPurchaseLand(TowerBuildArea buildArea)
    {
        int activePlayerId = GetCurrentPlayerId();
        PlayerResource activePlayerResource = GetCurrentPlayerResource();

        if (activePlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (!activePlayerResource.CanAfford(buildArea.landPurchaseCost))
        {
            ShowToast("Not enough gold to buy this land.");
            return;
        }

        if (!activePlayerResource.SpendMoney(buildArea.landPurchaseCost))
        {
            ShowToast("Not enough gold to buy this land.");
            return;
        }

        buildArea.SetOwner(activePlayerId, playerManager);
        RefreshCurrentPlayerUI();
        ShowToast("Land purchased.");
    }

    private void TryBuildTower(TowerBuildArea buildArea)
    {
        int activePlayerId = GetCurrentPlayerId();
        PlayerResource activePlayerResource = GetCurrentPlayerResource();

        if (activePlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        bool usesPlayerSpecificTowerPrefab = HasPlayerSpecificTowerPrefab(activePlayerId);
        GameObject towerPrefab = GetTowerPrefabForPlayer(activePlayerId);

        if (towerPrefab == null)
        {
            ShowToast("Tower prefab is missing.");
            return;
        }

        if (buildArea.isOccupied)
        {
            ShowToast("A tower is already built here.");
            return;
        }

        if (!buildArea.CanBuildTower(activePlayerId))
        {
            ShowToast("This land belongs to another player.");
            return;
        }

        if (!activePlayerResource.CanAfford(cannonTowerCost))
        {
            ShowToast("Not enough gold to build a tower.");
            return;
        }

        if (!activePlayerResource.SpendMoney(cannonTowerCost))
        {
            ShowToast("Not enough gold to build a tower.");
            return;
        }

        Transform spawnPoint = buildArea.towerSpawnPoint != null
            ? buildArea.towerSpawnPoint
            : buildArea.transform;

        GameObject tower = Instantiate(
            towerPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        ForceTowerAlphaOpaque(tower);

        CannonTower cannonTower = tower.GetComponent<CannonTower>();

        if (cannonTower != null)
        {
            cannonTower.ownerPlayerId = activePlayerId;
            cannonTower.ownerResource = activePlayerResource;
            cannonTower.ApplyOwnerVisual(playerManager, activePlayerId, !usesPlayerSpecificTowerPrefab);
        }
        else if (!usesPlayerSpecificTowerPrefab)
        {
            ApplyOwnerVisualToGenericTower(tower, activePlayerId);
        }

        ForceTowerAlphaOpaque(tower);

        buildArea.SetTower(tower);
        RefreshCurrentPlayerUI();
        ShowToast("Tower built.");
    }

    private int GetCurrentPlayerId()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerId() : currentPlayerId;
    }

    private PlayerResource GetCurrentPlayerResource()
    {
        return playerManager != null ? playerManager.GetCurrentPlayerResource() : currentPlayerResource;
    }

    private GameObject GetTowerPrefabForPlayer(int playerId)
    {
        if (playerManager != null)
        {
            GameObject playerTowerPrefab = playerManager.GetTowerPrefabForPlayer(playerId);

            if (playerTowerPrefab != null)
            {
                return playerTowerPrefab;
            }
        }

        return cannonTowerPrefab;
    }

    private bool HasPlayerSpecificTowerPrefab(int playerId)
    {
        return playerManager != null && playerManager.GetTowerPrefabForPlayer(playerId) != null;
    }

    private void ApplyOwnerVisualToGenericTower(GameObject tower, int playerId)
    {
        if (tower == null || playerManager == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tower.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        Sprite towerSprite = playerManager.GetTowerSpriteForPlayer(playerId);

        if (towerSprite != null)
        {
            spriteRenderer.sprite = towerSprite;
        }

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
    }

    private void ForceTowerAlphaOpaque(GameObject tower)
    {
        if (tower == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tower.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
    }

    private void RefreshCurrentPlayerUI()
    {
        if (playerManager != null)
        {
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    private void ShowToast(string message)
    {
        bool shown = TryCallToastMethod(message);

        if (!shown)
        {
            ShowToastObject(message);
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

    private void ShowToastObject(string message)
    {
        if (toastMessageObject == null && toastText == null)
        {
            return;
        }

        if (toastMessageObject == null && toastText != null)
        {
            toastMessageObject = toastText.transform.parent != null
                ? toastText.transform.parent.gameObject
                : toastText.gameObject;
        }

        if (toastText == null && toastMessageObject != null)
        {
            toastText = toastMessageObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (toastText != null)
        {
            toastText.text = message;
            toastText.fontSize = GetToastFontSize(message);
        }

        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(true);
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(HideToastAfterDelay());
    }

    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSeconds(toastDuration);

        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(false);
        }
    }

    private float GetToastFontSize(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return 4f;
        }

        if (message.Length > 33)
        {
            return 2f;
        }

        if (message.Length >= 20)
        {
            return 3f;
        }

        return 4f;
    }
}
