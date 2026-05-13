/*
 * File: CastleBase.cs
 *
 * Purpose:
 * This script represents one player's castle/base health. Enemy arrival nodes
 * call TakeDamage() on this script, and the base tracks whether its HP has
 * reached zero.
 *
 * Runtime behavior:
 * - Start() initializes currentHP to maxHP.
 * - TakeDamage() subtracts HP, clamps it to zero, and triggers game-over behavior.
 * - OnGameOver() currently logs the loss and turns the castle SpriteRenderer gray.
 *
 * Inspector setup:
 * - playerName is used in debug messages.
 * - maxHP controls the starting health.
 * Dependency notes:
 * - CastleEndNode calls TakeDamage() when an enemy reaches that endpoint.
 * - CastleHealthBar reads currentHP and maxHP to update the visual HP display.
 */
using UnityEngine;

public class CastleBase : MonoBehaviour
{
    public string playerName = "Player";
    public int maxHP = 20;
    public int currentHP = 20;

    void Start()
    {
        currentHP = maxHP;
        Debug.Log(playerName + " base ready. HP = " + currentHP);
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
