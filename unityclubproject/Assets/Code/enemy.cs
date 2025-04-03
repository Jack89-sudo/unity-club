using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private SpriteRenderer wanderAreaSprite;
    [SerializeField] private float wanderDelay = 2f;

    [Header("Detection")]
    [SerializeField] private Transform detectionSprite;
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float secondaryDetectionRadius = 6f; // New secondary radius
    [SerializeField] private float chaseDuration = 5f;

    [Header("References")]
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    private Bounds wanderBounds;
    private Coroutine chaseCoroutine;
    private Coroutine wanderCoroutine;

    private CircleCollider2D primaryCollider;
    private CircleCollider2D secondaryCollider;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        if (wanderAreaSprite != null)
        {
            wanderBounds = wanderAreaSprite.bounds;
        }
        else
        {
            Debug.LogError("Wander Area Sprite not assigned!");
        }

        // Primary detection collider
        primaryCollider = gameObject.AddComponent<CircleCollider2D>();
        primaryCollider.isTrigger = true;
        primaryCollider.radius = detectionRadius;

        // Secondary detection collider
        secondaryCollider = gameObject.AddComponent<CircleCollider2D>();
        secondaryCollider.isTrigger = true;
        secondaryCollider.radius = secondaryDetectionRadius;

        if (detectionSprite != null)
        {
            detectionSprite.localScale = new Vector3(detectionRadius * 2, detectionRadius * 2, 1);
        }

        SetNewWanderDestination();
    }

    private void Update()
    {
        if (currentState == State.Chasing)
        {
            agent.SetDestination(target.position);
        }
        else if (currentState == State.Wandering && !agent.pathPending && agent.remainingDistance < 0.1f && wanderCoroutine == null)
        {
            wanderCoroutine = StartCoroutine(WanderDelayRoutine());
        }

        RotateTowardsMovementDirection();
    }

    private IEnumerator WanderDelayRoutine()
    {
        yield return new WaitForSeconds(wanderDelay);
        SetNewWanderDestination();
        wanderCoroutine = null;
    }

    private void SetNewWanderDestination()
    {
        if (wanderAreaSprite == null || currentState == State.Chasing) return;

        float randomX = Random.Range(wanderBounds.min.x, wanderBounds.max.x);
        float randomY = Random.Range(wanderBounds.min.y, wanderBounds.max.y);

        Vector3 newTarget = new Vector3(randomX, randomY, transform.position.z);
        agent.SetDestination(newTarget);
    }

    private void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 moveDirection = agent.velocity.normalized;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        float distance = Vector3.Distance(transform.position, other.transform.position);

        if (distance <= detectionRadius)
        {
            StartChasing();
        }
        else if (distance <= secondaryDetectionRadius)
        {
            Debug.Log("Player entered outer detection zone (secondary radius).");
            // Optional: Begin suspicious state or delay-based chasing here
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Optional: Handle logic for player leaving detection zones
        }
    }

    private void StartChasing()
    {
        if (chaseCoroutine != null)
            StopCoroutine(chaseCoroutine);

        currentState = State.Chasing;
        chaseCoroutine = StartCoroutine(ChaseTimer());
    }

    private IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(chaseDuration);
        currentState = State.Wandering;
        SetNewWanderDestination();
    }

    private void OnDrawGizmosSelected()
    {
        if (wanderAreaSprite != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(wanderAreaSprite.bounds.center, wanderAreaSprite.bounds.size);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange for outer radius
        Gizmos.DrawWireSphere(transform.position, secondaryDetectionRadius);
    }
}
