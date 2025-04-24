using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float secondaryDetectionRadius = 10f;
    [SerializeField] private float chaseDuration = 5f;

    [Header("Vision Settings")]
    [SerializeField] private float visionDistance = 10f;
    [SerializeField] private float visionAngle = 45f;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Light Settings")]
    [SerializeField] private Light2D exitRoomLight;

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

    private bool isPersistentChase = false;
    private Vector3 secondaryChaseTarget;

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

        // Set up detection colliders
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

            if (currentState != State.Wandering)
                continue;

            var point = patrolPoints[Random.Range(0, patrolPoints.Count)];
            agent.SetDestination(point.position);

            while (Vector3.Distance(transform.position, point.position) > 0.5f)
                yield return null;

            yield return new WaitForSeconds(patrolWaitTime);
            SetNewWanderDestination();
        }
    }

    private void Update()
    {
        // Vision-based chasing
        if (CheckVision())
            StartChasing(persistent: true);

        // Cancel chase if hidden
        if (currentState == State.Chasing && PlayerHiding.Instance.IsHidden)
        {
            StopChasing();
            StartCoroutine(WanderAndReturn());
        }

        // Automatic cancel if player leaves secondary range
        if (currentState == State.Chasing && !isPersistentChase)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > secondaryDetectionRadius)
                StopChasing();
        }

        // Apply chase or wander destinations
        if (currentState == State.Chasing)
        {
            agent.SetDestination(isPersistentChase ? target.position : secondaryChaseTarget);
        }
        else if (currentState == State.Wandering &&
                 !agent.pathPending &&
                 agent.remainingDistance < 0.1f &&
                 wanderCoroutine == null)
        {
            wanderCoroutine = StartCoroutine(WanderDelayRoutine());
        }

        RotateTowardsMovementDirection();

        // Toggle exit-room light
        if (wanderAreaSprite != null && exitRoomLight != null)
            exitRoomLight.enabled = !wanderBounds.Contains(transform.position);

        // Kill-check
        if (target != null &&
            Vector3.Distance(transform.position, target.position) <= killRadius)
        {
            GameOver();
        }
    }

    private bool CheckVision()
    {
        if (target == null || PlayerHiding.Instance.IsHidden)
            return false;

        Vector3 toPlayer = target.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > visionDistance) return false;

        float angle = Vector3.Angle(transform.right, toPlayer);
        if (angle > visionAngle * 0.5f) return false;

        var hit = Physics2D.Raycast(transform.position, toPlayer.normalized, dist, obstructionMask);
        return hit.collider == null;
    }

    private IEnumerator WanderAndReturn()
    {
        float wanderTime = 2f;
        float t = 0f;

        while (t < wanderTime)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
                SetNewWanderDestination();

            t += Time.deltaTime;
            yield return null;
        }

        agent.SetDestination(originalPosition);
        while (Vector3.Distance(transform.position, originalPosition) > 0.5f)
            yield return null;

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

        Vector3 rnd = new Vector3(
            Random.Range(wanderBounds.min.x, wanderBounds.max.x),
            Random.Range(wanderBounds.min.y, wanderBounds.max.y),
            transform.position.z
        );
        agent.SetDestination(rnd);
    }

    private void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            float ang = Mathf.Atan2(agent.velocity.y, agent.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, ang);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") ||
            isPersistentChase ||
            PlayerHiding.Instance.IsHidden)
            return;

        float dist = Vector3.Distance(transform.position, other.transform.position);
        if (dist <= detectionRadius)
        {
            StartChasing(persistent: true);
        }
        else if (dist <= secondaryDetectionRadius)
        {
            secondaryChaseTarget = other.transform.position;
            StartChasing(persistent: false);
        }
    }

    private void StartChasing(bool persistent)
    {
        if (currentState == State.Chasing && isPersistentChase && persistent) return;
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);

        currentState = State.Chasing;
        isPersistentChase = persistent;
        chaseCoroutine = StartCoroutine(ChaseTimer());
    }

    private void StopChasing()
    {
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
        currentState = State.Wandering;
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
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, 0, visionAngle * 0.5f) * forward * visionDistance);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, 0, -visionAngle * 0.5f) * forward * visionDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, killRadius);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var pt in patrolPoints)
                Gizmos.DrawSphere(pt.position, 0.2f);
        }
    }
}
