using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    public static PlayerHiding Instance; // Singleton pattern for global access

    [Header("Hiding Settings")]
    public float hideConeAngle = 45f;
    public float hideDistance = 2f;
    public LayerMask hideableLayer;
    public float unhideOffset = 0.5f;

    [Header("Child Objects")]
    public GameObject[] objectsToHide; // Assign flashlight and other children in Inspector

    private Vector3 preHidePosition;
    private bool isHidden;
    private SpriteRenderer[] playerRenderers;
    private PlayerMovement playerMovement;
    private Collider2D playerCollider;

    public bool IsHidden => isHidden; // Public read-only access

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerRenderers = GetComponentsInChildren<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isHidden)
            {
                Unhide();
            }
            else
            {
                Collider2D hideSpot = CheckForHideableInCone();
                if (hideSpot != null)
                {
                    Hide(hideSpot);
                }
            }
        }
    }

    Collider2D CheckForHideableInCone()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, hideDistance, hideableLayer);
        
        foreach (Collider2D col in colliders)
        {
            Vector2 directionToObject = (col.transform.position - transform.position).normalized;
            float angleToObject = Vector2.Angle(transform.right, directionToObject);

            if (angleToObject <= hideConeAngle / 2f)
            {
                return col;
            }
        }
        return null;
    }

    void Hide(Collider2D hideSpot)
    {
        isHidden = true;
        preHidePosition = transform.position;

        // Store player at hide spot's position
        transform.position = hideSpot.bounds.center;

        // Disable components
        TogglePlayerComponents(false);
        ToggleChildren(false);
    }

    void Unhide()
    {
        isHidden = false;
        
        // Return to original position with offset
        transform.position = preHidePosition + (Vector3)Random.insideUnitCircle.normalized * unhideOffset;

        // Enable components
        TogglePlayerComponents(true);
        ToggleChildren(true);
    }

    void TogglePlayerComponents(bool state)
    {
        foreach (SpriteRenderer renderer in playerRenderers)
        {
            renderer.enabled = state;
        }
        
        playerMovement.enabled = state;
        playerCollider.enabled = state;
    }

    void ToggleChildren(bool state)
    {
        foreach (GameObject child in objectsToHide)
        {
            child.SetActive(state);
        }
    }

    // Add Gizmos drawing from previous versions
}