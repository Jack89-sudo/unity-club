using Unity.VisualScripting;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public Item item; // Reference to the item script

    void OnMouseDown()
    {
        // This simulates picking up the item when clicked
        Inventory inventory = FindObjectOfType<Inventory>();
        if (inventory != null)
        {
            inventory.AddItem(item);
            Destroy(gameObject); // Destroy the item in the world after pickup
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            OnMouseDown();
        }
    }

}
