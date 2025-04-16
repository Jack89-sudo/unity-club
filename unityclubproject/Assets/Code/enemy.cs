using UnityEngine.AI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For using lists

public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private SpriteRenderer wanderAreaSprite;
    [SerializeField] private float wanderDelay = 2f;

    [Header("Detection")]
    [SerializeField] public float detectionRadius = 3f;         // Primary detection radius
    [SerializeField] private float secondaryDetectionRadius = 10f; // Secondary detection radius (for running)
    [SerializeField] private float chaseDuration = 5f;

    [Header("References")]
    [SerializeField] private Transform target; // Typically, the player's transform.
    private NavMeshAgent agent;
    private Bounds wanderBounds;
    private Coroutine chaseCoroutine;
    private Coroutine wanderCoroutine;

    // We set up two colliders for independent detection areas:
    private CircleCollider2D primaryCollider;
    private CircleCollider2D secondaryCollider;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement playerMovement; // Reference to the PlayerMovement script

    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints; // List of patrol points
    [SerializeField] private float patrolInterval = 10f;
    [SerializeField] private float patrolWaitTime = 3f;

    private Vector3 lastWanderPosition;
    private Coroutine patrolCoroutine;

    // For secondary detection chase:
    private bool isSecondaryChase = false;
    private Vector3 secondaryChaseTarget;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        if (wanderAreaSprite != null)
            wanderBounds = wanderAreaSprite.bounds;
        else
            Debug.LogError("Wander Area Sprite not assigned!");

        // Setup the primary detection collider.
        primaryCollider = gameObject.AddComponent<CircleCollider2D>();
        primaryCollider.isTrigger = true;
        primaryCollider.radius = detectionRadius;

        // Setup the secondary detection collider.
        secondaryCollider = gameObject.AddComponent<CircleCollider2D>();
        secondaryCollider.isTrigger = true;
        secondaryCollider.radius = secondaryDetectionRadius;

        SetNewWanderDestination();

        if (patrolPoints.Count > 0)
            patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(patrolInterval);

            if (currentState != State.Wandering || patrolPoints.Count == 0)
                continue;

            // Choose a random patrol point.
            Transform patrolPoint = patrolPoints[Random.Range(0, patrolPoints.Count)];
            lastWanderPosition = transform.position;
            agent.SetDestination(patrolPoint.position);

            // Wait until reaching the patrol point.
            while (Vector3.Distance(transform.position, patrolPoint.position) > 0.5f)
                yield return null;

            yield return new WaitForSeconds(patrolWaitTime);
            SetNewWanderDestination();
        }
    }

    private void Update()
    {
        // Always check that playerMovement is assigned.
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();

        if (currentState == State.Chasing)
        {
            float playerDist = Vector3.Distance(transform.position, target.position);
            // If the player is in the primary detection radius, always chase the current position.
            if (playerDist <= detectionRadius)
            {
                isSecondaryChase = false;
                agent.SetDestination(target.position);
            }
            // Otherwise, if we're in secondary chase mode, go to the locked (last known) position.
            else if (isSecondaryChase)
            {
                agent.SetDestination(secondaryChaseTarget);
            }
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
        if (wanderAreaSprite == null || currentState == State.Chasing)
            return;

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
        if (!other.CompareTag("Player"))
            return;

        float distance = Vector3.Distance(transform.position, other.transform.position);

        // Primary detection: if the player is inside the primary radius, chase continuously.
        if (distance <= detectionRadius)
        {
            isSecondaryChase = false;
            StartChasing();
        }
        // Secondary detection: if the player is outside the primary but within the secondary radius
        // and the player is running, lock onto the player's position.
        else if (distance <= secondaryDetectionRadius && playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
        {
            isSecondaryChase = true;
            secondaryChaseTarget = other.transform.position;
            StartChasing();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        float distance = Vector3.Distance(transform.position, other.transform.position);

        // If the player is within the primary radius at any point, always track their current position.
        if (distance <= detectionRadius)
        {
            isSecondaryChase = false;
            StartChasing();
        }
        // If the player is within the secondary radius and running, lock on if not already in secondary mode.
        else if (distance <= secondaryDetectionRadius && playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
        {
            if (!isSecondaryChase)
            {
                isSecondaryChase = true;
                secondaryChaseTarget = other.transform.position;
                StartChasing();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Optionally, you can implement behavior when the player exits the detection zones.
    }

    private void StartChasing()
    {
        // Restart the chase timer every time chase is initiated.
        if (chaseCoroutine != null)
            StopCoroutine(chaseCoroutine);

        currentState = State.Chasing;
        chaseCoroutine = StartCoroutine(ChaseTimer());
    }

    private IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(chaseDuration);
        currentState = State.Wandering;
        isSecondaryChase = false;
        SetNewWanderDestination();
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the wander area.
        if (wanderAreaSprite != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(wanderAreaSprite.bounds.center, wanderAreaSprite.bounds.size);
        }

        // Primary detection radius in red.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Secondary detection radius in an orange tone.
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, secondaryDetectionRadius);

        // Optional: Visualize patrol points.
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in patrolPoints)
                Gizmos.DrawSphere(point.position, 0.2f);
        }
    }
}
