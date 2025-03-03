using UnityEngine;
using UnityEngine.AI;

public class TestNavMeshMove : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform targetPoint;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(targetPoint.position);
    }
}
