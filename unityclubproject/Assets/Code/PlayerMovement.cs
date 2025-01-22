using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f; // Default walking speed
    public float runSpeed = 7f;  // Speed when running
    public float slowSpeed = 1.5f; // Speed when walking slower
    public float acceleration = 10f; // Smoothing acceleration

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private float currentSpeed;
    private float targetSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed; // Start with default walking speed
    }

    void Update()
    {
        // Get input from player
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized; // Normalize to prevent faster diagonal movement

        // Adjust target speed based on input
        if (Input.GetKey(KeyCode.LeftShift)) // Running
        {
            targetSpeed = runSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl)) // Walking slower
        {
            targetSpeed = slowSpeed;
        }
        else // Default walking
        {
            targetSpeed = walkSpeed;
        }
    }

    void FixedUpdate()
    {
        // Smoothly transition to the target speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // Apply movement with the smoothed speed
        rb.linearVelocity = movementInput * currentSpeed;
    }
}
