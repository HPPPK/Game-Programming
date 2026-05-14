using TMPro;
using UnityEngine;

public class PresentTheNumberUI : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI cardText;

    [Header("Initial Values")]
    public int currentMoney = 0;
    public int currentCardCount = 0;

    private void Start()
    {
        RefreshUI();
    }

    public void SetMoney(int money)
    {
        currentMoney = money;
        RefreshUI();
    }

    public void SetCardCount(int cardCount)
    {
        currentCardCount = cardCount;
        RefreshUI();
    }

    public void SetNumbers(int money, int cardCount)
    {
        currentMoney = money;
        currentCardCount = cardCount;
        RefreshUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        RefreshUI();
    }

    public void AddCardCount(int amount)
    {
        currentCardCount += amount;

        if (currentCardCount < 0)
        {
            currentCardCount = 0;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString();
        }

        if (cardText != null)
        {
            cardText.text = currentCardCount.ToString();
        }
    }
}