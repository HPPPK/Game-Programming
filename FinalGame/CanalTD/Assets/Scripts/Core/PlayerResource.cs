using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [Header("Player")]
    public int playerId = 0;

    [Header("Resource")]
    public int money = 12;
    public int score = 0;

    [Header("UI")]
    public PresentTheNumberUI presentTheNumberUI;

    private void Start()
    {
        
        RefreshUI();
    }

    public bool CanAfford(int cost)
    {
        return money >= cost;
    }

    public bool SpendMoney(int cost)
    {
        if (money < cost)
        {
            return false;
        }

        money -= cost;
        RefreshUI();
        return true;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void AddKillReward(int goldReward, int scoreReward)
    {
        money += goldReward;
        score += scoreReward;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (presentTheNumberUI != null)
        {
            presentTheNumberUI.SetMoney(money);
        }
    }
}
