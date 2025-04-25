using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent), typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    public enum State { Wandering, Chasing }
    private State currentState = State.Wandering;
    public State CurrentState => currentState;

    [Header("Appearance")]
    [Tooltip("The task number at which this enemy will appear.")]
    [SerializeField] private int appearTaskNumber = 1;

    [Header("Dialogue Settings")]
    [Tooltip("What the teacher says when they first appear.")]
    [SerializeField] private string appearMessage;
    [Tooltip("UI TextMeshPro for dialogue message. Should be on a Screen-Space Canvas.")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("UI Image for showing the teacher's portrait.")]
    [SerializeField] private Image dialoguePortrait;
    [Tooltip("Optional sprite for the dialogue portrait. If unset, uses the enemy's sprite.")]
    [SerializeField] private Sprite dialoguePortraitSprite;
    [Tooltip("UI Image for the speech bubble background.")]
    [SerializeField] private Image speechBubbleImage;
    [Tooltip("How long (seconds) the dialogue stays on-screen.")]
    [SerializeField] private float dialogueDuration = 3f;

    [Header("Audio Clips (no AudioSource needed)")]
    [Tooltip("Clip to play for the length of dialogueDuration.")]
    [SerializeField] private AudioClip timedSpeechClip;
    [Tooltip("Clip to play full-length.")]
    [SerializeField] private AudioClip fullAudioClip;
    [Range(0f, 10f)]
    [Tooltip("Volume for speech clip.")]
    [SerializeField] private float speechVolume = 1f;
    [Range(0.1f, 3f)]
    [Tooltip("Pitch (speed) for speech clip.")]
    [SerializeField] private float speechPitch = 1f;
    [Range(0f, 1f)]
    [Tooltip("Volume for full-length clip.")]
    [SerializeField] private float fullAudioVolume = 1f;

    [Header("Wander Area")]
    [Tooltip("Drag in a GameObject with a BoxCollider2D defining the bounds.")]
    [SerializeField] private BoxCollider2D wanderAreaCollider;

    [Header("Wander Settings")]
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
    private Color originalLightColor;

    [Header("Kill Settings")]
    [SerializeField] private float killRadius = 1f;

    [Header("Game Over Settings")]
    [SerializeField] private float gameOverHoldDuration = 2f;
    [SerializeField] private Slider gameOverSlider;
    [SerializeField] private Vector3 gameOverSliderOffset = new Vector3(0f, 1.5f, 0f);

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private float patrolInterval = 10f;
    [SerializeField] private float patrolWaitTime = 3f;

    // Internal state
    private control taskController;
    private PlayerMovement playerMovement;
    private NavMeshAgent agent;
    private SpriteRenderer enemySprite;
    private Bounds wanderBounds;
    private Vector3 originalPosition;
    private float gameOverTimer = 0f;
    private bool hasAppeared = false;
    private bool inKillRange = false;
    private bool isPersistentChase = false;

    // UI helpers
    private RectTransform canvasRect;
    private RectTransform sliderRect;
    private Camera uiCamera;
    private Transform playerTransform;

    // Audio
    private AudioSource speechAudioSource;
    private AudioSource fullAudioSource;

    // Coroutines
    private Coroutine chaseCoroutine;
    private Coroutine wanderCoroutine;
    private Coroutine patrolCoroutine;

    void Awake()
    {
        speechAudioSource = gameObject.AddComponent<AudioSource>();
        speechAudioSource.playOnAwake = false;
        fullAudioSource = gameObject.AddComponent<AudioSource>();
        fullAudioSource.playOnAwake = false;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemySprite = GetComponent<SpriteRenderer>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
        if (dialoguePortrait != null) dialoguePortrait.gameObject.SetActive(false);
        if (speechBubbleImage != null) speechBubbleImage.gameObject.SetActive(false);

        if (gameOverSlider != null)
        {
            gameOverSlider.gameObject.SetActive(false);
            gameOverSlider.minValue = 0f;
            gameOverSlider.maxValue = 1f;
            gameOverSlider.value = 0f;
            sliderRect = gameOverSlider.GetComponent<RectTransform>();
            Canvas parentCanvas = gameOverSlider.GetComponentInParent<Canvas>();
            canvasRect = parentCanvas.GetComponent<RectTransform>();
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }

        taskController = FindObjectOfType<control>();
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target != null)
        {
            playerMovement = target.GetComponent<PlayerMovement>();
            playerTransform = target;
        }

        if (wanderAreaCollider != null)
        {
            wanderBounds = wanderAreaCollider.bounds;
            originalPosition = wanderBounds.center;
        }
        else
        {
            originalPosition = transform.position;
            wanderBounds = new Bounds(transform.position, Vector3.one * 5f);
        }

        // Light initialization
        if (exitRoomLight != null)
        {
            originalLightColor = exitRoomLight.color;
            exitRoomLight.enabled = false;
        }

        hasAppeared = false;
        enemySprite.enabled = false;
        agent.enabled = false;

        if (taskController.currentTask >= appearTaskNumber)
            Appear();
    }

    void Update()
    {
        if (!hasAppeared)
        {
            if (taskController.currentTask >= appearTaskNumber)
                Appear();
            else
                return;
        }

        if (target == null) return;
        float dist = Vector3.Distance(transform.position, target.position);

        // Kill-range logic
        if (dist <= killRadius)
        {
            inKillRange = true;
            UpdateGameOverSlider();
            gameOverTimer += Time.deltaTime;
            if (gameOverTimer >= gameOverHoldDuration) GameOver();
        }
        else if (inKillRange)
        {
            inKillRange = false;
            gameOverTimer = 0f;
            if (gameOverSlider != null) gameOverSlider.gameObject.SetActive(false);
        }
        if (inKillRange) return;

        bool isRunning = playerMovement != null && playerMovement.currentMoveState == PlayerMovement.MoveState.Running;
        bool isCrouching = playerMovement != null && playerMovement.currentMoveState == PlayerMovement.MoveState.Slow;

        // Light color update
        if (exitRoomLight != null)
        {
            bool isOutsideWander = !wanderBounds.Contains(transform.position);
            
            if (currentState == State.Chasing)
            {
                exitRoomLight.enabled = true;
                exitRoomLight.color = Color.red;
            }
            else
            {
                exitRoomLight.enabled = isOutsideWander;
                exitRoomLight.color = originalLightColor;
            }
        }

        if (CheckVision()) StartChasing(true);
        else if (currentState == State.Wandering && dist <= secondaryDetectionRadius && isRunning) StartChasing(false);
        else if (currentState == State.Wandering && dist <= detectionRadius && !isCrouching) StartChasing(true);
        else if (currentState == State.Wandering && dist <= detectionRadius && isCrouching && CheckVision()) StartChasing(true);

        if (currentState == State.Chasing && PlayerHiding.Instance.IsHidden)
        {
            StopChasing();
            StartCoroutine(WanderAndReturn());
        }
        if (currentState == State.Chasing && !isPersistentChase && dist > secondaryDetectionRadius) StopChasing();

        if (currentState == State.Chasing) agent.SetDestination(target.position);
        else if (currentState == State.Wandering && !agent.pathPending && agent.remainingDistance < 0.1f && wanderCoroutine == null)
            wanderCoroutine = StartCoroutine(WanderDelayRoutine());

        RotateTowardsMovementDirection();
    }

    // === ALL ORIGINAL METHODS BELOW THIS LINE ===

    private void Appear()
    {
        hasAppeared = true;
        enemySprite.enabled = true;
        agent.enabled = true;
        if (exitRoomLight != null) exitRoomLight.enabled = true;
        SetNewWanderDestination();
        if (patrolPoints.Count > 0) patrolCoroutine = StartCoroutine(PatrolRoutine());
        StartCoroutine(ShowDialogue());
    }

    private IEnumerator ShowDialogue()
    {
        if (dialoguePortrait != null)
        {
            dialoguePortrait.sprite = dialoguePortraitSprite != null ? dialoguePortraitSprite : enemySprite.sprite;
            dialoguePortrait.gameObject.SetActive(true);
        }
        if (speechBubbleImage != null) speechBubbleImage.gameObject.SetActive(true);
        if (dialogueText != null)
        {
            dialogueText.text = appearMessage;
            dialogueText.gameObject.SetActive(true);
        }

        if (timedSpeechClip != null)
        {
            speechAudioSource.clip = timedSpeechClip;
            speechAudioSource.volume = speechVolume;
            speechAudioSource.pitch = speechPitch;
            speechAudioSource.Play();
            StartCoroutine(StopSpeechAfterDuration(dialogueDuration));
        }

        if (fullAudioClip != null)
        {
            fullAudioSource.clip = fullAudioClip;
            fullAudioSource.volume = fullAudioVolume;
            fullAudioSource.pitch = 1f;
            fullAudioSource.Play();
        }

        yield return new WaitForSeconds(dialogueDuration);

        if (dialoguePortrait != null) dialoguePortrait.gameObject.SetActive(false);
        if (speechBubbleImage != null) speechBubbleImage.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
    }

    private IEnumerator StopSpeechAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        speechAudioSource.Stop();
    }

    private void UpdateGameOverSlider()
    {
        if (gameOverSlider == null || playerTransform == null) return;
        if (!gameOverSlider.gameObject.activeSelf) gameOverSlider.gameObject.SetActive(true);
        gameOverSlider.value = Mathf.Clamp01(gameOverTimer / gameOverHoldDuration);
        Vector3 wp = transform.position + gameOverSliderOffset;
        Vector2 sp = Camera.main.WorldToScreenPoint(wp);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, uiCamera, out Vector2 lp);
        sliderRect.anchoredPosition = lp;
        Vector3 dir = (playerTransform.position - wp).normalized;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        sliderRect.localRotation = Quaternion.Euler(0f, 0f, ang);
    }

    private bool CheckVision()
    {
        if (PlayerHiding.Instance.IsHidden) return false;
        Vector3 toP = target.position - transform.position;
        float d = toP.magnitude;
        if (d > visionDistance) return false;
        if (Vector3.Angle(transform.right, toP) > visionAngle * 0.5f) return false;
        return Physics2D.Raycast(transform.position, toP.normalized, d, obstructionMask).collider == null;
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

    private IEnumerator WanderDelayRoutine()
    {
        yield return new WaitForSeconds(wanderDelay);
        SetNewWanderDestination();
        wanderCoroutine = null;
    }

    private IEnumerator WanderAndReturn()
    {
        float t = 0f;
        while (t < 2f)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
                SetNewWanderDestination();
            t += Time.deltaTime;
            yield return null;
        }
        agent.SetDestination(originalPosition);
        yield return new WaitUntil(() => Vector3.Distance(transform.position, originalPosition) <= 0.5f);
        SetNewWanderDestination();
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(patrolInterval);
            if (currentState != State.Wandering) continue;
            var pt = patrolPoints[Random.Range(0, patrolPoints.Count)];
            agent.SetDestination(pt.position);
            yield return new WaitUntil(() => Vector3.Distance(transform.position, pt.position) <= 0.5f);
            yield return new WaitForSeconds(patrolWaitTime);
            SetNewWanderDestination();
        }
    }

    private void SetNewWanderDestination()
    {
        if (!hasAppeared || currentState == State.Chasing) return;
        var min = wanderBounds.min;
        var max = wanderBounds.max;
        Vector3 rnd = new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
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

    private void GameOver()
    {
        Debug.Log("Game Over: Player has been killed.");
        SceneManager.LoadScene("EndingScreen");
    }

    private void OnDrawGizmosSelected()
    {
        if (wanderAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(wanderAreaCollider.bounds.center, wanderAreaCollider.bounds.size);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, secondaryDetectionRadius);
        var f = transform.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, 0, visionAngle * 0.5f) * f * visionDistance);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, 0, -visionAngle * 0.5f) * f * visionDistance);
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