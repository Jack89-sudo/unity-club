using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class HomeworkTask : MonoBehaviour
{
    [Header("Hold‐to‐Complete Settings")]
    [Tooltip("Time in seconds the player must hold E to finish this homework.")]
    [SerializeField] private float holdDuration = 2f;

    [Header("UI")]
    [Tooltip("UI Slider showing hold progress. Should be hidden by default and placed on a Screen Space Canvas.")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("Offset (in world units) above the sprite to anchor the slider's screen position.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    private float holdTimer = 0f;
    private bool isPlayerInRange = false;

    // references
    private control taskController;
    private SpriteRenderer spriteRenderer;
    private Collider2D taskCollider;
    private Transform playerTransform;

    // for UI positioning
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private RectTransform sliderRect;
    private Camera uiCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        taskCollider = GetComponent<Collider2D>();
        taskCollider.isTrigger = true;

        // find control script
        taskController = FindObjectOfType<control>();
        if (taskController == null)
            Debug.LogError("HomeworkTask: no 'control' in scene.");

        // find player for range + rotation
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;
        else
            Debug.LogError("HomeworkTask: no GameObject tagged 'Player'.");

        // slider setup
        if (progressSlider == null)
        {
            Debug.LogError("HomeworkTask: assign Progress Slider.");
        }
        else
        {
            progressSlider.gameObject.SetActive(false);
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;

            // cache UI RectTransforms and canvas
            sliderRect = progressSlider.GetComponent<RectTransform>();
            parentCanvas = progressSlider.GetComponentInParent<Canvas>();
            canvasRect = parentCanvas.GetComponent<RectTransform>();
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                                ? null
                                : parentCanvas.worldCamera;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            isPlayerInRange = true;
            if (progressSlider != null)
                progressSlider.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            isPlayerInRange = false;
            ResetProgress();
            if (progressSlider != null)
                progressSlider.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlayerInRange || progressSlider == null || playerTransform == null)
            return;

        // constantly reposition slider on canvas
        UpdateSliderOnCanvas();

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            progressSlider.value = Mathf.Clamp01(holdTimer / holdDuration);

            if (holdTimer >= holdDuration)
                CompleteTask();
        }
        else if (holdTimer > 0f)
        {
            // cancelled mid‐hold
            ResetProgress();
        }
    }

    private void UpdateSliderOnCanvas()
    {
        // world position above sprite
        Vector3 worldPos = transform.position + worldOffset;
        // convert to screen point
        Vector2 screenPt = Camera.main.WorldToScreenPoint(worldPos);
        // convert to canvas local point
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPt, uiCamera, out Vector2 localPoint
        );
        // place slider
        sliderRect.anchoredPosition = localPoint;

        // optionally rotate to face player:
        Vector3 dir = (playerTransform.position - worldPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg+90;
        sliderRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ResetProgress()
    {
        holdTimer = 0f;
        progressSlider.value = 0f;
    }

    private void CompleteTask()
    {
        taskController?.CompleteHomework();

        // hide everything
        progressSlider.gameObject.SetActive(false);
        spriteRenderer.enabled = false;
        taskCollider.enabled = false;
        enabled = false;
    }
}
