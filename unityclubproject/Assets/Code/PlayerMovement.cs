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
    public Camera playerCamera;
    public float defaultZoom = 5f;
    public float runZoom = 7f;
    public float slowZoom = 3.5f;
    public float zoomSpeed = 5f;
    public float cameraMoveSpeed = 3f;

    [Header("Room Trigger Settings")]
    public Transform roomTriggerObject;

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

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        if (movementInput.sqrMagnitude > 1)
        {
            movementInput = movementInput.normalized;
        }

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

        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = movementInput * currentSpeed;

        if (playerCamera != null)
        {
            Vector3 targetPosition = inRoom
                ? new Vector3(roomCenter.x, roomCenter.y, playerCamera.transform.position.z)
                : new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z);

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (roomTriggerObject != null && other.transform == roomTriggerObject)
        {
            Transform parent = other.transform.parent;
            if (parent != null && parent.CompareTag("roomtriggerbox"))
            {
                inRoom = true;
                roomCenter = parent.GetComponent<Collider2D>().bounds.center;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (roomTriggerObject != null && other.transform == roomTriggerObject)
        {
            inRoom = false;
        }
    }
}