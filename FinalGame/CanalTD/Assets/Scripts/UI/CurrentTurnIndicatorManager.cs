/*
 * File: CurrentTurnIndicatorManager.cs
 */
using TMPro;
using UnityEngine;

public class CurrentTurnIndicatorManager : MonoBehaviour
{
    public static CurrentTurnIndicatorManager Instance;

    [Header("Turn Markers")]
    public TurnMarkerAnimator[] turnMarkers;
    public int[] turnMarkerPlayerIds;

    [Header("Round UI")]
    public TextMeshProUGUI roundText;

    [Header("Colors")]
    public Color turnColor = new Color(1f, 0.8f, 0.25f, 1f);

    private int currentPlayerId = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 初始化时隐藏所有 marker
        HideAllMarkers();
        
        // 获取初始玩家
        int initialPlayerId = GetInitialPlayerId();
        UpdateCurrentTurnIndicator(initialPlayerId);
    }

    private int GetInitialPlayerId()
    {
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null) return turnManager.currentPlayerId;

        AIPrototypeTurnManager aiTurnManager = FindObjectOfType<AIPrototypeTurnManager>();
        if (aiTurnManager != null) return aiTurnManager.CurrentPlayerId;

        return 0;
    }

    public void UpdateCurrentTurnIndicator(int playerId)
    {
        currentPlayerId = playerId;
        UpdateTurnMarkers(playerId);
        UpdateRoundText();
    }

    public void HideAllMarkers()
    {
        if (turnMarkers == null) return;
        foreach (TurnMarkerAnimator marker in turnMarkers)
        {
            if (marker != null) marker.Hide();
        }
    }

    public void ShowActionMarkers(int[] targetPlayerIds, Color actionColor)
    {
        if (turnMarkers == null) return;

        for (int i = 0; i < turnMarkers.Length; i++)
        {
            TurnMarkerAnimator marker = turnMarkers[i];
            if (marker == null) continue;

            int markerPlayerId = (turnMarkerPlayerIds != null && i < turnMarkerPlayerIds.Length)
                ? turnMarkerPlayerIds[i]
                : i;

            bool isTarget = System.Array.Exists(targetPlayerIds, element => element == markerPlayerId);

            if (isTarget)
            {
                marker.SetMarkerColor(actionColor);
                marker.Show();
            }
            else
            {
                marker.Hide();
            }
        }
    }

    public void ResetMarkersToTurnIndicator()
    {
        if (turnMarkers == null) return;

        for (int i = 0; i < turnMarkers.Length; i++)
        {
            TurnMarkerAnimator marker = turnMarkers[i];
            if (marker != null)
            {
                marker.ResetMarkerColor();
            }
        }

        UpdateCurrentTurnIndicator(currentPlayerId);
    }

    private void UpdateTurnMarkers(int playerId)
    {
        if (turnMarkers == null) 
        {
            Debug.LogError("turnMarkers array is null!");
            return;
        }

        Debug.Log($"=== Updating Turn Markers for Plagyer {playerId} ===");

        for (int i = 0; i < turnMarkers.Length; i++)
        {
            TurnMarkerAnimator marker = turnMarkers[i];
            
            if (marker == null)
            {
                Debug.LogWarning($"TurnMarker[{i}] is null!");
                continue;
            }

            int markerPlayerId = (turnMarkerPlayerIds != null && i < turnMarkerPlayerIds.Length) 
                ? turnMarkerPlayerIds[i] 
                : i;

            bool shouldShow = markerPlayerId == playerId;
            
            Debug.Log($"TurnMarker[{i}]: name={marker.gameObject.name}, markerPlayerId={markerPlayerId}, shouldShow={shouldShow}");

            // 检查 Sprite Renderer
            SpriteRenderer sr = marker.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning($"  -> No SpriteRenderer found!");
            }
            else
            {
                Debug.Log($"  -> SpriteRenderer: sprite={(sr.sprite != null ? sr.sprite.name : "null")}, enabled={sr.enabled}, color={sr.color}");
            }

            // 检查父对象状态
            Debug.Log($"  -> Parent active: {marker.transform.parent.gameObject.activeSelf}");
            Debug.Log($"  -> LocalPosition: {marker.transform.localPosition}");
            Debug.Log($"  -> Scale: {marker.transform.localScale}");

            if (shouldShow)
            {
                marker.Show();
                Debug.Log($"  -> Called Show()");
                // 强制刷新
                marker.gameObject.SetActive(false);
                marker.gameObject.SetActive(true);
            }
            else
            {
                marker.Hide();
            }
        }
    }
    private void UpdateRoundText()
    {
        if (roundText == null) return;

        int currentRound = GetCurrentRound();
        int maxWaves = GetMaxWaves();
        string playerName = GetPlayerDisplayName(currentPlayerId);

        roundText.text = $"Round: {currentRound}/{maxWaves} | {playerName} Turn";
        roundText.color = turnColor;
    }

    private string GetPlayerDisplayName(int playerId)
    {
        PlayerResource[] players = FindObjectsOfType<PlayerResource>();
        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId == playerId)
            {
                string name = player.GetDisplayName();
                return !string.IsNullOrWhiteSpace(name) ? name : $"Player {playerId + 1}";
            }
        }
        return $"Player {playerId + 1}";
    }

    private int GetCurrentRound()
    {
        AIPrototypeTurnManager aiTurnManager = FindObjectOfType<AIPrototypeTurnManager>();
        if (aiTurnManager != null && aiTurnManager.isActiveAndEnabled) return aiTurnManager.currentRound;

        GamePhaseManager phaseManager = FindObjectOfType<GamePhaseManager>();
        if (phaseManager != null) return phaseManager.currentRound;

        return 1;
    }

    private int GetMaxWaves()
    {
        AIPrototypeTurnManager aiTurnManager = FindObjectOfType<AIPrototypeTurnManager>();
        if (aiTurnManager != null && aiTurnManager.isActiveAndEnabled) return aiTurnManager.maxWaves;

        GamePhaseManager phaseManager = FindObjectOfType<GamePhaseManager>();
        if (phaseManager != null) return phaseManager.maxWaves;

        return 10;
    }
}
