using UnityEngine;
using TMPro; // ✅ IMPORTANT!
using UnityEngine.EventSystems;

public class MathMinigame : MonoBehaviour
{
    public TMP_Text questionText; // ✅ Use TMP_Text instead of Text
    public TMP_InputField answerInput; // ✅ Use TMP_InputField

    private int correctAnswer;
    private Minigame trigger;

    private void OnEnable()
    {
        trigger = FindFirstObjectByType<Minigame>();
        GenerateProblem();
        answerInput.text = "";
        EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            trigger.EndGame();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            CheckAnswer();
        }
    }

    void GenerateProblem()
    {
        int a = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        correctAnswer = a + b;
        questionText.text = $"What is {a} + {b}?";
    }

    void CheckAnswer()
    {
        int playerAnswer;
        if (int.TryParse(answerInput.text, out playerAnswer))
        {
            if (playerAnswer == correctAnswer)
            {
                Debug.Log("Correct!");
                trigger.EndGame();
            }
            else
            {
                Debug.Log("Incorrect! Try again.");
                answerInput.text = "";
            }
        }
        else
        {
            Debug.Log("Invalid input.");
            answerInput.text = "";
        }
    }
}
