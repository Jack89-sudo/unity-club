using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("2D Settings")]
    [SerializeField] private KeyCode collectKey = KeyCode.E;
    [SerializeField] private bool destroyInsteadOfHide = true;
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("2D References")]
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private GameObject collectionEffect;
    [SerializeField] private GameObject collectPromptUI;
    [SerializeField] private AudioClip collectSound;

    private Collider2D itemCollider;
    private static int totalCollected;
    private bool canCollect;
    private bool wasCollected;
    private AudioSource audioSource;

    private void Start()
    {
        itemCollider = GetComponent<Collider2D>();
        
        // Collider setup
        if (itemCollider == null)
        {
            Debug.LogError("Collectible missing Collider2D!", this);
            enabled = false;
            return;
        }
        itemCollider.isTrigger = true;

        // Player reference setup
        if (playerCollider == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerCollider = player.GetComponent<Collider2D>();
        }

        // Physics setup
        if (!TryGetComponent<Rigidbody2D>(out _))
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        // Audio setup
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Initialize UI
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (canCollect && !wasCollected && Input.GetKeyDown(collectKey))
        {
            CollectItem();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == playerCollider && !wasCollected)
        {
            canCollect = true;
            ShowPrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == playerCollider)
        {
            canCollect = false;
            ShowPrompt(false);
        }
    }

    private void ShowPrompt(bool show)
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(show);
        }
    }

    private void CollectItem()
    {
        wasCollected = true;
        totalCollected++;
        ShowPrompt(false);

        // Play sound
        if (collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Spawn effect
        if (collectionEffect != null)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }

        // Disable components
        itemCollider.enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Cleanup object
        if (destroyInsteadOfHide)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log($"Total Collected: {totalCollected}/6");
    }

    private void OnDrawGizmos()
    {
        if (itemCollider == null) return;
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        if (itemCollider is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        }
        else if (itemCollider is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
    }

    public static int GetTotalCollected() => totalCollected;
    public static void ResetCounter() => totalCollected = 0;
}