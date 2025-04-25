using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(HingeJoint2D), typeof(Collider2D))]
public class DoorController2D : MonoBehaviour
{
    public enum State { Closed, Open }

    [Header("Lock & Task Settings")]
    [Tooltip("If true, door starts locked until task requirement is met.")]
    public bool locked = false;
    [Tooltip("The minimum 'currentTask' value required to unlock.")]
    public int requiredTaskNumber = 0;

    [Header("Appearance & UI")]
    [Tooltip("World-space Canvas TextMeshProUGUI for interaction prompts.")]
    public TextMeshProUGUI promptText;
    [Tooltip("Offset (in world units) above the door to position the prompt.")]
    public Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Door Mechanics")]
    public HingeJoint2D hingeJoint;
    public Collider2D triggerZone;
    public float openAngle = 90f;
    public float closedAngle = 0f;
    public float motorSpeed = 200f;
    public float collisionSlowFactor = 0.3f;

    [Header("Audio Settings")]
    [Tooltip("Sound played when door opens.")]
    public AudioClip openSound;
    [Tooltip("Sound played when door closes.")]
    public AudioClip closeSound;
    [Range(0f, 1f)]
    [Tooltip("Volume for door sounds.")]
    public float audioVolume = 1f;

    // Internal references
    private control taskController;
    private HingeJoint2D doorHinge;
    private bool isOpen = false;
    private bool playerInRange = false;
    private AudioSource audioSource;

    // UI positioning
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private RectTransform textRect;
    private Camera uiCamera;

    private void Awake()
    {
        // Hinge
        doorHinge = hingeJoint != null ? hingeJoint : GetComponent<HingeJoint2D>();
        if (triggerZone == null)
            Debug.LogError("DoorController2D: triggerZone not assigned.");

        // Task controller
        taskController = FindObjectOfType<control>();
        if (taskController == null)
            Debug.LogError("DoorController2D: could not find 'control' script.");

        // Prompt UI
        if (promptText == null)
            Debug.LogError("DoorController2D: promptText not assigned.");
        else
        {
            textRect = promptText.GetComponent<RectTransform>();
            parentCanvas = promptText.canvas;
            canvasRect = parentCanvas.GetComponent<RectTransform>();
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            promptText.gameObject.SetActive(false);
        }

        // AudioSource for door sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        bool hasTask = taskController != null && taskController.currentTask >= requiredTaskNumber;
        bool isUnlocked = !locked && hasTask;

        // Position & show prompt
        if (promptText != null)
        {
            Vector3 worldPos = transform.position + promptOffset;
            Vector2 screenPt = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPt, uiCamera, out Vector2 localPoint
            );
            textRect.anchoredPosition = localPoint;

            promptText.text = isUnlocked ? (isOpen ? "E" : "E") : $"<color=red>{requiredTaskNumber}</color>";
            if (!promptText.gameObject.activeSelf)
                promptText.gameObject.SetActive(true);
        }

        // Interaction
        if (isUnlocked && Input.GetKeyDown(KeyCode.E))
        {
            ToggleDoor();
            // update prompt
            if (promptText != null)
                promptText.text = isOpen ? "E" : "E";
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        var motor = doorHinge.motor;
        motor.motorSpeed = isOpen ? motorSpeed : -motorSpeed;
        doorHinge.motor = motor;
        doorHinge.useMotor = true;

        // Play open/close sound
        if (audioSource != null)
        {
            AudioClip clip = isOpen ? openSound : closeSound;
            if (clip != null)
                audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == triggerZone)
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == triggerZone)
        {
            playerInRange = false;
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
            AdjustMotorSpeed(collisionSlowFactor);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
            AdjustMotorSpeed(1f);
    }

    private void AdjustMotorSpeed(float factor)
    {
        var motor = doorHinge.motor;
        motor.motorSpeed *= factor;
        doorHinge.motor = motor;
    }
}