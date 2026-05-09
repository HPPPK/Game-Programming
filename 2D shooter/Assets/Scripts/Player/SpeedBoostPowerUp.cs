using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A collectible power-up that temporarily increases the player's movement speed.
/// The script tries to find a movement speed field on the player automatically.
/// </summary>
public class SpeedBoostPowerUp : MonoBehaviour
{
    [Header("Power Up Settings")]

    // How much the player's movement speed will be multiplied by.
    public float boostMultiplier = 1.8f;

    // How long the speed boost lasts in seconds.
    public float duration = 5f;

    // Prevents the power-up from being triggered more than once.
    private bool used = false;

    /// <summary>
    /// Called when another Collider2D enters this power-up's trigger area.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // If this power-up has already been used, do nothing.
        if (used)
        {
            return;
        }

        // Only allow the player to collect this power-up.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Try to find the player's movement script.
        MonoBehaviour movementScript = FindMovementScript(other.gameObject);

        // If no valid movement script is found, show a warning and stop.
        if (movementScript == null)
        {
            Debug.LogWarning("SpeedBoostPowerUp: Could not find a player movement script with speed / moveSpeed / movementSpeed.");
            return;
        }

        // Mark the power-up as used so it cannot be triggered again.
        used = true;

        // Start the temporary speed boost effect.
        StartCoroutine(ApplySpeedBoost(movementScript));

        // Hide the power-up object after it is collected.
        HidePowerUp();
    }

    /// <summary>
    /// Searches all MonoBehaviour scripts attached to the player object
    /// and returns the first script that contains a valid speed field.
    /// </summary>
    /// <param name="player">The player GameObject.</param>
    /// <returns>A movement script with a speed field, or null if none is found.</returns>
    private MonoBehaviour FindMovementScript(GameObject player)
    {
        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            // Check for common movement speed field names.
            if (HasFloatField(script, "speed") ||
                HasFloatField(script, "moveSpeed") ||
                HasFloatField(script, "movementSpeed"))
            {
                return script;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether a script has a float field with the given name.
    /// This works for both public and private fields.
    /// </summary>
    /// <param name="script">The script to inspect.</param>
    /// <param name="fieldName">The name of the field to look for.</param>
    /// <returns>True if the field exists and is a float.</returns>
    private bool HasFloatField(MonoBehaviour script, string fieldName)
    {
        FieldInfo field = script.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        return field != null && field.FieldType == typeof(float);
    }

    /// <summary>
    /// Temporarily increases the player's movement speed,
    /// waits for the duration, then restores the original speed.
    /// </summary>
    /// <param name="movementScript">The movement script found on the player.</param>
    private IEnumerator ApplySpeedBoost(MonoBehaviour movementScript)
    {
        // Try to find one of the common speed field names.
        FieldInfo speedField =
            movementScript.GetType().GetField("speed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            movementScript.GetType().GetField("moveSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            movementScript.GetType().GetField("movementSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Stop if no speed field is found.
        if (speedField == null)
        {
            yield break;
        }

        // Save the player's original speed before applying the boost.
        float originalSpeed = (float)speedField.GetValue(movementScript);

        // Apply the speed boost.
        speedField.SetValue(movementScript, originalSpeed * boostMultiplier);

        Debug.Log("Speed Boost Activated!");

        // Wait for the boost duration.
        yield return new WaitForSeconds(duration);

        // Restore the original speed if the player still exists.
        if (movementScript != null)
        {
            speedField.SetValue(movementScript, originalSpeed);
            Debug.Log("Speed Boost Ended!");
        }

        // Remove the power-up object after the effect ends.
        Destroy(gameObject);
    }

    /// <summary>
    /// Hides the power-up after collection by disabling its sprite and collider.
    /// The object is kept alive temporarily so the coroutine can finish.
    /// </summary>
    private void HidePowerUp()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Collider2D collider2D = GetComponent<Collider2D>();

        // Hide the visual sprite.
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Disable the collider so it cannot be collected again.
        if (collider2D != null)
        {
            collider2D.enabled = false;
        }
    }
}