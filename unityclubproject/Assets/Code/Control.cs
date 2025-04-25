using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class control : MonoBehaviour
{
    [Header("Light Controllers")]
    public List<LightController2D> lightControllers;

    [Header("Difficulty Settings")]
    [Tooltip("Starts at 1. Higher values make lights go off longer and more often.")]
    public int difficulty = 1;

    [Header("Timing Settings (in seconds)")]
    public float minEventInterval = 120f;
    public float maxEventInterval = 180f;
    public float baseOffDuration = 5f;

    [Header("Task Tracking")]
    public TextMeshProUGUI taskText;
    public int currentTask = 0;

    private int homeworkCompleted = 0;

    [Header("Escape Exit")]
    [Tooltip("Trigger collider to mark level exit; only active when Task is Escape.")]
    public Collider2D exitTrigger;

    void Start()
    {
        StartCoroutine(ManageLightsRoutine());
        UpdateTaskDisplay();

        // Ensure exit trigger is disabled until escape task
        if (exitTrigger != null)
            exitTrigger.enabled = false;
    }

    IEnumerator ManageLightsRoutine()
    {
        while (true)
        {
            float interval = Random.Range(minEventInterval, maxEventInterval) / Mathf.Clamp(difficulty, 1f, 100f);
            yield return new WaitForSeconds(interval);

            if (lightControllers.Count > 0)
            {
                int randomIndex = Random.Range(0, lightControllers.Count);
                var chosen = lightControllers[randomIndex];
                if (chosen != null)
                {
                    chosen.SetLights(false);
                    float offTime = baseOffDuration * Mathf.Clamp(difficulty, 1f, 10f);
                    yield return new WaitForSeconds(offTime);
                    chosen.SetLights(true);
                }
            }
        }
    }

    public void CompleteHomework()
    {
        if (currentTask >= 1 && currentTask <= 7)
        {
            homeworkCompleted++;
            if (homeworkCompleted >= 6)
                currentTask = 8; // Escape
            else
                currentTask = 1 + homeworkCompleted;

            UpdateTaskDisplay();
        }
    }

    public void LeaveRoom()
    {
        if (currentTask == 0)
        {
            currentTask = 1;
            UpdateTaskDisplay();
        }
    }

    private void UpdateTaskDisplay()
    {
        if (taskText == null) return;
        switch (currentTask)
        {
            case 0:
                taskText.text = "Current Task: Leave the room";
                break;
            case 1:
                taskText.text = "Current Task: Complete homework (0/6)";
                break;
            case >= 2 and <= 7:
                int completed = currentTask - 1;
                taskText.text = $"Current Task: Complete homework ({completed}/6)";
                break;
            case 8:
                taskText.text = "Current Task: Escape";
                // activate exit trigger
                if (exitTrigger != null)
                    exitTrigger.enabled = true;
                break;
            default:
                taskText.text = "No current task";
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentTask == 8 && exitTrigger != null && other == exitTrigger)
        {
            // ensure it's the player
            var player = other.attachedRigidbody?.GetComponent<PlayerMovement>();
            if (player != null)
            {
                SceneManager.LoadScene("win");
            }
        }
    }
}
