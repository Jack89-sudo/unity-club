using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    public GameObject[] lights; // Assign light GameObjects in the Inspector
    private bool isOn = true; // Default light state
    private bool playerInRange = false; // Track if player is in range
    public Collider2D playertrigger;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleLights();
        }
    }

    void ToggleLights()
    {
        isOn = !isOn; // Toggle state

        foreach (GameObject light in lights)
        {
            light.SetActive(isOn); // Enable/Disable light objects
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == playertrigger) // Ensure player has "Player" tag
        {
            playerInRange = true;
            Debug.Log("touched");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == playertrigger)
        {
            playerInRange = false;
        }
    }
}
