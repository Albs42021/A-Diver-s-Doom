using UnityEngine;

/// <summary>
/// This script helps set up the CrabMonster prefab with the AI system.
/// Attach this to the crab monster prefab to automatically configure it.
/// </summary>
public class CrabMonsterSetup : MonoBehaviour
{
    [Header("Setup Options")]
    public bool autoSetupOnStart = true;
    public bool addNavMeshAgent = true;
    public bool addAudioSource = true;
    public bool disableRikayonScript = true;
    
    [Header("NavMesh Agent Settings")]
    public float agentRadius = 0.5f;
    public float agentHeight = 2f;
    public float baseOffset = 0f;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCrabMonster();
        }
    }
    
    [ContextMenu("Setup Crab Monster")]
    public void SetupCrabMonster()
    {
        // Add NavMeshAgent if needed
        if (addNavMeshAgent)
        {
            UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }
            
            // Configure NavMeshAgent
            navAgent.radius = agentRadius;
            navAgent.height = agentHeight;
            navAgent.baseOffset = baseOffset;
            navAgent.speed = 2f; // Will be overridden by AI script
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 2f;
            navAgent.autoBraking = true;
        }
        
        // Add AudioSource if needed
        if (addAudioSource)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Configure AudioSource
            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.volume = 0.7f;
            audioSource.pitch = 1f;
            audioSource.playOnAwake = false;
        }
        
        // Add CrabMonsterAI if needed
        CrabMonsterAI ai = GetComponent<CrabMonsterAI>();
        if (ai == null)
        {
            ai = gameObject.AddComponent<CrabMonsterAI>();
        }
        
        // Auto-assign animator
        if (ai.animator == null)
        {
            ai.animator = GetComponent<Animator>();
            if (ai.animator == null)
            {
                ai.animator = GetComponentInChildren<Animator>();
            }
        }
        
        // Disable Rikayon script if it exists (demo script)
        if (disableRikayonScript)
        {
            Rikayon rikayon = GetComponent<Rikayon>();
            if (rikayon != null)
            {
                rikayon.enabled = false;
                Debug.Log("Disabled Rikayon demo script on " + gameObject.name);
            }
        }
        
        // Find and assign player
        FirstPersonController playerController = FindFirstObjectByType<FirstPersonController>();
        if (playerController != null && ai.player == null)
        {
            ai.player = playerController.transform;
        }
        
        Debug.Log("Crab Monster setup complete on " + gameObject.name);
    }
    
    void OnValidate()
    {
        // Ensure values stay within reasonable ranges
        agentRadius = Mathf.Max(0.1f, agentRadius);
        agentHeight = Mathf.Max(0.1f, agentHeight);
    }
}