using UnityEngine;

public class DoorController2D : MonoBehaviour
{
    public HingeJoint2D hingeJoint;  // hingecomponent on door
    public Collider2D triggerZone;   // player trigger zone colider
    public float openAngle = 90f;    // angle of open rotation 
    public float closedAngle = 0f;   // close rotation
    public float motorSpeed = 200f;  // speed at opening
    public float collisionSlowFactor = 0.3f;

    private bool isOpen = false;      // Door state
    private bool playerInRange = false; // Tracks if the player is inside the trigger

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
        if (collision.collider.CompareTag("Player"))
        {
            AdjustMotorSpeed(collisionSlowFactor);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
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
