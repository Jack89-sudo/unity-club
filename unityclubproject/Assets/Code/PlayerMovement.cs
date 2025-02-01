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

    [Header("Camera Settings")]
    public Camera playerCamera; // Allow setting the camera manually in Unity

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;

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

        if (Input.GetKey(KeyCode.LeftShift) && staminaBar.currentStamina > 0) // Only run if stamina available
        {
            targetSpeed = runSpeed;
            staminaBar.ChangeStamina(-staminaBar.staminaDrainRate * Time.deltaTime); // Drain stamina
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSpeed = slowSpeed;
        }
        else
        {
            targetSpeed = walkSpeed;
            staminaBar.ChangeStamina(staminaBar.staminaRegenRate * Time.deltaTime); // Regenerate stamina
        }

        RotateTowardsMouse(); // Rotate player to face the cursor
    }

    void FixedUpdate()
    {
        // Adjust speed smoothly
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // Apply movement
        rb.linearVelocity = movementInput * currentSpeed;
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
}
