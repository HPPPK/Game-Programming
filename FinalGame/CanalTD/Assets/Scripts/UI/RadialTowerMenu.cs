/*
 * File: RadialTowerMenu.cs
 *
 * Purpose:
 * Implements RadialTowerMenu for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for RadialTowerMenu within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify RadialTowerMenu in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
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

    /// <summary>
    /// Finds and stores radial tower menu references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        EnsureRoot();
    }

    public bool IsOpen
    {
        get { return currentState != RadialTowerMenuState.None; }
    }

    /// <summary>
    /// Checks radial tower menu input, timing, animation, or UI state once per frame.
    /// </summary>
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

    /// <summary>
    /// Initializes initialize and prepares the references needed before use.
    /// </summary>
    public void Initialize(BuildTowerManager manager)
    {
        buildTowerManager = manager;
    }

    /// <summary>
    /// Shows buy land confirm with the correct current context.
    /// </summary>
    public void ShowBuyLandConfirm(TowerBuildArea area, int cost)
    {
        selectedBuildArea = area;
        currentTowerStats = null;
        pendingConfirmAction = RadialTowerConfirmAction.BuyLand;
        Debug.Log("Show Style 1 Confirm: BuyLand");
        ShowConfirm(area, cost);
    }

    /// <summary>
    /// Shows build selection with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Shows tower management with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Shows build confirm with the correct current context.
    /// </summary>
    public void ShowBuildConfirm(TowerType type, int cost)
    {
        pendingTowerType = type;
        pendingConfirmAction = RadialTowerConfirmAction.BuildTower;
        Debug.Log("Show Style 1 Confirm: BuildTower");
        ShowConfirm(selectedBuildArea, cost);
    }

    /// <summary>
    /// Shows upgrade confirm with the correct current context.
    /// </summary>
    public void ShowUpgradeConfirm(int cost)
    {
        pendingConfirmAction = RadialTowerConfirmAction.UpgradeTower;
        Debug.Log("Show Style 1 Confirm: UpgradeTower");
        ShowConfirm(selectedBuildArea, cost);
    }

    /// <summary>
    /// Shows sell confirm with the correct current context.
    /// </summary>
    public void ShowSellConfirm(int refund)
    {
        pendingConfirmAction = RadialTowerConfirmAction.SellTower;
        Debug.Log("Show Style 1 Confirm: SellTower");
        ShowConfirm(selectedBuildArea, refund);
    }

    // Compatibility wrapper for older BuildTowerManager calls.
    /// <summary>
    /// Shows build menu with the correct current context.
    /// </summary>
    public void ShowBuildMenu(TowerBuildArea area, Vector3 worldPosition)
    {
        lastWorldPosition = GetMenuWorldPosition(area, worldPosition);
        ShowBuildSelection(area);
    }

    // Compatibility wrapper for older BuildTowerManager calls.
    /// <summary>
    /// Shows tower menu with the correct current context.
    /// </summary>
    public void ShowTowerMenu(TowerBuildArea area, TowerStats tower, Vector3 worldPosition)
    {
        lastWorldPosition = GetMenuWorldPosition(area, worldPosition);
        currentTowerStats = tower;
        ShowTowerManagement(area);
    }

    /// <summary>
    /// Hides this UI element and clears temporary visual state.
    /// </summary>
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

    /// <summary>
    /// Refreshes costs from the latest gameplay data.
    /// </summary>
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

    /// <summary>
    /// Handles force show for debug for land ownership, tower actions, or build UI.
    /// </summary>
    public void ForceShowForDebug()
    {
        EnsureRoot();
        ShowAtWorldPosition(transform.position);
    }

    /// <summary>
    /// Responds to on click confirm and updates the affected gameplay or UI systems.
    /// </summary>
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

    /// <summary>
    /// Responds to on click build cannon and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickBuildCannon()
    {
        Debug.Log("Build Cannon clicked");
        SelectTowerForBuild(TowerType.Cannon);
    }

    /// <summary>
    /// Responds to on click build archer and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickBuildArcher()
    {
        Debug.Log("Build Archer clicked");
        SelectTowerForBuild(TowerType.Archer);
    }

    /// <summary>
    /// Responds to on click build frost and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickBuildFrost()
    {
        Debug.Log("Build Frost clicked");
        SelectTowerForBuild(TowerType.Frost);
    }

    /// <summary>
    /// Responds to on click build shock and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickBuildShock()
    {
        Debug.Log("Build Shock clicked");
        SelectTowerForBuild(TowerType.Shock);
    }

    /// <summary>
    /// Responds to on click build sniper and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnClickBuildSniper()
    {
        Debug.Log("Build Sniper clicked");
        SelectTowerForBuild(TowerType.Sniper);
    }

    /// <summary>
    /// Responds to on click upgrade and updates the affected gameplay or UI systems.
    /// </summary>
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

        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.PrepareUpgradeTower, selectedBuildArea != null ? selectedBuildArea.gameObject : null);
        ShowUpgradeConfirm(currentTowerStats.GetUpgradeCost());
    }

    /// <summary>
    /// Responds to on click sell and updates the affected gameplay or UI systems.
    /// </summary>
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

        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.PrepareSellTower, selectedBuildArea != null ? selectedBuildArea.gameObject : null);
        ShowSellConfirm(currentTowerStats.GetSellValue());
    }

    /// <summary>
    /// Responds to on click cancel and updates the affected gameplay or UI systems.
    /// </summary>
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

    /// <summary>
    /// Selects tower for build and updates the related targeting or UI highlight.
    /// </summary>
    private void SelectTowerForBuild(TowerType towerType)
    {
        if (!HasSelectedAreaAndManager())
        {
            return;
        }

        ShowBuildConfirm(towerType, buildTowerManager.GetTowerCost(towerType));
    }

    /// <summary>
    /// Checks whether selected area and manager is present before the code depends on it.
    /// </summary>
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

    /// <summary>
    /// Shows confirm with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Returns menu world position used to update UI text, layout, or feedback.
    /// </summary>
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

    /// <summary>
    /// Shows at world position with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Refreshes confirm preview from the latest gameplay data.
    /// </summary>
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

    /// <summary>
    /// Handles highlight selected area for land ownership, tower actions, or build UI.
    /// </summary>
    private void HighlightSelectedArea()
    {
        highlightedPreviewArea = selectedBuildArea;

        if (highlightedPreviewArea != null)
        {
            highlightedPreviewArea.ShowHighlight(selectedAreaPreviewColor);
        }
    }

    /// <summary>
    /// Creates tower preview and configures it for the current scene or interaction.
    /// </summary>
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

    /// <summary>
    /// Handles prepare tower preview object for land ownership, tower actions, or build UI.
    /// </summary>
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

    /// <summary>
    /// Clears preview and removes its temporary gameplay or visual effect.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether pointer over this menu is true.
    /// </summary>
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

    /// <summary>
    /// Returns tower stats used to update UI text, layout, or feedback.
    /// </summary>
    private TowerStats GetTowerStats(TowerBuildArea area)
    {
        if (area == null || area.currentTower == null)
        {
            return null;
        }

        TowerStats stats = area.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : area.currentTower.GetComponentInChildren<TowerStats>();
    }

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Ensures root exists or is initialized before the flow continues.
    /// </summary>
    private void EnsureRoot()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    /// <summary>
    /// Sets confirm visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetConfirmVisible(bool visible)
    {
        SetButtonVisible(confirmButton, visible);
        SetTextVisible(confirmCostText, visible);
    }

    /// <summary>
    /// Refreshes tower info text from the latest gameplay data.
    /// </summary>
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

    /// <summary>
    /// Sets tower info visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetTowerInfoVisible(bool visible)
    {
        SetTextVisible(towerInfoText, visible);
    }

    /// <summary>
    /// Sets build buttons visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetBuildButtonsVisible(bool visible)
    {
        SetButtonVisible(cannonButton, visible);
        SetButtonVisible(archerButton, visible);
        SetButtonVisible(frostButton, visible);
        SetButtonVisible(shockButton, visible);
        SetButtonVisible(sniperButton, visible);
    }

    /// <summary>
    /// Sets tower buttons visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetTowerButtonsVisible(bool visible)
    {
        SetButtonVisible(upgradeButton, visible);
        SetButtonVisible(sellButton, visible);
    }

    /// <summary>
    /// Sets button visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Sets text visible and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetTextVisible(TMP_Text text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Sets text and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
