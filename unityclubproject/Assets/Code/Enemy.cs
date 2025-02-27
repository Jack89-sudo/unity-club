using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public Transform target;

    NavMeshAgent agent;
    private void Start()
    {
        agent= GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.updateUpAxis= false;
    }

    private void Update()
    {
        agent.SetDestination(target.position);
    }
}
