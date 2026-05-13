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

    [Header("Tower")]
    public GameObject cannonTowerPrefab;
    public int cannonTowerCost = 6;

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
        if (buildArea == null)
        {
            return;
        }

        if (currentPlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsUnowned())
        {
            TryPurchaseLand(buildArea);
            return;
        }

        if (buildArea.IsClaimable() && buildArea.IsOwnedByOtherPlayer(currentPlayerId))
        {
            ShowToast("This land belongs to another player.");
            return;
        }

        TryBuildTower(buildArea);
    }

    private void TryPurchaseLand(TowerBuildArea buildArea)
    {
        if (currentPlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (!currentPlayerResource.CanAfford(buildArea.landPurchaseCost))
        {
            ShowToast("Not enough gold to buy this land.");
            return;
        }

        if (!currentPlayerResource.SpendMoney(buildArea.landPurchaseCost))
        {
            ShowToast("Not enough gold to buy this land.");
            return;
        }

        buildArea.ClaimArea(currentPlayerId);
        ShowToast("Land purchased.");
    }

    private void TryBuildTower(TowerBuildArea buildArea)
    {
        if (currentPlayerResource == null)
        {
            ShowToast("Player resource is missing.");
            return;
        }

        if (cannonTowerPrefab == null)
        {
            ShowToast("Tower prefab is missing.");
            return;
        }

        if (buildArea.isOccupied)
        {
            ShowToast("A tower is already built here.");
            return;
        }

        if (!buildArea.CanBuildTower(currentPlayerId))
        {
            ShowToast("This land belongs to another player.");
            return;
        }

        if (!currentPlayerResource.CanAfford(cannonTowerCost))
        {
            ShowToast("Not enough gold to build a tower.");
            return;
        }

        if (!currentPlayerResource.SpendMoney(cannonTowerCost))
        {
            ShowToast("Not enough gold to build a tower.");
            return;
        }

        Transform spawnPoint = buildArea.towerSpawnPoint != null
            ? buildArea.towerSpawnPoint
            : buildArea.transform;

        GameObject tower = Instantiate(
            cannonTowerPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        CannonTower cannonTower = tower.GetComponent<CannonTower>();

        if (cannonTower != null)
        {
            cannonTower.ownerPlayerId = currentPlayerId;
            cannonTower.ownerResource = currentPlayerResource;
        }

        buildArea.SetTower(tower);
        ShowToast("Tower built.");
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
