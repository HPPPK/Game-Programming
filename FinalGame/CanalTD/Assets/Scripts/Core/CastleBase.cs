using UnityEngine;

public class CastleBase : MonoBehaviour
{
    public string playerName = "Player";
    public int maxHP = 20;
    public int currentHP = 20;

    [Header("Test Only")]
    public KeyCode testDamageKey = KeyCode.None;

    void Start()
    {
        currentHP = maxHP;
        Debug.Log(playerName + " base ready. HP = " + currentHP);
    }

    void Update()
    {
        if (testDamageKey != KeyCode.None && Input.GetKeyDown(testDamageKey))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP < 0)
        {
            currentHP = 0;
        }

        Debug.Log(playerName + " base took " + damage + " damage. HP = " + currentHP);

        if (currentHP <= 0)
        {
            Debug.Log(playerName + " GAME OVER");
            OnGameOver();
        }
    }

    void OnGameOver()
    {
        Debug.Log(">>> " + playerName + " LOSE <<<");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }
    }
}