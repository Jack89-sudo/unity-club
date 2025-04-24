using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private SpriteRenderer wanderAreaSprite;
    [SerializeField] private float wanderDelay = 2f;

    [Header("Detection")]
    [SerializeField] public float detectionRadius = 3f;
    [SerializeField] private float secondaryDetectionRadius = 10f;
    [SerializeField] private float chaseDuration = 5f;

    [Header("Vision Settings")]
    [SerializeField] private float visionDistance = 10f;
    [SerializeField] private float visionAngle = 45f;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Light Settings")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D exitRoomLight;

    [Header("Kill Settings")]
    [SerializeField] private float killRadius = 1f;

    [Header("References")]
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    private Bounds wanderBounds;
    private Coroutine chaseCoroutine;
    private Coroutine wanderCoroutine;

    private CircleCollider2D primaryCollider;
    private CircleCollider2D secondaryCollider;

    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float patrolInterval = 10f;
    [SerializeField] private float patrolWaitTime = 3f;

    private Vector3 lastWanderPosition;
    private Coroutine patrolCoroutine;
    private Vector3 originalPosition;
    
    private bool isSecondaryChase = false;
    private Vector3 secondaryChaseTarget;
    private bool isPersistentChase = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        if (wanderAreaSprite != null)
        {
            wanderBounds = wanderAreaSprite.bounds;
            originalPosition = wanderBounds.center;
        }
        else
        {
            Debug.LogError("Wander Area Sprite not assigned!");
        }

        primaryCollider = gameObject.AddComponent<CircleCollider2D>();
        primaryCollider.isTrigger = true;
        primaryCollider.radius = detectionRadius;

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

            Transform patrolPoint = patrolPoints[Random.Range(0, patrolPoints.Count)];
            lastWanderPosition = transform.position;
            agent.SetDestination(patrolPoint.position);

            while (Vector3.Distance(transform.position, patrolPoint.position) > 0.5f)
                yield return null;

            yield return new WaitForSeconds(patrolWaitTime);
            SetNewWanderDestination();
        }
    }

    private void Update()
    {
        // Vision-based chasing
        if (CheckVision())
        {
            StartChasing(true);
        }

        // Handle hidden player
        if (currentState == State.Chasing && PlayerHiding.Instance.IsHidden)
        {
            StopChasing();
            StartCoroutine(WanderAndReturn());
        }

        // Chase cancellation logic
        if (currentState == State.Chasing && !isPersistentChase)
        {
            float playerDist = Vector3.Distance(transform.position, target.position);
            if (playerDist > secondaryDetectionRadius)
            {
                StopChasing();
            }
        }

        // Chase behavior
        if (currentState == State.Chasing)
        {
            if (isPersistentChase)
            {
                agent.SetDestination(target.position);
            }
            else
            {
                agent.SetDestination(secondaryChaseTarget);
            }
        }
        else if (currentState == State.Wandering && !agent.pathPending && agent.remainingDistance < 0.1f && wanderCoroutine == null)
        {
            wanderCoroutine = StartCoroutine(WanderDelayRoutine());
        }

        RotateTowardsMovementDirection();

        // Light activation
        if (wanderAreaSprite != null && exitRoomLight != null)
        {
            exitRoomLight.enabled = !wanderBounds.Contains(transform.position);
        }

        // Kill radius check
        if (target != null && Vector3.Distance(transform.position, target.position) <= killRadius)
        {
            GameOver();
        }
    }

    private bool CheckVision()
    {
        if (target == null || PlayerHiding.Instance.IsHidden) return false;

        Vector3 toPlayer = target.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer > visionDistance) return false;

        float angleToPlayer = Vector3.Angle(transform.right, toPlayer);
        if (angleToPlayer > visionAngle * 0.5f) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, 
                                           distanceToPlayer, obstructionMask);
        return hit.collider == null;
    }

    private IEnumerator WanderAndReturn()
    {
        // Wander for 2 seconds
        float wanderTime = 2f;
        float timer = 0f;

        while (timer < wanderTime)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                SetNewWanderDestination();
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Return to original position
        agent.SetDestination(originalPosition);
        while (Vector3.Distance(transform.position, originalPosition) > 0.5f)
        {
            yield return null;
        }

        // Resume normal behavior
        SetNewWanderDestination();
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

        Vector3 newTarget = new Vector3(
            Random.Range(wanderBounds.min.x, wanderBounds.max.x),
            Random.Range(wanderBounds.min.y, wanderBounds.max.y),
            transform.position.z
        );
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
        if (!other.CompareTag("Player") || isPersistentChase || PlayerHiding.Instance.IsHidden) return;

        float distance = Vector3.Distance(transform.position, other.transform.position);
        if (distance <= detectionRadius)
        {
            StartChasing(true);
        }
        else if (distance <= secondaryDetectionRadius)
        {
            secondaryChaseTarget = other.transform.position;
            StartChasing(false);
        }
    }

    private void StartChasing(bool persistent)
    {
        if (currentState == State.Chasing && isPersistentChase && persistent) return;

        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
        
        currentState = State.Chasing;
        isPersistentChase = persistent;
        chaseCoroutine = StartCoroutine(ChaseTimer());
    }

    private void StopChasing()
    {
        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
        currentState = State.Wandering;
        isSecondaryChase = false;
        isPersistentChase = false;
    }

    private IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(chaseDuration);
        StopChasing();
    }

    private void GameOver()
    {
        Debug.Log("Game Over: Player has been killed.");
        SceneManager.LoadScene("Ending Screen");
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
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, secondaryDetectionRadius);

        Vector3 forward = transform.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + 
            Quaternion.Euler(0, 0, visionAngle * 0.5f) * forward * visionDistance);
        Gizmos.DrawLine(transform.position, transform.position + 
            Quaternion.Euler(0, 0, -visionAngle * 0.5f) * forward * visionDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, killRadius);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in patrolPoints)
                Gizmos.DrawSphere(point.position, 0.2f);
        }
    }
}