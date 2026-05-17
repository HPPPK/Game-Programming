using TMPro;
using UnityEngine;

public class ResultRowUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI moneyText;

    public void SetData(PlayerResultEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (rankText != null)
        {
            rankText.text = entry.rank.ToString();
        }

        if (playerText != null)
        {
            playerText.text = entry.displayName;
        }

        if (scoreText != null)
        {
            scoreText.text = entry.score.ToString();
        }

        if (hpText != null)
        {
            hpText.text = entry.castleHp.ToString();
        }

        if (moneyText != null)
        {
            moneyText.text = entry.money.ToString();
        }
    }
}
