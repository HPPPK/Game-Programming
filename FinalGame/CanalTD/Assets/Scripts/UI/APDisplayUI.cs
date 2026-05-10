/*
 * File: APDisplayUI.cs
 *
 * Purpose:
 * This script displays the current AP value from TurnManager using TextMeshPro.
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

        if (manager == null || apText == null)
        {
            return;
        }

        apText.text = "AP: " + manager.currentAP + " / " + manager.maxAP;
    }
}
