using UnityEngine;
using UnityEngine.UI;

public class QTEGame : MonoBehaviour
{
    public Text promptText; // UI text to show button prompt
    private KeyCode targetKey;
    private Minigame trigger;

    private void OnEnable()
    {
        trigger = FindFirstObjectByType<Minigame>();
        GenerateKey();
    }

    private void Update()
    {
        // Check for QTE success
        if (Input.GetKeyDown(targetKey))
        {
            Debug.Log("QTE Success!");
            trigger.EndGame();
        }
        // Exit mini-game on Space or Escape
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Exited mini-game.");
            trigger.EndGame();
        }
        // Wrong key input
        else if (Input.anyKeyDown)
        {
            Debug.Log("Wrong key!");
            GenerateKey(); // Could be replaced with a strike/fail system
        }
    }

    void GenerateKey()
    {
        KeyCode[] keys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.W };
        targetKey = keys[Random.Range(0, keys.Length)];
        promptText.text = $"Press {targetKey}";
    }
}
