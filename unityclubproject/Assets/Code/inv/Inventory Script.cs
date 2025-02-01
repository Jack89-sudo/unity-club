using UnityEngine;

public class Inventory : MonoBehaviour
{
    public ItemSlot[] itemSlots; // Array to hold 4 inventory slots

    public void AddItem(Item item)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].item == null) // Empty slot
            {
                itemSlots[i].SetItem(item);
                break;
            }
        }
    }

    public void RemoveItem(int slotIndex)
    {
        if (itemSlots[slotIndex].item != null)
        {
            itemSlots[slotIndex].ClearSlot();
        }
    }
}
