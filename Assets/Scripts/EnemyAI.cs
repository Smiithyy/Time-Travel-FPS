using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float detectionRange = 10f;
    public float attackRange = 3f;

    private int currentPoint = 0;
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;

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

        if (distance <= attackRange && CanSeePlayer()) // Only attack if the enemy sees the player
        {
            AttackPlayer();
            // Make the enemy always face the player while attacking
            FacePlayer(); // Rotate to face the player while attacking
        }
        else if (distance <= detectionRange && CanSeePlayer()) // Only chase if the enemy sees the player
        {
            animator.SetBool("isAttacking", false);
            agent.isStopped = false; // Resume movement
            ChasePlayer();
        }
        else // If no player detected, patrol
        {
            animator.SetBool("isAttacking", false);

            // Check if the enemy has reached the patrol point before switching
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                GoToNextPoint();
            }
        }
    }

    // Patrol between points
    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        // Ensure the enemy is moving
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        // Set the patrol destination
        agent.SetDestination(patrolPoints[currentPoint].position);
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