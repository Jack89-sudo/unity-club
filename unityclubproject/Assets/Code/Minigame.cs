using UnityEngine;

public class Minigame : MonoBehaviour
{
        public Collider2D otherCollider; // Assign in Inspector

        public Collider2D myCollider;
        private bool game = false;

        void Start()
        {
            myCollider = GetComponent<Collider2D>();
        }

        void Update()
        {
            if (myCollider.IsTouching(otherCollider) && Input.GetKeyDown(KeyCode.E))
            {
            

            }
        }
        
        void activategame()
         {
        game = true;
           }
    }

