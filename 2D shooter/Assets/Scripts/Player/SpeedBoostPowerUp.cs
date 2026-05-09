using System.Collections;
using System.Reflection;
using UnityEngine;

public class SpeedBoostPowerUp : MonoBehaviour
{
    [Header("Power Up Settings")]
    public float boostMultiplier = 1.8f;
    public float duration = 5f;

    private bool used = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        MonoBehaviour movementScript = FindMovementScript(other.gameObject);

        if (movementScript == null)
        {
            Debug.LogWarning("SpeedBoostPowerUp: Could not find a player movement script with speed / moveSpeed / movementSpeed.");
            return;
        }

        used = true;
        StartCoroutine(ApplySpeedBoost(movementScript));
        HidePowerUp();
    }

    private MonoBehaviour FindMovementScript(GameObject player)
    {
        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            if (HasFloatField(script, "speed") ||
                HasFloatField(script, "moveSpeed") ||
                HasFloatField(script, "movementSpeed"))
            {
                return script;
            }
        }

        return null;
    }

    private bool HasFloatField(MonoBehaviour script, string fieldName)
    {
        FieldInfo field = script.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        return field != null && field.FieldType == typeof(float);
    }

    private IEnumerator ApplySpeedBoost(MonoBehaviour movementScript)
    {
        FieldInfo speedField =
            movementScript.GetType().GetField("speed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            movementScript.GetType().GetField("moveSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            movementScript.GetType().GetField("movementSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (speedField == null)
        {
            yield break;
        }

        float originalSpeed = (float)speedField.GetValue(movementScript);
        speedField.SetValue(movementScript, originalSpeed * boostMultiplier);

        Debug.Log("Speed Boost Activated!");

        yield return new WaitForSeconds(duration);

        if (movementScript != null)
        {
            speedField.SetValue(movementScript, originalSpeed);
            Debug.Log("Speed Boost Ended!");
        }

        Destroy(gameObject);
    }

    private void HidePowerUp()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Collider2D collider2D = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (collider2D != null)
        {
            collider2D.enabled = false;
        }
    }
}