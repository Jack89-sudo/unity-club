using UnityEngine;

public class Item : MonoBehaviour
{
    public int itemID;  // Unique ID for the item
    public string itemName; // Optional: Name of the item (e.g., "Key", "Lollipop")
    public Sprite itemIcon; // Optional: Icon for the item (can be used in UI)

    // This function can be used to trigger item-related behavior
    public void OnPickUp()
    {
        // Here you can handle any logic when the item is picked up
        // E.g., play a sound, trigger an animation, etc.
        Debug.Log("Picked up: " + itemName);
    }
}
