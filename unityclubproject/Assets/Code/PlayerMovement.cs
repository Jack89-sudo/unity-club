using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float slowSpeed = 1.5f;
    public float acceleration = 10f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float defaultZoom = 5f;
    public float runZoom = 7f;
    public float slowZoom = 3.5f;
    public float zoomSpeed = 5f;
    public float cameraMoveSpeed = 3f;

    public enum MoveState { Idle, Walking, Running, Slow }
    public MoveState currentMoveState;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;

    [Header("Pickup Settings")]
    public Collider2D pickuprange;

    [Header("Inventory UI")]
    public List<Image> itemUISlots;

    [Header("Equip Settings")]
    public SpriteRenderer handRenderer;

    [Header("Flashlight Settings")]
    public Light2D flashlightLight;

    [Header("Drop Settings")]
    public float dropOffset = 1f; // distance in front of player to drop

    private List<Item> inventory = new List<Item>();
    private int equippedIndex = -1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed;
        currentMoveState = MoveState.Idle;

        if (playerCamera == null)
            playerCamera = Camera.main;

        // hide UI, hand, flashlight
        foreach (var slot in itemUISlots)
            slot.enabled = false;
        if (handRenderer != null)
            handRenderer.enabled = false;
        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    void Update()
    {
        HandleMovementInput();
        RotateTowardsMouse();

        if (Input.GetKeyDown(KeyCode.E))
            TryPickUpItem();

        // Equip by number
        for (int i = 0; i < itemUISlots.Count; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                EquipItem(i);

        // Toggle flashlight
        if (equippedIndex >= 0 && inventory[equippedIndex].itemType == ItemType.Flashlight)
            if (Input.GetKeyDown(KeyCode.F) && flashlightLight != null)
                flashlightLight.enabled = !flashlightLight.enabled;

        // Drop item
        if (Input.GetKeyDown(KeyCode.G))
            DropEquippedItem();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movementInput * currentSpeed;
        SmoothCameraFollow();
    }

    void HandleMovementInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        if (movementInput.sqrMagnitude > 1) movementInput.Normalize();

        if (Input.GetKey(KeyCode.LeftControl))
        {
            currentMoveState = MoveState.Slow;
            targetSpeed = slowSpeed;
            AdjustCameraZoom(slowZoom);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveState = MoveState.Running;
            targetSpeed = runSpeed;
            AdjustCameraZoom(runZoom);
        }
        else
        {
            currentMoveState = MoveState.Walking;
            targetSpeed = walkSpeed;
            AdjustCameraZoom(defaultZoom);
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
    }

    void TryPickUpItem()
    {
        if (pickuprange == null) return;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        Collider2D[] picks = new Collider2D[16];
        int count = pickuprange.Overlap(filter, picks);

        for (int i = 0; i < count; i++)
        {
            var pickup = picks[i]?.GetComponent<ItemPickup>();
            if (pickup != null && pickup.item != null)
            {
                AddToInventory(pickup.item);
                Destroy(pickup.gameObject);
                break;
            }
        }
    }

    void AddToInventory(Item item)
    {
        inventory.Add(item);
        int slotIdx = inventory.Count - 1;
        if (slotIdx < itemUISlots.Count)
        {
            itemUISlots[slotIdx].sprite = item.itemIcon;
            itemUISlots[slotIdx].enabled = true;
        }
        else
        {
            Debug.LogWarning("No UI slot for item " + item.itemName);
        }
    }

    void EquipItem(int index)
    {
        if (index < 0 || index >= inventory.Count)
        {
            equippedIndex = -1;
            if (handRenderer != null) handRenderer.enabled = false;
            if (flashlightLight != null) flashlightLight.enabled = false;
            return;
        }
        equippedIndex = index;
        Item itm = inventory[index];

        switch (itm.itemType)
        {
            case ItemType.Apple:
                Debug.Log("Eaten apple: restores health");
                break;
            case ItemType.Lollipop:
                Debug.Log("Lollipop: temporary speed boost");
                break;
            case ItemType.Flashlight:
                Debug.Log("Flashlight equipped (press F to toggle)");
                if (flashlightLight != null) flashlightLight.enabled = false;
                break;
            case ItemType.Key:
                Debug.Log("Key equipped: can unlock doors");
                break;
        }

        if (handRenderer != null)
        {
            handRenderer.sprite = itm.itemIcon;
            handRenderer.enabled = true;
        }
    }

    void DropEquippedItem()
    {
        if (equippedIndex < 0 || equippedIndex >= inventory.Count)
            return;

        Item itm = inventory[equippedIndex];
        if (itm.worldPrefab != null)
        {
            Vector3 dropPos = transform.position + transform.right * dropOffset;
            var obj = Instantiate(itm.worldPrefab, dropPos, Quaternion.identity);
            var pickup = obj.GetComponent<ItemPickup>();
            if (pickup != null)
                pickup.item = itm;
        }

        // remove from inventory & UI
        RemoveEquippedItem();
    }

    public bool HasEquippedKey()
    {
        return equippedIndex >= 0 && inventory[equippedIndex].itemType == ItemType.Key;
    }

    public void RemoveEquippedItem()
    {
        if (equippedIndex < 0 || equippedIndex >= inventory.Count)
            return;

        inventory.RemoveAt(equippedIndex);

        for (int i = 0; i < itemUISlots.Count; i++)
        {
            if (i < inventory.Count)
            {
                itemUISlots[i].sprite = inventory[i].itemIcon;
                itemUISlots[i].enabled = true;
            }
            else
            {
                itemUISlots[i].enabled = false;
            }
        }

        if (handRenderer != null) handRenderer.enabled = false;
        if (flashlightLight != null) flashlightLight.enabled = false;
        equippedIndex = -1;
    }

    void RotateTowardsMouse()
    {
        if (playerCamera == null) return;
        Vector3 m = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;
        Vector2 d = (m - transform.position).normalized;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);
    }

    void SmoothCameraFollow()
    {
        if (playerCamera == null) return;
        Vector3 target = new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, target, Time.deltaTime * cameraMoveSpeed);
    }

    void AdjustCameraZoom(float z)
    {
        if (playerCamera != null)
            playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, z, Time.deltaTime * zoomSpeed);
    }
}