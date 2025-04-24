using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HistoryMinigame : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_InputField answerInput;
    private control gameControl;
    private Minigame trigger;

    private int currentQuestionIndex;
    private string correctAnswer;

    private struct Question
    {
        public string question;
        public string[] options;
        public string correctOption;
    }

    private Question[] questions = new Question[]
    {
        // ... (keep your existing questions array the same) ...
    };

    private void OnEnable()
    {
        gameControl = FindFirstObjectByType<control>();
        trigger = FindFirstObjectByType<Minigame>();
        
        if (trigger == null)
        {
            Debug.LogError("Minigame component not found in scene!");
            return;
        }

        ShowRandomQuestion();
        answerInput.text = "";
        
        if (EventSystem.current != null && answerInput != null)
        {
            EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
        }
    }

    private void Update()
    {
        if (trigger == null) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            trigger.EndGame();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            CheckAnswer();
        }
    }

    void ShowRandomQuestion()
    {
        currentQuestionIndex = Random.Range(0, questions.Length);
        var q = questions[currentQuestionIndex];
        correctAnswer = q.correctOption.ToLower();
        questionText.text = q.question + "\n" + string.Join("\n", q.options);
    }

    void CheckAnswer()
    {
        if (string.IsNullOrEmpty(answerInput.text)) return;

        string playerInput = answerInput.text.Trim().ToLower();
        if (playerInput == correctAnswer)
        {
            Debug.Log("Correct!");
            if (gameControl != null && gameControl.currentTask >= 1 && gameControl.currentTask < 7)
            {
                gameControl.CompleteHomework();
            }
            
            if (trigger != null)
            {
                trigger.EndGame();
            }
            else
            {
                Debug.LogError("Minigame reference missing!");
            }
        }
        else
        {
            Debug.Log("Incorrect! Try again.");
            answerInput.text = "";
        }
    }
}