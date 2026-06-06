/*
 * File: RadialTowerMenu.cs
 *
 * Purpose:
 * World-space UI controller for build-area and tower actions. This script only
 * switches visible button groups, stores pending confirmation state, and forwards
 * confirmed actions to BuildTowerManager.
 */
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum RadialTowerMenuState
{
    None,
    Confirm,
    BuildSelection,
    TowerManagement
}

public enum RadialTowerConfirmAction
{
    None,
    BuyLand,
    BuildTower,
    UpgradeTower,
    SellTower
}

public class RadialTowerMenu : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Confirm Buttons")]
    public Button confirmButton;
    public TMP_Text confirmCostText;

    [Header("Build Buttons")]
    public Button cannonButton;
    public Button archerButton;
    public Button frostButton;
    public Button shockButton;
    public Button sniperButton;

    [Header("Tower Buttons")]
    public Button upgradeButton;
    public Button sellButton;
    public Button cancelButton;

    [Header("Cost Text")]
    public TMP_Text cannonCostText;
    public TMP_Text archerCostText;
    public TMP_Text frostCostText;
    public TMP_Text shockCostText;
    public TMP_Text sniperCostText;
    public TMP_Text upgradeCostText;
    public TMP_Text sellValueText;

    [Header("Tower Info")]
    public TMP_Text towerInfoText;

    [Header("Preview")]
    public Color selectedAreaPreviewColor = new Color(1f, 0.9f, 0.1f, 0.8f);
    public float towerPreviewAlpha = 0.55f;

    private BuildTowerManager buildTowerManager;
    private TowerBuildArea selectedBuildArea;
    private TowerStats currentTowerStats;
    private RadialTowerMenuState currentState = RadialTowerMenuState.None;
    private RadialTowerConfirmAction pendingConfirmAction = RadialTowerConfirmAction.None;
    private TowerType pendingTowerType = TowerType.Cannon;
    private Vector3 lastWorldPosition;
    private int openedFrame = -1;
    private GameObject towerPreviewInstance;
    private TowerBuildArea highlightedPreviewArea;

    private void Awake()
    {
        EnsureRoot();
    }

    public bool IsOpen
    {
        get { return currentState != RadialTowerMenuState.None; }
    }

    private void Update()
    {
        if (!IsOpen || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        // Ignore the same click that opened the menu, then close on any later
        // click outside this menu so other UI such as End Turn behaves normally.
        if (Time.frameCount == openedFrame)
        {
            return;
        }

        if (!IsPointerOverThisMenu())
        {
            Hide();
        }
    }

    public void Initialize(BuildTowerManager manager)
    {
        buildTowerManager = manager;
    }

    public void ShowBuyLandConfirm(TowerBuildArea area, int cost)
    {
        selectedBuildArea = area;
        currentTowerStats = null;
        pendingConfirmAction = RadialTowerConfirmAction.BuyLand;
        Debug.Log("Show Style 1 Confirm: BuyLand");
        ShowConfirm(area, cost);
    }

    public void ShowBuildSelection(TowerBuildArea area)
    {
        selectedBuildArea = area;
        currentTowerStats = null;
        ClearPreview();
        pendingConfirmAction = RadialTowerConfirmAction.None;
        currentState = RadialTowerMenuState.BuildSelection;
        Debug.Log("Show Style 2 BuildSelection");
        ShowAtWorldPosition(GetMenuWorldPosition(area, lastWorldPosition));
        SetConfirmVisible(false);
        SetTowerInfoVisible(false);
        SetBuildButtonsVisible(true);
        SetTowerButtonsVisible(false);
        SetButtonVisible(cancelButton, true);
        RefreshCosts();
    }

    public void ShowTowerManagement(TowerBuildArea area)
    {
        selectedBuildArea = area;
        currentTowerStats = GetTowerStats(area);
        ClearPreview();
        pendingConfirmAction = RadialTowerConfirmAction.None;
        currentState = RadialTowerMenuState.TowerManagement;
        Debug.Log("Show Style 3 TowerManagement");
        ShowAtWorldPosition(GetMenuWorldPosition(area, lastWorldPosition));
        SetConfirmVisible(false);
        SetBuildButtonsVisible(false);
        SetTowerButtonsVisible(true);
        RefreshCosts();
        RefreshTowerInfoText();
    }

    public void ShowBuildConfirm(TowerType type, int cost)
    {
        pendingTowerType = type;
        pendingConfirmAction = RadialTowerConfirmAction.BuildTower;
        Debug.Log("Show Style 1 Confirm: BuildTower");
        ShowConfirm(selectedBuildArea, cost);
    }

    public void ShowUpgradeConfirm(int cost)
    {
        pendingConfirmAction = RadialTowerConfirmAction.UpgradeTower;
        Debug.Log("Show Style 1 Confirm: UpgradeTower");
        ShowConfirm(selectedBuildArea, cost);
    }

    public void ShowSellConfirm(int refund)
    {
        pendingConfirmAction = RadialTowerConfirmAction.SellTower;
        Debug.Log("Show Style 1 Confirm: SellTower");
        ShowConfirm(selectedBuildArea, refund);
    }

    // Compatibility wrapper for older BuildTowerManager calls.
    public void ShowBuildMenu(TowerBuildArea area, Vector3 worldPosition)
    {
        lastWorldPosition = GetMenuWorldPosition(area, worldPosition);
        ShowBuildSelection(area);
    }

    // Compatibility wrapper for older BuildTowerManager calls.
    public void ShowTowerMenu(TowerBuildArea area, TowerStats tower, Vector3 worldPosition)
    {
        lastWorldPosition = GetMenuWorldPosition(area, worldPosition);
        currentTowerStats = tower;
        ShowTowerManagement(area);
    }

    public void Hide()
    {
        currentState = RadialTowerMenuState.None;
        pendingConfirmAction = RadialTowerConfirmAction.None;
        ClearPreview();
        selectedBuildArea = null;
        currentTowerStats = null;
        EnsureRoot();
        root.SetActive(false);
    }

    public void RefreshCosts()
    {
        if (buildTowerManager != null)
        {
            SetText(cannonCostText, buildTowerManager.GetTowerCost(TowerType.Cannon).ToString());
            SetText(archerCostText, buildTowerManager.GetTowerCost(TowerType.Archer).ToString());
            SetText(frostCostText, buildTowerManager.GetTowerCost(TowerType.Frost).ToString());
            SetText(shockCostText, buildTowerManager.GetTowerCost(TowerType.Shock).ToString());
            SetText(sniperCostText, buildTowerManager.GetTowerCost(TowerType.Sniper).ToString());
        }

        currentTowerStats = currentTowerStats != null ? currentTowerStats : GetTowerStats(selectedBuildArea);

        if (currentTowerStats != null)
        {
            bool canUpgrade = currentTowerStats.CanUpgrade();
            SetText(upgradeCostText, canUpgrade ? currentTowerStats.GetUpgradeCost().ToString() : "Max Level");
            SetText(sellValueText, currentTowerStats.GetSellValue().ToString());

            if (upgradeButton != null)
            {
                upgradeButton.interactable = canUpgrade;
            }
        }

        RefreshTowerInfoText();
    }

    public void ForceShowForDebug()
    {
        EnsureRoot();
        ShowAtWorldPosition(transform.position);
    }

    public void OnClickConfirm()
    {
        Debug.Log("Confirm action: " + pendingConfirmAction + (pendingConfirmAction == RadialTowerConfirmAction.BuildTower ? " " + pendingTowerType : ""));

        if (!HasSelectedAreaAndManager())
        {
            return;
        }

        if (pendingConfirmAction == RadialTowerConfirmAction.BuyLand)
        {
            if (buildTowerManager.ConfirmBuyLand(selectedBuildArea))
            {
                if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
                {
                    Hide();
                }
                else
                {
                    ShowBuildSelection(selectedBuildArea);
                }
            }

            return;
        }

        if (pendingConfirmAction == RadialTowerConfirmAction.BuildTower)
        {
            if (buildTowerManager.ConfirmBuildTower(selectedBuildArea, pendingTowerType))
            {
                Hide();
            }

            return;
        }

        if (pendingConfirmAction == RadialTowerConfirmAction.UpgradeTower)
        {
            if (buildTowerManager.ConfirmUpgradeTower(selectedBuildArea))
            {
                Hide();
            }
            else
            {
                RefreshCosts();
            }

            return;
        }

        if (pendingConfirmAction == RadialTowerConfirmAction.SellTower)
        {
            if (buildTowerManager.ConfirmSellTower(selectedBuildArea))
            {
                Hide();
            }

            return;
        }

        ShowToast("Choose an action first.");
    }

    public void OnClickBuildCannon()
    {
        Debug.Log("Build Cannon clicked");
        SelectTowerForBuild(TowerType.Cannon);
    }

    public void OnClickBuildArcher()
    {
        Debug.Log("Build Archer clicked");
        SelectTowerForBuild(TowerType.Archer);
    }

    public void OnClickBuildFrost()
    {
        Debug.Log("Build Frost clicked");
        SelectTowerForBuild(TowerType.Frost);
    }

    public void OnClickBuildShock()
    {
        Debug.Log("Build Shock clicked");
        SelectTowerForBuild(TowerType.Shock);
    }

    public void OnClickBuildSniper()
    {
        Debug.Log("Build Sniper clicked");
        SelectTowerForBuild(TowerType.Sniper);
    }

    public void OnClickUpgrade()
    {
        Debug.Log("Upgrade clicked");

        if (!HasSelectedAreaAndManager())
        {
            return;
        }

        currentTowerStats = GetTowerStats(selectedBuildArea);

        if (currentTowerStats == null)
        {
            ShowToast("Tower stats are missing.");
            return;
        }

        if (!currentTowerStats.CanUpgrade())
        {
            ShowToast("Max Level.");
            return;
        }

        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.InspectTower, selectedBuildArea != null ? selectedBuildArea.gameObject : null);
        ShowUpgradeConfirm(currentTowerStats.GetUpgradeCost());
    }

    public void OnClickSell()
    {
        Debug.Log("Sell clicked");

        if (!HasSelectedAreaAndManager())
        {
            return;
        }

        currentTowerStats = GetTowerStats(selectedBuildArea);

        if (currentTowerStats == null)
        {
            ShowToast("No tower to sell.");
            return;
        }

        ShowSellConfirm(currentTowerStats.GetSellValue());
    }

    public void OnClickCancel()
    {
        Debug.Log("Cancel clicked");

        if (currentState == RadialTowerMenuState.Confirm && pendingConfirmAction == RadialTowerConfirmAction.BuildTower)
        {
            ShowBuildSelection(selectedBuildArea);
            return;
        }

        Hide();
    }

    private void SelectTowerForBuild(TowerType towerType)
    {
        if (!HasSelectedAreaAndManager())
        {
            return;
        }

        ShowBuildConfirm(towerType, buildTowerManager.GetTowerCost(towerType));
    }

    private bool HasSelectedAreaAndManager()
    {
        if (selectedBuildArea == null)
        {
            ShowToast("No build area selected.");
            return false;
        }

        if (buildTowerManager == null)
        {
            ShowToast("Build manager is missing.");
            return false;
        }

        return true;
    }

    private void ShowConfirm(TowerBuildArea area, int value)
    {
        currentState = RadialTowerMenuState.Confirm;
        ShowAtWorldPosition(GetMenuWorldPosition(area, lastWorldPosition));
        SetConfirmVisible(true);
        SetTowerInfoVisible(false);
        SetBuildButtonsVisible(false);
        SetTowerButtonsVisible(false);
        SetButtonVisible(cancelButton, true);
        SetText(confirmCostText, value.ToString());
        RefreshConfirmPreview();
    }

    private Vector3 GetMenuWorldPosition(TowerBuildArea area, Vector3 fallbackWorldPosition)
    {
        if (area == null)
        {
            return fallbackWorldPosition;
        }

        return area.towerSpawnPoint != null
            ? area.towerSpawnPoint.position
            : area.transform.position;
    }

    private void ShowAtWorldPosition(Vector3 worldPosition)
    {
        EnsureRoot();
        lastWorldPosition = worldPosition;
        root.SetActive(true);
        transform.position = worldPosition;
        transform.SetAsLastSibling();
        openedFrame = Time.frameCount;
        Debug.Log("RadialTowerMenu activeSelf=" + root.activeSelf);
    }

    private void RefreshConfirmPreview()
    {
        ClearPreview();

        if (selectedBuildArea == null)
        {
            return;
        }

        HighlightSelectedArea();

        if (pendingConfirmAction == RadialTowerConfirmAction.BuildTower)
        {
            CreateTowerPreview(pendingTowerType);
        }
    }

    private void HighlightSelectedArea()
    {
        highlightedPreviewArea = selectedBuildArea;

        if (highlightedPreviewArea != null)
        {
            highlightedPreviewArea.ShowHighlight(selectedAreaPreviewColor);
        }
    }

    private void CreateTowerPreview(TowerType towerType)
    {
        if (buildTowerManager == null || selectedBuildArea == null)
        {
            return;
        }

        GameObject previewPrefab = buildTowerManager.GetTowerPreviewPrefab(towerType);

        if (previewPrefab == null)
        {
            ShowToast("Tower prefab is missing.");
            return;
        }

        Vector3 previewPosition = selectedBuildArea.towerSpawnPoint != null
            ? selectedBuildArea.towerSpawnPoint.position
            : selectedBuildArea.transform.position;

        towerPreviewInstance = Instantiate(previewPrefab, previewPosition, Quaternion.identity);
        towerPreviewInstance.name = towerType + " Preview";
        PrepareTowerPreviewObject(towerPreviewInstance);
    }

    private void PrepareTowerPreviewObject(GameObject previewObject)
    {
        if (previewObject == null)
        {
            return;
        }

        foreach (Collider2D collider in previewObject.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = false;
        }

        foreach (CannonTower tower in previewObject.GetComponentsInChildren<CannonTower>(true))
        {
            tower.enabled = false;
        }

        foreach (SpriteRenderer renderer in previewObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            color.a = towerPreviewAlpha;
            renderer.color = color;
        }
    }

    private void ClearPreview()
    {
        if (towerPreviewInstance != null)
        {
            Destroy(towerPreviewInstance);
            towerPreviewInstance = null;
        }

        if (highlightedPreviewArea != null)
        {
            highlightedPreviewArea.HideHighlight();
            highlightedPreviewArea = null;
        }
    }

    private bool IsPointerOverThisMenu()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
        {
            return false;
        }

        return selectedObject == gameObject || selectedObject.transform.IsChildOf(transform);
    }

    private TowerStats GetTowerStats(TowerBuildArea area)
    {
        if (area == null || area.currentTower == null)
        {
            return null;
        }

        TowerStats stats = area.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : area.currentTower.GetComponentInChildren<TowerStats>();
    }

    private void ShowToast(string message)
    {
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ShowToast(message);
            return;
        }

        GlobalUIManager globalUIManager = FindObjectOfType<GlobalUIManager>();

        if (globalUIManager != null)
        {
            globalUIManager.ShowToast(message);
            return;
        }

        Debug.Log(message);
    }

    private void EnsureRoot()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    private void SetConfirmVisible(bool visible)
    {
        SetButtonVisible(confirmButton, visible);
        SetTextVisible(confirmCostText, visible);
    }

    private void RefreshTowerInfoText()
    {
        if (currentState != RadialTowerMenuState.TowerManagement)
        {
            SetTowerInfoVisible(false);
            return;
        }

        currentTowerStats = currentTowerStats != null ? currentTowerStats : GetTowerStats(selectedBuildArea);

        if (currentTowerStats == null)
        {
            SetTowerInfoVisible(false);
            return;
        }

        SetText(towerInfoText, currentTowerStats.towerType + " Lv. " + currentTowerStats.level + " / " + currentTowerStats.maxLevel);
        SetTowerInfoVisible(true);
    }

    private void SetTowerInfoVisible(bool visible)
    {
        SetTextVisible(towerInfoText, visible);
    }

    private void SetBuildButtonsVisible(bool visible)
    {
        SetButtonVisible(cannonButton, visible);
        SetButtonVisible(archerButton, visible);
        SetButtonVisible(frostButton, visible);
        SetButtonVisible(shockButton, visible);
        SetButtonVisible(sniperButton, visible);
    }

    private void SetTowerButtonsVisible(bool visible)
    {
        SetButtonVisible(upgradeButton, visible);
        SetButtonVisible(sellButton, visible);
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private void SetTextVisible(TMP_Text text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
