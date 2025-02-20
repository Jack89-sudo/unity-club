using UnityEngine;

public class DoorController2D : MonoBehaviour
{
    public HingeJoint2D hingeJoint;  // Reference to the HingeJoint2D component
    public Collider2D triggerZone;   // The collider that allows interaction
    public float openAngle = 90f;    // Open angle (degrees)
    public float closedAngle = 0f;   // Closed angle (degrees)
    public float motorSpeed = 200f;  // Default motor speed
    public float collisionSlowFactor = 0.3f; // How much the door slows when colliding

    private bool isOpen = false;      // Door state
    private bool playerInRange = false; // Tracks if the player is inside the trigger
    private bool isColliding = false;  // Checks if door is hitting an object

    private void Start()
    {
        if (hingeJoint == null)
            hingeJoint = GetComponent<HingeJoint2D>();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleDoor();
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        JointMotor2D motor = hingeJoint.motor;
        motor.motorSpeed = isOpen ? motorSpeed : -motorSpeed;
        hingeJoint.motor = motor;
        hingeJoint.useMotor = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player")) // Any collidable object tagged "Obstacle"
        {
            isColliding = true;
            AdjustMotorSpeed(collisionSlowFactor);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isColliding = false;
            AdjustMotorSpeed(1f); // Restore full speed
        }
    }

    private void AdjustMotorSpeed(float factor)
    {
        JointMotor2D motor = hingeJoint.motor;
        motor.motorSpeed *= factor; // Reduce speed
        hingeJoint.motor = motor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == triggerZone)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == triggerZone)
        {
            playerInRange = false;
        }
    }
}
