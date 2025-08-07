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
    public float wanderSpeed = 3f;
    public float chaseSpeed = 8f;
    public float wanderRadius = 8f;
    public float wanderInterval = 3f;
    
    [Header("Attack Settings")]
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public string[] attackAnimations = { "Attack_1", "Attack_2", "Attack_3", "Attack_4", "Attack_5" };
    
    [Header("Intimidation Settings")]
    public string[] intimidateAnimations = { "Intimidate_1", "Intimidate_2", "Intimidate_3" };
    public float intimidationDuration = 4f; // Increased from 2f to 4f
    public AudioClip[] intimidateSounds;
    
    [Header("Animation Parameters")]
    public string walkingSlowParam = "Walk_Cycle_2";
    public string walkingFastParam = "Walk_Cycle_1";
    public string idleParam = "Fight_Idle_1";
    
    // State management
    public enum CrabState
    {
        Wandering,
        Intimidating,
        Chasing,
        Attacking,
        Idle
    }
    
    public CrabState currentState = CrabState.Wandering;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float attackTimer;
    private bool isAttacking = false;
    private bool isIntimidating = false;
    private bool hasValidNavMesh = false;
    
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
        
        // Initialize NavMesh Agent with better settings
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = wanderSpeed;
            navMeshAgent.acceleration = 12f; // Faster acceleration
            navMeshAgent.angularSpeed = 180f; // Faster turning
            navMeshAgent.stoppingDistance = attackRange * 0.7f;
            navMeshAgent.autoBraking = true;
            navMeshAgent.updateRotation = true;
            navMeshAgent.updatePosition = true;
            
            // Check if we're on NavMesh
            hasValidNavMesh = navMeshAgent.isOnNavMesh;
            if (!hasValidNavMesh)
            {
                Debug.LogWarning($"Crab {gameObject.name} is not on NavMesh! Position: {transform.position}");
            }
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
        if (player == null || playerHealth == null || !playerHealth.IsAlive())
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
                
            case CrabState.Intimidating:
                HandleIntimidating(distanceToPlayer);
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
            SetState(CrabState.Intimidating); // Changed from Chasing to Intimidating
            return;
        }
        
        // Continue wandering
        if (wanderTimer <= 0f || (navMeshAgent.hasPath && navMeshAgent.remainingDistance < 1f))
        {
            SetNewWanderTarget();
            wanderTimer = wanderInterval;
        }
    }
    
    void HandleIntimidating(float distanceToPlayer)
    {
        // Ensure we're completely stopped during intimidation
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            if (!navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
            }
        }
        
        // Face the player continuously during intimidation
        if (player != null && !isIntimidating)
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0f;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // Check if player moved too far during intimidation
        if (distanceToPlayer > chaseRange && !isIntimidating)
        {
            SetState(CrabState.Wandering);
            return;
        }
        
        // Start intimidation if not already intimidating
        if (!isIntimidating)
        {
            StartCoroutine(PerformIntimidation());
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
        if (distanceToPlayer <= attackRange && attackTimer <= 0f && !isAttacking)
        {
            SetState(CrabState.Attacking);
            return;
        }
        
        // Ensure NavMesh agent is properly configured for chasing
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            if (navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = chaseSpeed;
            }
            
            // Continuously update destination while chasing
            navMeshAgent.SetDestination(player.position);
        }
    }
    
    void HandleAttacking(float distanceToPlayer)
    {
        // Ensure we're completely stopped during attack
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            if (!navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
            }
        }
        
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }
    
    void HandleIdle(float distanceToPlayer)
    {
        // Stop moving
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.ResetPath();
        }
        
        // Check if player comes back to life or gets close
        if (playerHealth != null && playerHealth.IsAlive() && distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            SetState(CrabState.Intimidating); // Changed from Chasing to Intimidating
        }
    }
    
    void SetState(CrabState newState)
    {
        if (currentState == newState) return;
        
        CrabState previousState = currentState;
        currentState = newState;
        
        // Update NavMesh Agent settings based on state
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            switch (newState)
            {
                case CrabState.Wandering:
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = wanderSpeed;
                    break;
                    
                case CrabState.Intimidating:
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero; // Force stop immediately
                    break;
                    
                case CrabState.Chasing:
                    navMeshAgent.isStopped = false;
                    navMeshAgent.speed = chaseSpeed;
                    break;
                    
                case CrabState.Attacking:
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero; // Force stop immediately
                    break;
                    
                case CrabState.Idle:
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero; // Force stop immediately
                    break;
            }
        }
        
        Debug.Log($"Crab {gameObject.name}: {previousState} → {newState}");
    }
    
    void SetNewWanderTarget()
    {
        if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled) return;
        
        for (int i = 0; i < 10; i++) // Try up to 10 times to find a valid position
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y; // Keep same height
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                wanderTarget = hit.position;
                navMeshAgent.SetDestination(wanderTarget);
                return;
            }
        }
        
        // Fallback: stay in place
        Debug.LogWarning($"Crab {gameObject.name}: Could not find valid wander target");
    }
    
    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        // Check if player is in front (wider field of view)
        if (angle < 120f) // Increased from 90f for better detection
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 1.5f; // Higher ray start
            if (Physics.Raycast(rayStart, directionToPlayer, out hit, detectionRange))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
        }
        
        return false;
    }
    
    IEnumerator PerformAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // Ensure we're completely stopped
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }
        
        // Face the player
        if (player != null)
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0f;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // DON'T touch animation bools - let the UpdateAnimations handle idle state
        // Just trigger the attack animation directly
        yield return new WaitForSeconds(0.2f);
        
        // Play random attack animation
        if (animator != null && attackAnimations.Length > 0)
        {
            string attackAnim = attackAnimations[Random.Range(0, attackAnimations.Length)];
            animator.SetTrigger(attackAnim);
            Debug.Log($"Crab {gameObject.name}: Playing attack animation {attackAnim}");
        }
        
        // Play attack sound
        PlayAttackSound();
        
        // Wait much longer before applying damage to let animation play
        yield return new WaitForSeconds(1.5f);
        
        // Apply damage if player is still in range
        if (player != null && playerHealth != null)
        {
            float damageDistance = Vector3.Distance(transform.position, player.position);
            if (damageDistance <= attackRange)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Crab {gameObject.name}: Dealt {attackDamage} damage to player");
            }
        }
        
        // Wait for attack animation to complete - significantly increased duration
        yield return new WaitForSeconds(3f);
        
        isAttacking = false;
        
        // Decide next state after attack
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= chaseRange && playerHealth != null && playerHealth.IsAlive())
            {
                SetState(CrabState.Chasing);
            }
            else
            {
                SetState(CrabState.Wandering);
            }
        }
        else
        {
            SetState(CrabState.Idle);
        }
    }
    
    IEnumerator PerformIntimidation()
    {
        isIntimidating = true;
        
        // Ensure we're completely stopped
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }
        
        // Face the player
        if (player != null)
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0f;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // DON'T touch animation bools - let the UpdateAnimations handle idle state
        // Just trigger the intimidation animation directly
        yield return new WaitForSeconds(0.2f);
        
        // Play random intimidation animation
        if (animator != null && intimidateAnimations.Length > 0)
        {
            string intimidateAnim = intimidateAnimations[Random.Range(0, intimidateAnimations.Length)];
            animator.SetTrigger(intimidateAnim);
            Debug.Log($"Crab {gameObject.name}: Playing intimidation animation {intimidateAnim}");
        }
        
        // Play intimidation sound
        PlayIntimidationSound();
        
        // Wait for intimidation animation to complete - much longer duration
        yield return new WaitForSeconds(intimidationDuration);
        
        isIntimidating = false;
        
        // After intimidation, start chasing if player is still in range
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= chaseRange && playerHealth != null && playerHealth.IsAlive())
            {
                SetState(CrabState.Chasing);
                PlayChaseSound(); // Play chase sound after intimidation
            }
            else
            {
                SetState(CrabState.Wandering);
            }
        }
        else
        {
            SetState(CrabState.Idle);
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // COMPLETELY SKIP animation updates during attacks or intimidation
        // This prevents any interference with trigger-based animations
        if ((currentState == CrabState.Attacking && isAttacking) || 
            (currentState == CrabState.Intimidating && isIntimidating))
        {
            // Do not touch any animation parameters during these states
            return;
        }
        
        // Cache current animation states to avoid unnecessary changes
        bool currentWalkingSlow = animator.GetBool(walkingSlowParam);
        bool currentWalkingFast = animator.GetBool(walkingFastParam);
        bool currentIdle = animator.GetBool(idleParam);
        
        bool targetWalkingSlow = false;
        bool targetWalkingFast = false;
        bool targetIdle = false;
        
        // Determine target animation state
        switch (currentState)
        {
            case CrabState.Wandering:
                if (navMeshAgent != null && !navMeshAgent.isStopped && navMeshAgent.velocity.magnitude > 0.2f)
                {
                    targetWalkingSlow = true;
                }
                else
                {
                    targetIdle = true;
                }
                break;
                
            case CrabState.Chasing:
                if (navMeshAgent != null && !navMeshAgent.isStopped && navMeshAgent.velocity.magnitude > 0.2f)
                {
                    targetWalkingFast = true;
                }
                else
                {
                    targetIdle = true;
                }
                break;
                
            case CrabState.Intimidating:
            case CrabState.Attacking:
            case CrabState.Idle:
                targetIdle = true;
                break;
        }
        
        // Only update parameters if they need to change
        if (currentWalkingSlow != targetWalkingSlow)
        {
            animator.SetBool(walkingSlowParam, targetWalkingSlow);
        }
        
        if (currentWalkingFast != targetWalkingFast)
        {
            animator.SetBool(walkingFastParam, targetWalkingFast);
        }
        
        if (currentIdle != targetIdle)
        {
            animator.SetBool(idleParam, targetIdle);
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
    
    void PlayIntimidationSound()
    {
        if (intimidateSounds != null && intimidateSounds.Length > 0 && audioSource != null)
        {
            AudioClip sound = intimidateSounds[Random.Range(0, intimidateSounds.Length)];
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
        
        // Draw line of sight
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position);
        }
    }
}