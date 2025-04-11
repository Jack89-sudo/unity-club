using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class control : MonoBehaviour
{
    [Header("Light Controllers")]
    public List<LightController2D> lightControllers;

    [Header("Difficulty Settings")]
    [Tooltip("Starts at 1. Higher values make lights go off longer and more often.")]
    public int difficulty = 1;

    [Header("Timing Settings (in seconds)")]
    public float minEventInterval = 120f; // 2 minutes
    public float maxEventInterval = 180f; // 3 minutes
    public float baseOffDuration = 5f;    // Base duration lights are off

    void Start()
    {
        StartCoroutine(ManageLightsRoutine());
    }

    IEnumerator ManageLightsRoutine()
    {
        while (true)
        {
            // Shorter wait between events as difficulty increases
            float interval = Random.Range(minEventInterval, maxEventInterval) / Mathf.Clamp(difficulty, 1f, 100f);
            yield return new WaitForSeconds(interval);

            // Random light controller
            if (lightControllers.Count > 0)
            {
                int randomIndex = Random.Range(0, lightControllers.Count);
                LightController2D chosen = lightControllers[randomIndex];

                if (chosen != null)
                {
                    chosen.SetLights(false);

                    // Lights stay off longer as difficulty increases
                    float offTime = baseOffDuration * Mathf.Clamp(difficulty, 1f, 10f);
                    yield return new WaitForSeconds(offTime);

                    chosen.SetLights(true);
                }
            }

            // No auto-difficulty scaling here
        }
    }
}
