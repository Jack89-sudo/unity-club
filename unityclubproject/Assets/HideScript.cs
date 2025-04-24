using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class PlayerHiding : MonoBehaviour
{
    public static PlayerHiding Instance;

    [Header("Hiding Settings")]
    [SerializeField] private float hideDistance = 2f;
    [SerializeField] private float hideAngle = 45f;
    [SerializeField] private float unhideOffset = 1f;

    [Header("Object Lists")]
    [SerializeField] private List<Transform> hidingTriggers = new List<Transform>();
    [SerializeField] private List<GameObject> objectsToHide = new List<GameObject>();

    [Header("Components")]
    [SerializeField] private Light2D playerFlashlight;
    [SerializeField] private GameObject hidePrompt;
    
    private Vector3 preHidePosition;
    private bool isHidden;
    private Transform currentHideSpot;
    private Rigidbody2D rb;
    private SpriteRenderer[] renderers;
    private Dictionary<SpriteRenderer, bool> rendererStates = new Dictionary<SpriteRenderer, bool>();

    public bool IsHidden => isHidden;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        
        // Store initial renderer states
        foreach (SpriteRenderer renderer in renderers)
        {
            rendererStates[renderer] = renderer.enabled;
        }
    }

    void Update()
    {
        if (!isHidden)
        {
            CheckForNearbyHidingSpots();
            if (Input.GetKeyDown(KeyCode.E)) AttemptHide();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.E)) Unhide();
        }
    }

    void CheckForNearbyHidingSpots()
    {
        currentHideSpot = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform trigger in hidingTriggers)
        {
            if (trigger == null) continue;
            if (!trigger.gameObject.activeInHierarchy) continue;

            Vector2 directionToTrigger = trigger.position - transform.position;
            float distance = directionToTrigger.magnitude;
            float angle = Vector2.Angle(transform.right, directionToTrigger);

            if (distance <= hideDistance && angle <= hideAngle/2)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentHideSpot = trigger;
                }
            }
        }

        if (hidePrompt) hidePrompt.SetActive(currentHideSpot != null);
    }

    void AttemptHide()
    {
        if (currentHideSpot != null)
        {
            Hide(currentHideSpot);
            Debug.Log("Hiding at: " + currentHideSpot.name);
        }
    }

    void Hide(Transform hideSpot)
    {
        isHidden = true;
        preHidePosition = transform.position;
        
        // Disable physics
        rb.simulated = false;
        transform.position = hideSpot.position;

        // Hide all renderers
        foreach (SpriteRenderer renderer in renderers)
            renderer.enabled = false;

        // Disable objects
        foreach (GameObject obj in objectsToHide)
            obj.SetActive(false);

        // Disable flashlight
        if (playerFlashlight != null)
            playerFlashlight.enabled = false;
    }

    void Unhide()
    {
        isHidden = false;
        
        // Restore physics
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        transform.position = preHidePosition + (Vector3)Random.insideUnitCircle.normalized * unhideOffset;

        // Restore original renderer states
        foreach (SpriteRenderer renderer in renderers)
        {
            if (rendererStates.TryGetValue(renderer, out bool originalState))
            {
                renderer.enabled = originalState;
            }
        }

        // Enable objects
        foreach (GameObject obj in objectsToHide)
            obj.SetActive(true);

        // Enable flashlight
        if (playerFlashlight != null)
            playerFlashlight.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hideDistance);
        
        Vector2 rightDirection = transform.right * hideDistance;
        Vector2 angleA = Quaternion.Euler(0, 0, hideAngle/2) * rightDirection;
        Vector2 angleB = Quaternion.Euler(0, 0, -hideAngle/2) * rightDirection;
        
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + angleA);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + angleB);
    }
}