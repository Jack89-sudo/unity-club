using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

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
    public float dropOffset = 1f;

    [Header("Sanity Settings")]
    public Slider sanityBar;
    public float maxSanity = 100f;
    private float currentSanity;

    [Header("Audio Clips")]
    [Tooltip("Looped when walking")]
    public AudioClip footstepWalkClip;
    [Tooltip("Looped when running")]
    public AudioClip footstepRunClip;
    [Tooltip("One-shot when toggling flashlight")]
    public AudioClip flashlightClip;
    [Tooltip("One-shot when eating apple")]
    public AudioClip appleClip;
    [Tooltip("One-shot when eating lollipop")]
    public AudioClip lollipopClip;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private List<Item> inventory = new List<Item>();
    private int equippedIndex = -1;

    // audio sources
    private AudioSource footstepSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // Footsteps: looping
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.volume = footstepVolume;

        // SFX: one-shot
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed;
        currentMoveState = MoveState.Idle;

        if (playerCamera == null)
            playerCamera = Camera.main;

        foreach (var slot in itemUISlots)
            slot.enabled = false;
        if (handRenderer != null) handRenderer.enabled = false;
        if (flashlightLight != null) flashlightLight.enabled = false;

        if (sanityBar != null)
        {
            currentSanity = maxSanity;
            sanityBar.minValue = 0f;
            sanityBar.maxValue = maxSanity;
            sanityBar.value = currentSanity;
        }
    }

    void Update()
    {
        HandleMovementInput();
        RotateTowardsMouse();

        ManageFootstepAudio();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (equippedIndex >= 0)
            {
                var itm = inventory[equippedIndex];
                if (itm.itemType == ItemType.Lollipop)
                    UseLollipop();
                else if (itm.itemType == ItemType.Apple)
                    UseApple();
                else
                    TryPickUpItem();
            }
            else
            {
                TryPickUpItem();
            }
        }

        // Equip by number
        for (int i = 0; i < itemUISlots.Count; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                EquipItem(i);

        // Toggle flashlight (with sound)
        if (equippedIndex >= 0 && inventory[equippedIndex].itemType == ItemType.Flashlight)
        {
            if (Input.GetKeyDown(KeyCode.F) && flashlightLight != null)
            {
                flashlightLight.enabled = !flashlightLight.enabled;
                sfxSource.PlayOneShot(flashlightClip, sfxVolume);
            }
        }

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
        else if (movementInput.sqrMagnitude > 0f)
        {
            currentMoveState = MoveState.Walking;
            targetSpeed = walkSpeed;
            AdjustCameraZoom(defaultZoom);
        }
        else
        {
            currentMoveState = MoveState.Idle;
            targetSpeed = 0f;
            AdjustCameraZoom(defaultZoom);
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
    }

    void ManageFootstepAudio()
    {
        // Only play when walking or running and moving
        bool moving = movementInput.sqrMagnitude > 0.01f;
        if (moving && (currentMoveState == MoveState.Walking || currentMoveState == MoveState.Running))
        {
            AudioClip desired = currentMoveState == MoveState.Running ? footstepRunClip : footstepWalkClip;
            if (footstepSource.clip != desired)
            {
                footstepSource.clip = desired;
                footstepSource.volume = footstepVolume;
                footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }
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
            itemUISlots[slotIdx].sprite = item.icon;
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
            return;
        }

        equippedIndex = index;
        var itm = inventory[index];
        if (handRenderer != null)
        {
            handRenderer.sprite = itm.icon;
            handRenderer.enabled = true;
        }

        switch (itm.itemType)
        {
            case ItemType.Apple: Debug.Log("Apple equipped: press E to boost speed"); break;
            case ItemType.Lollipop: Debug.Log("Lollipop equipped: press E to restore sanity"); break;
            case ItemType.Flashlight: Debug.Log("Flashlight equipped (F to toggle)"); break;
            case ItemType.Key: Debug.Log("Key equipped: can unlock doors"); break;
        }
    }

    void DropEquippedItem()
    {
        if (equippedIndex < 0 || equippedIndex >= inventory.Count) return;
        var itm = inventory[equippedIndex];
        if (itm.worldPrefab != null)
        {
            Vector3 dropPos = transform.position + transform.right * dropOffset;
            var obj = Instantiate(itm.worldPrefab, dropPos, Quaternion.identity);
            obj.GetComponent<ItemPickup>().item = itm;
        }
        RemoveEquippedItem();
    }

    public void RemoveEquippedItem()
    {
        if (equippedIndex < 0 || equippedIndex >= inventory.Count) return;
        inventory.RemoveAt(equippedIndex);
        for (int i = 0; i < itemUISlots.Count; i++)
        {
            if (i < inventory.Count)
            {
                itemUISlots[i].sprite = inventory[i].icon;
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

    private void UseLollipop()
    {
        var sanityComp = GetComponent<PlayerSanity>();
        if (sanityComp != null) sanityComp.Refill(30f);
        sfxSource.PlayOneShot(lollipopClip, sfxVolume);
        RemoveEquippedItem();
        Debug.Log("Sanity restored by 30");
    }

    private void UseApple()
    {
        walkSpeed *= 1.2f;
        runSpeed *= 1.2f;
        slowSpeed *= 1.2f;
        sfxSource.PlayOneShot(appleClip, sfxVolume);
        RemoveEquippedItem();
        Debug.Log("Movement speeds increased by 20%");
    }

    void RotateTowardsMouse()
    {
        if (playerCamera == null) return;
        Vector3 m = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;
        Vector2 dir = (m - transform.position).normalized;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);
    }

    void SmoothCameraFollow()
    {
        if (playerCamera == null) return;
        Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
    }

    void AdjustCameraZoom(float z)
    {
        if (playerCamera != null)
            playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, z, Time.deltaTime * zoomSpeed);
    }
}
