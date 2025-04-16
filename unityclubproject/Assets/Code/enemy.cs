using UnityEngine;
using UnityEngine.AI;
 // For Light2D
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private SpriteRenderer wanderAreaSprite;
    [SerializeField] private float wanderDelay = 2f;

    [Header("Detection")]
    [SerializeField] public float detectionRadius = 3f;         // Primary detection radius (walking detection)
    [SerializeField] private float secondaryDetectionRadius = 10f; // Secondary detection radius (for running)
    [SerializeField] private float chaseDuration = 5f;

    [Header("Vision Settings")]
    [SerializeField] private float visionDistance = 10f; // Maximum vision distance in front of the enemy
    [SerializeField] private float visionAngle = 45f;    // Field of view (FOV) in degrees
    [SerializeField] private LayerMask obstructionMask;  // Layer mask for walls/obstacles

    [Header("Light Settings")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D exitRoomLight; // Attach a Light2D to this field in the inspector

    [Header("Kill Settings")]
    [SerializeField] private float killRadius = 1f; // If the player gets within this distance, the game ends

    [Header("References")]
    [SerializeField] private Transform target; // Typically, the player's transform.
    private NavMeshAgent agent;
    private Bounds wanderBounds;
    private Coroutine chaseCoroutine;
    private Coroutine wanderCoroutine;

    // Two colliders for independent detection areas.
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

    // Flag for a persistent (locked-in) chase (triggered via walking or vision).
    private bool isPersistentChase = false;

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
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();

        // --- Vision Check ---
        // If the enemy sees the player (within its vision cone and unobstructed), begin a persistent chase.
        if (CheckVision())
        {
            StartChasing(true);
        }

        // For nonpersistent chases, if the player moves slow, cancel chasing.
        if (playerMovement != null && playerMovement.currentMoveState == PlayerMovement.MoveState.Slow && !isPersistentChase)
        {
            if (currentState == State.Chasing)
            {
                currentState = State.Wandering;
                isSecondaryChase = false;
                if (chaseCoroutine != null)
                {
                    StopCoroutine(chaseCoroutine);
                    chaseCoroutine = null;
                }
                SetNewWanderDestination();
            }
            return;
        }

        // --- Chase Behavior ---
        if (currentState == State.Chasing)
        {
            if (isPersistentChase)
            {
                // Persistent chase: always follow the player.
                agent.SetDestination(target.position);
            }
            else
            {
                float playerDist = Vector3.Distance(transform.position, target.position);
                if (playerDist <= detectionRadius)
                {
                    isSecondaryChase = false;
                    agent.SetDestination(target.position);
                }
                else if (playerDist <= secondaryDetectionRadius && playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
                {
                    secondaryChaseTarget = target.position;
                    agent.SetDestination(secondaryChaseTarget);
                }
            }
        }
        else if (currentState == State.Wandering && !agent.pathPending && agent.remainingDistance < 0.1f && wanderCoroutine == null)
        {
            wanderCoroutine = StartCoroutine(WanderDelayRoutine());
        }

        RotateTowardsMovementDirection();

        // --- Light Activation ---
        // If the enemy has left its original room (wanderArea bounds), enable the attached light.
        if (wanderAreaSprite != null && exitRoomLight != null)
        {
            if (wanderBounds.Contains(transform.position))
                exitRoomLight.enabled = false;
            else
                exitRoomLight.enabled = true;
        }

        // --- Kill Radius Check ---
        // If the player gets too close to the enemy, trigger game over.
        if (target != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, target.position);
            if (distToPlayer <= killRadius)
            {
                GameOver();
            }
        }
    }

    // CheckVision casts a ray from the enemy toward the player and returns true if:
    // - The player is within visionDistance.
    // - The player is within the enemy's FOV (centered on transform.right).
    // - There is no obstruction (using obstructionMask) blocking the view.
    private bool CheckVision()
    {
        if (target == null)
            return false;

        Vector3 toPlayer = target.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer > visionDistance)
            return false;

        float angleToPlayer = Vector3.Angle(transform.right, toPlayer);
        if (angleToPlayer > visionAngle * 0.5f)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distanceToPlayer, obstructionMask);
        if (hit.collider != null)
        {
            // An obstruction is blocking the view.
            return false;
        }
        return true;
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

        // If already in a persistent chase (via vision or walking), ignore new triggers.
        if (isPersistentChase)
            return;

        // For nonpersistent detection, ignore slow movement.
        if (playerMovement != null && playerMovement.currentMoveState == PlayerMovement.MoveState.Slow)
            return;

        float distance = Vector3.Distance(transform.position, other.transform.position);
        if (distance <= detectionRadius)
        {
            if (playerMovement.currentMoveState == PlayerMovement.MoveState.Walking)
                StartChasing(true);
            else if (playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
                StartChasing(false);
        }
        else if (distance <= secondaryDetectionRadius && playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
        {
            StartChasing(false);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isPersistentChase)
            return;

        if (playerMovement != null && playerMovement.currentMoveState == PlayerMovement.MoveState.Slow)
            return;

        float distance = Vector3.Distance(transform.position, other.transform.position);
        if (distance <= detectionRadius)
        {
            if (playerMovement.currentMoveState == PlayerMovement.MoveState.Walking)
                StartChasing(true);
            else if (playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
                StartChasing(false);
        }
        else if (distance <= secondaryDetectionRadius && playerMovement.currentMoveState == PlayerMovement.MoveState.Running)
        {
            StartChasing(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // For persistent chase (walking/vision), do not cancel the chase upon exit.
        // For nonpersistent (running) chase, the chase timer will cancel the chase.
    }

    // StartChasing starts a chase that lasts exactly chaseDuration seconds.
    // If 'persistent' is true (set via walking or vision), the chase is locked-in.
    private void StartChasing(bool persistent)
    {
        if (currentState == State.Chasing && isPersistentChase && persistent)
            return;

        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
        currentState = State.Chasing;
        isPersistentChase = persistent;
        chaseCoroutine = StartCoroutine(ChaseTimer());
    }

    private IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(chaseDuration);
        currentState = State.Wandering;
        isSecondaryChase = false;
        isPersistentChase = false;
        SetNewWanderDestination();
    }

    // GameOver is called when the player enters the kill radius.
    // Replace or expand this method with your own end-game logic.
    private void GameOver()
    {
        Debug.Log("Game Over: Player has been killed.");
        // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw wander area.
        if (wanderAreaSprite != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(wanderAreaSprite.bounds.center, wanderAreaSprite.bounds.size);
        }
        // Primary detection radius.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        // Secondary detection radius.
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, secondaryDetectionRadius);

        // Vision cone.
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.right;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, visionAngle * 0.5f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -visionAngle * 0.5f) * forward;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * visionDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * visionDistance);

        // Kill radius.
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, killRadius);

        // Patrol points.
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in patrolPoints)
                Gizmos.DrawSphere(point.position, 0.2f);
        }
    }
}
