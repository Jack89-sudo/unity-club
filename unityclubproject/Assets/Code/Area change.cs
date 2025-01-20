using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera; // Reference to the main camera
    public float zoomOutSize = 10f; // The size to zoom out to
    public float zoomSpeed = 2f; // Speed of the zoom

    private float originalSize; // Original camera size

    void Start()
    {
        // Ensure mainCamera is assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Store the original size of the camera
        originalSize = mainCamera.orthographicSize;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player enters the trigger
        if (other.CompareTag("Player"))
        {
            // Start zooming out
            StartCoroutine(ZoomCamera(zoomOutSize));
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if the player exits the trigger
        if (other.CompareTag("Player"))
        {
            // Return to the original zoom level
            StartCoroutine(ZoomCamera(originalSize));
        }
    }

    private System.Collections.IEnumerator ZoomCamera(float targetSize)
    {
        // Smoothly interpolate to the target size
        while (Mathf.Abs(mainCamera.orthographicSize - targetSize) > 0.01f)
        {
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // Ensure exact target size is set
        mainCamera.orthographicSize = targetSize;
    }
}
