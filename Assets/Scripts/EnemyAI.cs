using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float detectionRange = 10f;
    public float attackRange = 3f;

    private int currentPoint = 0;
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private bool hasReachedPoint = false;


    [Header("Enemy Vision Settings")]
    public float fieldOfViewAngle = 90f; // How wide the enemy's vision is
    public float visionDistance = 15f; // How far the enemy can see
    public LayerMask obstacleMask; // Walls, objects that block vision

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Get Animator component
        player = GameObject.FindGameObjectWithTag("Player").transform;

        GoToNextPoint();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange && CanSeePlayer())
        {
            AttackPlayer();
            FacePlayer();
        }
        else if (distance <= detectionRange && CanSeePlayer())
        {
            animator.SetBool("isAttacking", false);
            agent.isStopped = false;
            ChasePlayer();
        }
        else
        {
            animator.SetBool("isAttacking", false);

            // Ensure the enemy only stops when it reaches the patrol point
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (!hasReachedPoint)  // Prevents looping back & forth
                {
                    hasReachedPoint = true;
                    Debug.Log("Enemy reached patrol point: " + currentPoint);
                    GoToNextPoint();
                }
            }
            else
            {
                hasReachedPoint = false; // Reset when moving
            }
        }
    }





    // Patrol between points
    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        // Stop movement fully before processing the next step
        agent.isStopped = true;
        agent.velocity = Vector3.zero;  // Prevents sliding
        animator.SetBool("isWalking", false);  // Play idle animation

        Debug.Log("Enemy stopped at patrol point: " + currentPoint);

        // Start idle pause before moving again
        StartCoroutine(IdlePause());
    }




    IEnumerator IdlePause()
    {
        Debug.Log("Pausing at patrol point: " + currentPoint);

        // Step 1: Rotate before moving again
        yield return StartCoroutine(SmoothTurn(patrolPoints[currentPoint].position));

        // Step 2: Wait before continuing patrol
        yield return new WaitForSeconds(2f);

        // Step 3: Move to next patrol point
        Vector3 nextPoint = patrolPoints[currentPoint].position;
        agent.isStopped = false;
        animator.SetBool("isWalking", true);

        Debug.Log("Moving to next patrol point: " + currentPoint);

        // Step 4: Set destination and update patrol point index AFTER movement starts
        agent.SetDestination(nextPoint);
        currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    IEnumerator SmoothTurn(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        direction.y = 0; // Prevents tilting

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Debug.Log("Turning towards patrol point: " + currentPoint);

        // Step 1: Stop movement while turning
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Step 2: Rotate smoothly towards the target point
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            yield return null;
        }

        // Step 3: Snap to exact rotation before moving
        transform.rotation = targetRotation;

        yield return new WaitForSeconds(0.1f); // Small delay to prevent instant movement issues
    }



    // Chase the player
    void ChasePlayer()
    {
        agent.isStopped = false;  // Ensure enemy doesn't freeze
        agent.SetDestination(player.position);
        animator.SetBool("isWalking", true);  // Ensure walking animation is set
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Prevent tilting
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // Attack the player
    void AttackPlayer()
    {
        agent.isStopped = true; // Stop moving while attacking
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", true);

        // Rotate to face the player (but only when attacking)
        FacePlayer();
    }


    void ResumeChase()
    {
        agent.isStopped = false;
        animator.SetBool("isWalking", true); // Go back to walking after attacking
        ChasePlayer();
    }

    // If the enemy is hit
    public void TakeDamage()
    {
        ChasePlayer(); // Enemy reacts and starts chasing
    }

    // Enemy dies
    public void Die()
    {
        animator.SetTrigger("Die"); // Play death animation
        agent.isStopped = true;
        this.enabled = false; // Disable AI after death
    }

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Start ray from chest level
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);



        if (angleToPlayer > fieldOfViewAngle / 2f)
        {
            Debug.Log("Player is OUTSIDE enemy FOV");
            return false;
        }

        // Debug: Draw the ray in Scene View
        Debug.DrawRay(rayOrigin, directionToPlayer * visionDistance, Color.red, 0.1f);

        // Raycast to check if there's an obstacle
        if (Physics.Raycast(rayOrigin, directionToPlayer, out RaycastHit hit, visionDistance, obstacleMask))
        {
            Debug.Log("Raycast hit: " + hit.collider.name);

            if (hit.collider.CompareTag("Player")) // Check if it's hitting the player
            {
                Debug.Log("ENEMY SEES THE PLAYER!");
                Debug.DrawRay(rayOrigin, directionToPlayer * hit.distance, Color.green, 0.1f);
                return true;
            }
            else
            {
                Debug.Log("Something is blocking view: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything.");
        }

        return false;
    }

}