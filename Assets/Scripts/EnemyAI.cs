using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private int currentPoint = 0;
    private bool isReturningToPatrol = false;

    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float attackRange = 3f;

    [Header("References")]
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;

    [Header("Enemy Vision Settings")]
    public float fieldOfViewAngle = 90f;
    public float visionDistance = 15f;
    public LayerMask obstacleMask;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        animator.applyRootMotion = false;
        agent.speed = 2f;

        if (patrolPoints.Length > 0)
        {
            StartCoroutine(StartPatrolProperly());  // 🔹 Ensure the first patrol move is correct
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // 🔹 Ensure animations are always synced with movement
        if (agent.velocity.magnitude > 0.1f)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        if (distance <= attackRange && CanSeePlayer())
        {
            AttackPlayer();
        }
        else if (distance <= detectionRange && CanSeePlayer())
        {
            ChasePlayer();
        }
        else
        {
            if (!isReturningToPatrol)
            {
                StartCoroutine(ReturnToPatrol());
            }
        }
    }



    IEnumerator StartPatrolProperly()
    {
        yield return new WaitForSeconds(0.5f);  // 🔹 Give Unity time to initialize everything

        agent.isStopped = false;  // 🔹 Ensure movement starts correctly
        agent.ResetPath();
        GoToNextPoint();
    }
    // 🔹 Delay before setting first patrol point to prevent bouncing
    IEnumerator StartPatrolAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        GoToNextPoint();
    }

    IEnumerator ReturnToPatrol()
    {
        isReturningToPatrol = true;
        yield return new WaitForSeconds(3f);

        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalking", true);

        agent.speed = 2f;
        GoToNextPoint();
        isReturningToPatrol = false;
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        Vector3 nextPoint = patrolPoints[currentPoint].position;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(nextPoint);

        Debug.Log($"Moving to patrol point {currentPoint}");

        StartCoroutine(CheckPatrolArrival());
    }




    IEnumerator SetNextPatrolPoint()
    {
        yield return new WaitForSeconds(0.2f); // Small delay before setting a new path

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPoint].position);
        StartCoroutine(CheckPatrolArrival());
    }

    IEnumerator CheckPatrolArrival()
    {
        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();

                animator.SetBool("isWalking", false);  // Stop walking only after fully stopping

                Vector3 nextPoint = patrolPoints[(currentPoint + 1) % patrolPoints.Length].position;
                StartCoroutine(SmoothTurn(nextPoint));

                yield return new WaitForSeconds(1f); // 🔹 Reduce idle time

                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPoint].position);
                agent.isStopped = false;

                yield return null;  // 🔹 Wait a single frame before triggering walk

                if (agent.velocity.magnitude > 0.1f)  // 🔹 Only switch animation if actually moving
                {
                    animator.SetBool("isWalking", true);
                }

                break;
            }
            yield return null;
        }
    }

    void ChasePlayer()
    {
        if (!CanSeePlayer())
        {
            StartCoroutine(ReturnToPatrol());
            return;
        }

        if (!agent.enabled) agent.enabled = true;

        agent.isStopped = false;
        agent.speed = 3f;
        agent.SetDestination(player.position);
        FacePlayer();

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("run"))
        {
            animator.Play("run", 0, 0);
        }

        animator.SetBool("isRunning", true);
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // 🔹 Prevent tilting

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
    }



    void AttackPlayer()
    {
        agent.isStopped = true; // Stop movement while attacking
        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", true);

        StartCoroutine(FacePlayerWhileAttacking());
    }
    IEnumerator FacePlayerWhileAttacking()
    {
        while (animator.GetBool("isAttacking")) // 🔹 Keep rotating while attacking
        {
            FacePlayer(); // 🔹 Call the function to smoothly rotate

            yield return null; // 🔹 Wait for the next frame to avoid freezing
        }
    }

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Start ray from chest level
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // 🔹 Give the enemy full 360° vision while chasing
        float currentFOV = animator.GetBool("isRunning") ? 360f : fieldOfViewAngle;

        if (angleToPlayer > currentFOV / 2f) return false;
        if (Vector3.Distance(transform.position, player.position) > detectionRange) return false;

        // 🔹 Raycast to check if the player is visible (not blocked by walls)
        if (Physics.Raycast(rayOrigin, directionToPlayer, out RaycastHit hit, visionDistance, obstacleMask))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }

        return false;
    }

    IEnumerator SmoothTurn(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}
