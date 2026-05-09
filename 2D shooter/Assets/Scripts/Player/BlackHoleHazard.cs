using UnityEngine;

public class BlackHoleHazard : MonoBehaviour
{
    [Header("Black Hole Settings")]
    public float pullStrength = 4f;
    public float dangerRadius = 0.6f;
    public bool instantGameOver = true;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Vector2 playerPosition = other.transform.position;
        Vector2 blackHolePosition = transform.position;

        Vector2 directionToBlackHole = blackHolePosition - playerPosition;
        float distance = directionToBlackHole.magnitude;

        if (distance <= dangerRadius)
        {
            if (instantGameOver && GameManager.instance != null)
            {
                Debug.Log("Player was pulled into the black hole!");
                GameManager.instance.GameOver();
            }

            return;
        }

        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            Vector2 pullDirection = directionToBlackHole.normalized;
            float distanceFactor = Mathf.Clamp01(1f - distance / GetComponent<CircleCollider2D>().radius);
            Vector2 pullForce = pullDirection * pullStrength * distanceFactor;

            playerRb.AddForce(pullForce, ForceMode2D.Force);
        }
        else
        {
            other.transform.position = Vector2.MoveTowards(
                other.transform.position,
                transform.position,
                pullStrength * 0.2f * Time.deltaTime
            );
        }
    }
}