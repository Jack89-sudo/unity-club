using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class Timelimit : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    // Assign via Inspector (your Canvas UI Text)
    public float countdownMinutes = 5; // Adjustable in Inspector

    private float remainingTime;
    private bool isWithinTimeWindow = false;

    void Start()
    {
        DateTime now = DateTime.Now;
        DateTime startWindow = now.Date.AddHours(19);  // 7:00 PM
        DateTime endWindow = now.Date.AddDays(now.Hour < 9 ? 0 : 1).AddHours(9); // 9:00 AM next day if past 9

        // Check if current time is between 7 PM and 9 AM
        if (now >= startWindow || now <= endWindow)
        {
            isWithinTimeWindow = true;
            remainingTime = countdownMinutes * 60;
        }
        else
        {
            isWithinTimeWindow = false;
            timerText.text = "Timer not active";
        }
    }

    void Update()
    {
        if (!isWithinTimeWindow) return;

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            TimeSpan time = TimeSpan.FromSeconds(remainingTime);
            timerText.text = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        }
        else
        {
            timerText.text = "Time's up!";
        }
    }
}
