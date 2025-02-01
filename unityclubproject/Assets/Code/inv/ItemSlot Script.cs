using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public Image icon; // Image that will hold the item icon
    public Item item;  // The item in this slot

    public void SetItem(Item newItem)
    {
        item = newItem;
        icon.sprite = item.itemIcon;
        icon.enabled = true; // Make sure the icon is visible
    }

    public void ClearSlot()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false;
    }
}
