using UnityEngine;

public class Minigame : MonoBehaviour
{
    public Collider2D playerCollider;
    private Collider2D myCollider;

    public GameObject minigameUI;

    public GameObject objectToHide; // 👈 Add this!

    private void Start()
    {
        myCollider = GetComponent<Collider2D>();
        minigameUI.SetActive(false);
    }

    private void Update()
    {
        if (myCollider.IsTouching(playerCollider) && Input.GetKeyDown(KeyCode.E))
        {
            ActivateGame();
        }
    }

    void ActivateGame()
    {
        minigameUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void EndGame()
    {
        minigameUI.SetActive(false);
        Time.timeScale = 1f;

        if (objectToHide != null)
        {
            objectToHide.SetActive(false); // 👈 Hide it!
        }
    }
}

