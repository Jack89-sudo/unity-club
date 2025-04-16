using UnityEngine;
using UnityEngine.UI;

public class PlayerSanity : MonoBehaviour
{
    [Header("Sanity Settings")]
    [Tooltip("The player's starting sanity value.")]
    public float sanity = 100f;

    [Tooltip("Radius around the player within which sanity will decrease if an enemy is present.")]
    public float sanityLossRadius = 5f;

    [Tooltip("Rate at which sanity decreases per second when in range of an enemy.")]
    public float sanityLossRate = 10f;

    [Header("Enemy Settings")]
    [Tooltip("Reference to the enemy GameObject that will trigger sanity loss.")]
    public GameObject enemyGameObject;

    [Header("UI Settings")]
    [Tooltip("Reference to the UI Slider that visualizes the player's sanity.")]
    public Slider sanitySlider;

    void Start()
    {
        // Initialize the slider's maximum value and set its initial value.
        if (sanitySlider != null)
        {
            sanitySlider.maxValue = 100f;  // Assumes maximum sanity is 100
            sanitySlider.value = sanity;
        }
    }

    void Update()
    {
        // Find all colliders within the defined sanity loss radius.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, sanityLossRadius);
        bool enemyInRange = false;

        // Check if any of the colliders belong to the assigned enemy GameObject.
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == enemyGameObject)
            {
                enemyInRange = true;
                break;
            }
        }

        // Decrease sanity over time if the enemy is in range.
        if (enemyInRange)
        {
            sanity -= sanityLossRate * Time.deltaTime;
        }

        // Clamp sanity between 0 and 100.
        sanity = Mathf.Clamp(sanity, 0f, 100f);

        // Update the UI slider to reflect the current sanity.
        if (sanitySlider != null)
        {
            sanitySlider.value = sanity;
        }

        // Output the current sanity value to the Console.
        Debug.Log("Player Sanity: " + sanity.ToString("F2"));
    }

    // Visualize the sanity loss radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sanityLossRadius);
    }
}
