using UnityEngine;

public class Room1trigger : MonoBehaviour
{
    public string playerTag = "Player"; // Default tag name

    // Assign the player GameObject in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))

        {
            control gameControl = FindFirstObjectByType<control>();
            if (gameControl != null && gameControl.currentTask == 0)
            {
                gameControl.LeaveRoom();
                Debug.Log("Player triggered task completion: Leave the room");
            }
        }
    }
}
