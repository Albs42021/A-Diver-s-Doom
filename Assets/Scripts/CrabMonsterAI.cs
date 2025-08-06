using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CrabMonsterAI : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform player;
    private NavMeshAgent navMeshAgent;
    private PlayerHealth playerHealth;
    
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float chaseRange = 15f;
    public float attackRange = 2.5f;
    public LayerMask playerLayer = 1;
    
    [Header("Movement Settings")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 6f;
    public float wanderRadius = 8f;
    public float wanderInterval = 3f;
    
    [Header("Attack Settings")]
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public string[] attackAnimations = { "Attack_1", "Attack_2", "Attack_3" };
    
    [Header("Animation Parameters")]
    public string walkingSlowParam = "Walk_Cycle_2";
    public string walkingFastParam = "Walk_Cycle_1";
    public string idleParam = "Fight_Idle_1";
    
    // State management
    public enum CrabState
    {
        Wandering,
        Chasing,
        Attacking,
        Idle
    }
    
    public CrabState currentState = CrabState.Wandering;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float attackTimer;
    private bool isAttacking = false;
    
    // Audio
    [Header("Audio")]
    public AudioClip[] attackSounds;
    public AudioClip[] chaseSounds;
    private AudioSource audioSource;
    
    void Start()
    {
        // Get components
        navMeshAgent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        
        // Find player if not assigned
        if (player == null)
        {
            FirstPersonController playerController = FindFirstObjectByType<FirstPersonController>();
            if (playerController != null)
            {
                player = playerController.transform;
                playerHealth = playerController.GetComponent<PlayerHealth>();
            }
        }
        else
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        
        // Initialize NavMesh Agent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = wanderSpeed;
            navMeshAgent.stoppingDistance = attackRange * 0.8f;
        }
        
        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D audio
        
        // Set initial wander target
        SetNewWanderTarget();
        wanderTimer = wanderInterval;
    }
    
    void Update()
    {
        if (player == null || !playerHealth.IsAlive())
        {
            SetState(CrabState.Idle);
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Update timers
        wanderTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        
        // State machine
        switch (currentState)
        {
            case CrabState.Wandering:
                HandleWandering(distanceToPlayer);
                break;
                
            case CrabState.Chasing:
                HandleChasing(distanceToPlayer);
                break;
                
            case CrabState.Attacking:
                HandleAttacking(distanceToPlayer);
                break;
                
            case CrabState.Idle:
                HandleIdle(distanceToPlayer);
                break;
        }
        
        // Update animation based on current state
        UpdateAnimations();
    }
    
    void HandleWandering(float distanceToPlayer)
    {
        // Check if player is within detection range
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            SetState(CrabState.Chasing);
            PlayChaseSound();
            return;
        }
        
        // Continue wandering
        if (wanderTimer <= 0f || navMeshAgent.remainingDistance < 0.5f)
        {
            SetNewWanderTarget();
            wanderTimer = wanderInterval;
        }
    }
    
    void HandleChasing(float distanceToPlayer)
    {
        // Check if player is too far away
        if (distanceToPlayer > chaseRange)
        {
            SetState(CrabState.Wandering);
            return;
        }
        
        // Check if close enough to attack
        if (distanceToPlayer <= attackRange && attackTimer <= 0f)
        {
            SetState(CrabState.Attacking);
            return;
        }
        
        // Chase the player
        navMeshAgent.SetDestination(player.position);
    }
    
    void HandleAttacking(float distanceToPlayer)
    {
        // Stop moving during attack
        navMeshAgent.SetDestination(transform.position);
        
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
        
        // Return to chasing if attack is complete
        if (!isAttacking)
        {
            if (distanceToPlayer <= chaseRange)
            {
                SetState(CrabState.Chasing);
            }
            else
            {
                SetState(CrabState.Wandering);
            }
        }
    }
    
    void HandleIdle(float distanceToPlayer)
    {
        // Stop moving
        navMeshAgent.SetDestination(transform.position);
        
        // Check if player comes back to life or gets close
        if (playerHealth.IsAlive() && distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            SetState(CrabState.Chasing);
        }
    }
    
    void SetState(CrabState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        // Update NavMesh Agent settings based on state
        switch (newState)
        {
            case CrabState.Wandering:
                navMeshAgent.speed = wanderSpeed;
                break;
                
            case CrabState.Chasing:
                navMeshAgent.speed = chaseSpeed;
                break;
                
            case CrabState.Attacking:
            case CrabState.Idle:
                navMeshAgent.speed = 0f;
                break;
        }
    }
    
    void SetNewWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y; // Keep same height
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            wanderTarget = hit.position;
            navMeshAgent.SetDestination(wanderTarget);
        }
    }
    
    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        // Check if player is in front (180 degree view)
        if (angle < 90f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange))
            {
                return hit.transform == player;
            }
        }
        
        return false;
    }
    
    IEnumerator PerformAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // Face the player
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0f;
        transform.rotation = Quaternion.LookRotation(lookDirection);
        
        // Play random attack animation
        if (animator != null && attackAnimations.Length > 0)
        {
            string attackAnim = attackAnimations[Random.Range(0, attackAnimations.Length)];
            animator.SetTrigger(attackAnim);
        }
        
        // Play attack sound
        PlayAttackSound();
        
        // Wait a bit before applying damage (for animation timing)
        yield return new WaitForSeconds(0.5f);
        
        // Apply damage if player is still in range
        float damageDistance = Vector3.Distance(transform.position, player.position);
        if (damageDistance <= attackRange && playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        
        // Wait for attack animation to complete
        yield return new WaitForSeconds(1f);
        
        isAttacking = false;
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Reset all animation bools
        animator.SetBool(walkingSlowParam, false);
        animator.SetBool(walkingFastParam, false);
        animator.SetBool(idleParam, false);
        
        // Set appropriate animation based on state
        switch (currentState)
        {
            case CrabState.Wandering:
                if (navMeshAgent.velocity.magnitude > 0.1f)
                {
                    animator.SetBool(walkingSlowParam, true); // Use Walk_Cycle_2 for slow wandering
                }
                else
                {
                    animator.SetBool(idleParam, true); // Use Fight_Idle_1 when not moving
                }
                break;
                
            case CrabState.Chasing:
                animator.SetBool(walkingFastParam, true); // Use Walk_Cycle_1 for fast chasing
                break;
                
            case CrabState.Attacking:
                // Attack animations are handled by triggers in PerformAttack()
                animator.SetBool(idleParam, true); // Use Fight_Idle_1 during attacks
                break;
                
            case CrabState.Idle:
                animator.SetBool(idleParam, true); // Use Fight_Idle_1 when idle
                break;
        }
    }
    
    void PlayAttackSound()
    {
        if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
        {
            AudioClip sound = attackSounds[Random.Range(0, attackSounds.Length)];
            audioSource.PlayOneShot(sound);
        }
    }
    
    void PlayChaseSound()
    {
        if (chaseSounds != null && chaseSounds.Length > 0 && audioSource != null)
        {
            AudioClip sound = chaseSounds[Random.Range(0, chaseSounds.Length)];
            audioSource.PlayOneShot(sound);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw chase range
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw wander radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        
        // Draw current target
        if (currentState == CrabState.Wandering)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(wanderTarget, 0.5f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }
}