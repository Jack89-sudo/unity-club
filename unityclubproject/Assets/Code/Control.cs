using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("Task Tracking")]
    public TextMeshProUGUI taskText; // Assign in Inspector
    public int currentTask = 0;      // Starts at 0

    private int homeworkCompleted = 0;

    void Start()
    {
        StartCoroutine(ManageLightsRoutine());
        UpdateTaskDisplay();
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
                LightController2D chosen = lightControllers[randomIndex];

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
            {
                currentTask = 8; // Move to "Escape"
            }
            else
            {
                currentTask = 1 + homeworkCompleted; // Next homework task
            }
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

    public void Escape()
    {
        if (currentTask == 8)
        {
            taskText.text = "You escaped!";
            // Add logic here to end the game or trigger cutscene
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
                break;
            default:
                taskText.text = "No current task";
                break;
        }
    }

}
