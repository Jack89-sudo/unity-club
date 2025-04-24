using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI")]
    public Transform itemsParent;       // Panel (with a Horizontal/Vertical LayoutGroup)
    public GameObject slotPrefab;       // Prefab: an Image under a LayoutGroup

    private readonly List<Item> items = new List<Item>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Call this when the player picks up an item.
    /// </summary>
    public void AddItem(Item item)
    {
        if (items.Contains(item)) return;
        items.Add(item);

        // Instantiate a new UI slot:
        var go = Instantiate(slotPrefab, itemsParent);
        var img = go.GetComponent<Image>();
        img.sprite = item.itemIcon;

        // Optional: store reference on the slot for future (e.g. hover tooltip)
    }
}
