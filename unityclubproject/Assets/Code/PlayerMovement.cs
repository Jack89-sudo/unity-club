using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float slowSpeed = 1.5f;
    public float acceleration = 10f;
    public StaminaBar staminaBar; // Reference to StaminaBar script

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;
    private Camera mainCamera;
    private bool isInRoom = false;
    private Transform roomTransform;
    private float originalCameraSize;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed;
        mainCamera = Camera.main;
        originalCameraSize = mainCamera.orthographicSize;
    }

    void Update()
    {
        // Get movement input
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;

        // Handle speed changes based on input
        if (Input.GetKey(KeyCode.LeftShift) && staminaBar.currentStamina > 0)
        {
            targetSpeed = runSpeed;
            staminaBar.ChangeStamina(-staminaBar.staminaDrainRate * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSpeed = slowSpeed;
        }
        else
        {
            targetSpeed = walkSpeed;
            staminaBar.ChangeStamina(staminaBar.staminaRegenRate * Time.deltaTime);
        }

        // Rotate player to face mouse cursor
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Handle camera behavior
        if (!isInRoom)
        {
            // Lock camera to player
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
            mainCamera.orthographicSize = originalCameraSize;
        }
        else if (roomTransform != null)
        {
            // Move camera to center of the room and expand view
            mainCamera.transform.position = new Vector3(roomTransform.position.x, roomTransform.position.y, mainCamera.transform.position.z);
        }
    }

    void FixedUpdate()
    {
        // Adjust speed smoothly
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // Apply movement without being affected by rotation
        rb.linearVelocity = movementInput * currentSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Room"))
        {
            isInRoom = true;
            roomTransform = collision.transform;
            mainCamera.orthographicSize = Mathf.Max(collision.bounds.size.x, collision.bounds.size.y) / 2f;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Room"))
        {
            isInRoom = false;
            roomTransform = null;
            mainCamera.orthographicSize = originalCameraSize;
        }
    }
}
