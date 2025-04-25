// PlayerSanity.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class PlayerSanity : MonoBehaviour
{
    [Header("Sanity Settings")]
    [Tooltip("The player's starting sanity value.")]
    public float sanity = 100f;

    [Tooltip("Radius around the player within which sanity will decrease if a matching object sprite is present.")]
    public float sanityLossRadius = 5f;

    [Header("Sprite Loss Settings")]
    [Tooltip("List of sprites and their associated sanity loss rates.")]
    public List<SpriteLossEntry> spriteLossEntries;

    [System.Serializable]
    public class SpriteLossEntry
    {
        public Sprite sprite;
        [Tooltip("Sanity loss per second when this sprite is within range.")]
        public float lossRate;
    }

    [Header("UI Settings")]
    [Tooltip("Reference to the UI Slider that visualizes the player's sanity.")]
    public Slider sanitySlider;

    private float initialSanity;

    void Start()
    {
        initialSanity = sanity;
        if (sanitySlider != null)
        {
            sanitySlider.maxValue = initialSanity;
            sanitySlider.value = sanity;
        }
    }

 private void GameOver()
    {
        Debug.Log("Game Over: Player has been killed.");
        SceneManager.LoadScene("EndingScreen");
    }
    void Update()
    {
        // Gather all colliders in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, sanityLossRadius);
        float totalLossRate = 0f;

        if(sanity <= 0){
            GameOver();
        }
        // For each collider, check its sprite against our entries
        foreach (var col in colliders)
        {
            var sr = col.GetComponent<SpriteRenderer>();
            if (sr == null || spriteLossEntries == null) continue;

            foreach (var entry in spriteLossEntries)
            {
                if (sr.sprite == entry.sprite)
                {
                    totalLossRate += entry.lossRate;
                    break;
                }
            }
        }

        // Apply sanity loss
        if (totalLossRate > 0f)
            sanity -= totalLossRate * Time.deltaTime;

        // Clamp
        sanity = Mathf.Clamp(sanity, 0f, initialSanity);

        // Update UI
        if (sanitySlider != null)
            sanitySlider.value = sanity;
    }

    /// <summary>
    /// Refill sanity by the given amount, clamped to the original maximum.
    /// </summary>
    public void Refill(float amount)
    {
        sanity = Mathf.Clamp(sanity + amount, 0f, initialSanity);
        if (sanitySlider != null)
            sanitySlider.value = sanity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sanityLossRadius);
    }
}
