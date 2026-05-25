/*
 * File: APDisplayUI.cs
 *
 * Purpose:
 * This script displays the current AP value from TurnManager using TextMeshPro.
 * In GameScene_AIPrototype, TurnManager is only a synced helper, but the active
 * turn source is still resolved so the UI does not imply humans can act during AI turns.
 * It is intentionally simple and refreshes every frame so the UI stays correct
 * even when AP changes from different gameplay actions.
 *
 * Inspector setup:
 * - apText should point to the TextMeshProUGUI object that displays AP.
 * - turnManager is optional. If left empty, the script uses TurnManager.Instance.
 */
using TMPro;
using UnityEngine;

public class APDisplayUI : MonoBehaviour
{
    public TurnManager turnManager;
    public TextMeshProUGUI apText;

    void Awake()
    {
        if (apText == null)
        {
            apText = GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        TurnManager manager = turnManager != null ? turnManager : TurnManager.Instance;
        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(manager);

        if (manager == null || apText == null)
        {
            return;
        }

        string prefix = turnSource != null && !turnSource.CanHumanAct && TurnSourceResolver.IsAIPrototypeActive()
            ? "AI AP: "
            : "AP: ";

        apText.text = prefix + manager.currentAP + " / " + manager.maxAP;
    }
}
