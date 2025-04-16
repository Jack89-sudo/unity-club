using UnityEngine;
using UnityEngine.UI; // For UI Image manipulation

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

    [Header("Item Pickup Settings")]
    public float pickupRange = 2f; // Range in which the player can pick up items
    private Item itemInRange; // The item that is within pickup range

    [Header("UI Settings")]
    public Image keyImage; // UI element for the key
    public Image lollipopImage; // UI element for the lollipop
    private bool hasKey = false;
    private bool hasLollipop = false;

    // Enum to track movement state
    public enum MoveState
    {
        Idle,
        Walking,
        Running,
        Slow
    }

    public MoveState currentMoveState; // Current movement state

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed;
        currentMoveState = MoveState.Idle;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        // Movement input
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        if (movementInput.sqrMagnitude > 1)
        {
            movementInput = movementInput.normalized;
        }

        // Adjust speed and state based on input
        if (Input.GetKey(KeyCode.LeftControl)) // Slow speed (e.g. crouch or walking)
        {
            targetSpeed = slowSpeed;
            currentMoveState = MoveState.Slow;
            AdjustCameraZoom(slowZoom);
        }
        else if (Input.GetKey(KeyCode.LeftShift)) // Running speed
        {
            targetSpeed = runSpeed;
            currentMoveState = MoveState.Running;
            AdjustCameraZoom(runZoom);
        }
        else // Walking speed
        {
            targetSpeed = walkSpeed;
            currentMoveState = MoveState.Walking;
            AdjustCameraZoom(defaultZoom);
        }

        // Handle item pickup
        if (itemInRange != null && Input.GetKeyDown(KeyCode.E))
        {
            PickUpItem(itemInRange);
        }

        // Rotate player towards mouse
        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        // Move player
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = movementInput * currentSpeed; // Use `rb.velocity` instead of `linearVelocity`

        // Move camera
        if (playerCamera != null)
        {
            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z);
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, Time.deltaTime * cameraMoveSpeed);
        }
    }

    void RotateTowardsMouse()
    {
        if (playerCamera == null) return;

        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;
        Vector2 direction = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void AdjustCameraZoom(float targetZoom)
    {
        if (playerCamera != null)
        {
            playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        }
    }

    // Handle the pickup of the item
    void PickUpItem(Item item)
    {
        // Call the item’s OnPickUp function to perform any behavior on pickup
        item.OnPickUp();

        // Check which item was picked up and update the UI
        if (item.itemID == 1 && !hasKey) // Assuming Key has ID 1
        {
            hasKey = true;
            keyImage.gameObject.SetActive(true); // Show the key image on the UI
        }
        else if (item.itemID == 2 && !hasLollipop) // Assuming Lollipop has ID 2
        {
            hasLollipop = true;
            lollipopImage.gameObject.SetActive(true); // Show the lollipop image on the UI
        }

        // Destroy the item from the world after pickup
        Destroy(item.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player enters pickup range of an item
        Item item = other.GetComponent<Item>();
        if (item != null && Vector2.Distance(transform.position, item.transform.position) <= pickupRange)
        {
            itemInRange = item;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Reset itemInRange when the player leaves the pickup range
        Item item = other.GetComponent<Item>();
        if (item != null)
        {
            itemInRange = null;
        }
    }
}
