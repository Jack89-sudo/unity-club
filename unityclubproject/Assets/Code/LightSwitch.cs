using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    public GameObject[] lights; // Assign light GameObjects in the Inspector
    public bool isOn = true; // Default light state
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
        // Toggle state
        Debug.Log("yea");
        foreach (GameObject light in lights)
        {
            light.SetActive(isOn); // Enable/Disable light objects
        }
    }

    private void OnTriggerEnter2D(Collider2D playertrigger)
    {

            playerInRange = true;
            Debug.Log("touched");
      
    }

    private void OnTriggerExit2D(Collider2D playertrigger)
    {

            playerInRange = false;
        
    }
}
