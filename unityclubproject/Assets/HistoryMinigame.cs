using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class HistoryQuestion
{
    public string question;
    public string[] answers;
    public int correctIndex;
}

public class HistoryMinigame : MonoBehaviour
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerTexts;

    [Header("Settings")]
    public float interactionRadius = 2f;
    public LayerMask interactableLayer;
    
    [SerializeField] private List<HistoryQuestion> questions = new List<HistoryQuestion>();
    
    private HistoryQuestion currentQuestion;
    private GameObject currentInteractable;
    private Transform playerTransform;

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        InitializeQuestions();
        ValidateComponents();
        minigamePanel.SetActive(false);
    }

    void ValidateComponents()
    {
        Debug.Assert(minigamePanel != null, "Assign minigame panel!");
        Debug.Assert(questionText != null, "Assign question text!");
        Debug.Assert(answerButtons.Length == 4, "Need 4 answer buttons!");
        Debug.Assert(answerTexts.Length == 4, "Need 4 answer text fields!");
    }

    void InitializeQuestions()
    {
        // Question 1
        questions.Add(new HistoryQuestion {
            question = "Who was the first president of the United States?",
            answers = new string[] {"George Washington", "Thomas Jefferson", "John Adams", "James Madison"},
            correctIndex = 0
        });

        // Question 2
        questions.Add(new HistoryQuestion {
            question = "In which year did World War II end?",
            answers = new string[] {"1945", "1939", "1918", "1950"},
            correctIndex = 0
        });

        // Question 3
        questions.Add(new HistoryQuestion {
            question = "Who wrote the Declaration of Independence?",
            answers = new string[] {"George Washington", "Thomas Jefferson", "Benjamin Franklin", "John Adams"},
            correctIndex = 1
        });

        // Question 4
        questions.Add(new HistoryQuestion {
            question = "What was the name of the ship that brought the Pilgrims to America?",
            answers = new string[] {"Santa Maria", "Mayflower", "Titanic", "HMS Victory"},
            correctIndex = 1
        });

        // Question 5
        questions.Add(new HistoryQuestion {
            question = "Which ancient civilization built the pyramids?",
            answers = new string[] {"Greeks", "Romans", "Egyptians", "Mayans"},
            correctIndex = 2
        });

        // Question 6
        questions.Add(new HistoryQuestion {
            question = "Who was the leader of the Soviet Union during WWII?",
            answers = new string[] {"Lenin", "Stalin", "Khrushchev", "Trotsky"},
            correctIndex = 1
        });

        // Question 7
        questions.Add(new HistoryQuestion {
            question = "Which country gifted the Statue of Liberty to the USA?",
            answers = new string[] {"France", "England", "Spain", "Italy"},
            correctIndex = 0
        });

        // Question 8
        questions.Add(new HistoryQuestion {
            question = "When did the Titanic sink?",
            answers = new string[] {"1905", "1912", "1920", "1898"},
            correctIndex = 1
        });

        // Question 9
        questions.Add(new HistoryQuestion {
            question = "Who invented the telephone?",
            answers = new string[] {"Thomas Edison", "Alexander Bell", "Nikola Tesla", "Samuel Morse"},
            correctIndex = 1
        });

        // Question 10
        questions.Add(new HistoryQuestion {
            question = "What was the first human spaceflight?",
            answers = new string[] {"Apollo 11", "Sputnik", "Vostok 1", "Mercury"},
            correctIndex = 2
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            CheckForInteractables();
        }
    }

    void CheckForInteractables()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, interactionRadius, interactableLayer);
        if (colliders.Length > 0)
        {
            currentInteractable = colliders[0].gameObject;
            StartMinigame();
        }
    }

    void StartMinigame()
    {
        if (questions.Count == 0) return;

        currentQuestion = questions[Random.Range(0, questions.Count)];
        questionText.text = currentQuestion.question;

        for (int i = 0; i < 4; i++)
        {
            answerTexts[i].text = currentQuestion.answers[i];
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }

        minigamePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CheckAnswer(int selectedIndex)
    {
        if (selectedIndex == currentQuestion.correctIndex)
        {
            if (currentInteractable != null)
            {
                currentInteractable.SetActive(false);
            }
        }
        CloseMinigame();
    }

    void CloseMinigame()
    {
        minigamePanel.SetActive(false);
        Time.timeScale = 1f;
        currentInteractable = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}