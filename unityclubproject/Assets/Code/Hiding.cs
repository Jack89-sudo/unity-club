using UnityEngine;

public class HideableCompartment : MonoBehaviour
{
    private Transform player;
    private Vector3 originalPlayerPosition;
    private bool isPlayerHidden = false;

    private void Update()
    {
        if (player != null && Input.GetKeyDown(KeyCode.E))
        {
            if (isPlayerHidden)
            {
                ExitCompartment();
            }
            else
            {
                EnterCompartment();
            }
        }
    }

    private void EnterCompartment()
    {
        originalPlayerPosition = player.position;
        player.gameObject.SetActive(false); // Hides entire player GameObject
        isPlayerHidden = true;
    }

    private void ExitCompartment()
    {
        player.position = originalPlayerPosition;
        player.gameObject.SetActive(true); // Shows entire player GameObject
        isPlayerHidden = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
        }
    }
}
