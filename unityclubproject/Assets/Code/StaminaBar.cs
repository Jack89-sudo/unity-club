using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float staminaDrainRate = 20f;

    public float currentStamina;

    [Header("UI Elements")]
    public Image staminaFill; // Assign this in the Inspector

    void Start()
    {
        currentStamina = maxStamina; // Start full
        UpdateStaminaUI();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift)) // Running drains stamina
        {
            ChangeStamina(-staminaDrainRate * Time.deltaTime);
        }
        else
        {
            ChangeStamina(staminaRegenRate * Time.deltaTime);
        }
    }

    public void ChangeStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        UpdateStaminaUI();
    }

    void UpdateStaminaUI()
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = currentStamina / maxStamina; // Ensure correct fill range (0 to 1)
        }
        else
        {
            Debug.LogError("StaminaFill UI Image is not assigned!");
        }
    }
}
