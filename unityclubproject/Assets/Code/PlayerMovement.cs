using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float slowSpeed = 1.5f;
    public float acceleration = 10f;

    [Header("Camera Settings")]
    public Camera playerCamera; // Allow setting the camera manually in Unity
    public float defaultZoom = 5f;
    public float runZoom = 7f;
    public float slowZoom = 3.5f;
    public float zoomSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;
    private bool inRoom = false;
    private Vector3 roomCenter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed;

        // If no camera is assigned, use the main camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSpeed = slowSpeed;
            AdjustCameraZoom(slowZoom);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            targetSpeed = runSpeed;
            AdjustCameraZoom(runZoom);
        }
        else
        {
            targetSpeed = walkSpeed;
            AdjustCameraZoom(defaultZoom);
        }

        RotateTowardsMouse(); // Rotate player to face the cursor
    }

    void FixedUpdate()
    {
        // Adjust speed smoothly
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // Apply movement
        rb.linearVelocity = movementInput * currentSpeed;

        // Make the camera follow the player or stay centered in the room
        if (playerCamera != null)
        {
            if (inRoom)
            {
                playerCamera.transform.position = new Vector3(roomCenter.x, roomCenter.y, playerCamera.transform.position.z);
            }
            else
            {
                playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z);
            }
        }
    }

    void RotateTowardsMouse()
    {
        if (playerCamera == null) return; // Safety check

        // Get mouse position in world space
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; // Ensure it's at the same Z level

        // Calculate direction and angle
        Vector2 direction = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Rotate the player towards the mouse
        rb.rotation = angle;
    }

    void AdjustCameraZoom(float targetZoom)
    {
        if (playerCamera != null)
        {
            playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("roomtriggerbox"))
        {
            inRoom = true;
            roomCenter = other.bounds.center;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("roomtriggerbox"))
        {
            inRoom = false;
        }
    }
}