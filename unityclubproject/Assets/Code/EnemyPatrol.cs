using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] waypoints;  // Assign waypoints in the Inspector
    public float speed = 2f;  // Movement speed
    public int currentWaypointIndex = 0;  // Start at first waypoint
    public bool loop = true;  // Should it loop the path?

    void Update()
    {
        if (waypoints.Length == 0) return; // Ensure waypoints exist

        // Move towards the current waypoint
        transform.position = Vector2.MoveTowards(transform.position, waypoints[currentWaypointIndex].position, speed * Time.deltaTime);

        // Check if we've reached the waypoint
        if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
        {
            currentWaypointIndex++; // Move to the next waypoint

            // Loop back to the first waypoint if at the end
            if (currentWaypointIndex >= waypoints.Length)
            {
                if (loop)
                    currentWaypointIndex = 0;
                else
                    enabled = false;  // Stop moving if not looping
            }
        }
    }
}
