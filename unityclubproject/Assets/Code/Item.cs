using UnityEngine;

public enum ItemType
{
    Apple,
    Lollipop,
    Flashlight,
    Key
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Identity")]
    public ItemType itemType;

    [Header("Display")]
    public string itemName;
    public Sprite itemIcon;

    [Header("World Prefab")]
    public GameObject worldPrefab; // Assign prefab with ItemPickup + SpriteRenderer
}