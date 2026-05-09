using UnityEngine;

/// <summary>
/// Creates a black hole hazard that pulls the player toward its center.
/// If the player gets too close to the center, the game can trigger Game Over.
/// </summary>
public class BlackHoleHazard : MonoBehaviour
{
    [Header("Black Hole Settings")]

    // Strength of the pulling force applied to the player.
    public float pullStrength = 4f;

    // Distance from the center where the black hole becomes deadly.
    public float dangerRadius = 0.6f;

    // If true, the player loses immediately when reaching the danger radius.
    public bool instantGameOver = true;

    /// <summary>
    /// Called every physics frame while another Collider2D stays inside
    /// the black hole trigger area.
    /// </summary>
    /// <param name="other">The collider currently inside the trigger area.</param>
    private void OnTriggerStay2D(Collider2D other)
    {
        // Only affect the player. Ignore enemies, bullets, and other objects.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Get the current positions of the player and the black hole.
        Vector2 playerPosition = other.transform.position;
        Vector2 blackHolePosition = transform.position;

        // Calculate the direction and distance from the player to the black hole.
        Vector2 directionToBlackHole = blackHolePosition - playerPosition;
        float distance = directionToBlackHole.magnitude;

        // If the player is too close to the center, trigger the danger effect.
        if (distance <= dangerRadius)
        {
            if (instantGameOver && GameManager.instance != null)
            {
                Debug.Log("Player was pulled into the black hole!");
                GameManager.instance.GameOver();
            }

            return;
        }

        // Try to move the player using Rigidbody2D physics.
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            // Normalize the direction so only the direction matters, not the distance.
            Vector2 pullDirection = directionToBlackHole.normalized;

            // Make the pull stronger when the player is closer to the black hole center.
            float distanceFactor = Mathf.Clamp01(1f - distance / GetComponent<CircleCollider2D>().radius);

            // Calculate and apply the final pulling force.
            Vector2 pullForce = pullDirection * pullStrength * distanceFactor;
            playerRb.AddForce(pullForce, ForceMode2D.Force);
        }
        else
        {
            // Fallback movement if the player does not have a Rigidbody2D.
            other.transform.position = Vector2.MoveTowards(
                other.transform.position,
                transform.position,
                pullStrength * 0.2f * Time.deltaTime
            );
        }
    }
}