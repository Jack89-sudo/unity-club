using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private SpriteRenderer wanderAreaSprite; // Sprite defines wander area
    [SerializeField] private float wanderDelay = 2f;

    [Header("Detection")]
    [SerializeField] private Transform detectionSprite; // Sprite for detection area
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float chaseDuration = 5f; // How long the enemy chases before giving up

    [Header("References")]
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    private Bounds wanderBounds;
    private Coroutine chaseCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // We will manually rotate
        agent.updateUpAxis = false; // Keep movement 2D (no Z rotation)

        // Get wander bounds from the attached sprite
        if (wanderAreaSprite != null)
        {
            wanderBounds = wanderAreaSprite.bounds;
        }
        else
        {
            Debug.LogError("Wander Area Sprite not assigned!");
        }

        // Scale detection sprite for visualization
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
        else if (currentState == State.Wandering && !agent.pathPending && agent.remainingDistance < 0.1f)
        {
            Invoke(nameof(SetNewWanderDestination), wanderDelay);
        }

        RotateTowardsMovementDirection();
    }

    private void SetNewWanderDestination()
    {
        if (wanderAreaSprite == null || currentState == State.Chasing) return; // Stop wandering if chasing

        float randomX = Random.Range(wanderBounds.min.x, wanderBounds.max.x);
        float randomY = Random.Range(wanderBounds.min.y, wanderBounds.max.y);

        Vector3 newTarget = new Vector3(randomX, randomY, transform.position.z);
        agent.SetDestination(newTarget);
    }

    private void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.01f) // Ensure movement is happening
        {
            Vector3 moveDirection = agent.velocity.normalized;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (chaseCoroutine != null)
                StopCoroutine(chaseCoroutine);

            currentState = State.Chasing;
            chaseCoroutine = StartCoroutine(ChaseTimer());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Start the chase timer but don't immediately stop chasing
        }
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
    }
}
