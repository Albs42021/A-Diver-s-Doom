using UnityEngine;

public class CrabMonsterSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject crabMonsterPrefab;
    public int spawnCount = 3;
    public float spawnRadius = 20f;
    public float minDistanceFromPlayer = 5f;
    public LayerMask groundLayer = 1;
    
    [Header("Spawn Conditions")]
    public bool spawnOnStart = true;
    public bool checkNavMesh = true;
    
    private Transform player;
    
    void Start()
    {
        // Find player
        FirstPersonController playerController = FindFirstObjectByType<FirstPersonController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        
        if (spawnOnStart)
        {
            SpawnCrabMonsters();
        }
    }
    
    public void SpawnCrabMonsters()
    {
        if (crabMonsterPrefab == null)
        {
            Debug.LogError("CrabMonsterSpawner: No crab monster prefab assigned!");
            return;
        }
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = spawnCount * 10; // Prevent infinite loops
        
        while (spawned < spawnCount && attempts < maxAttempts)
        {
            attempts++;
            
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            if (IsValidSpawnPosition(spawnPosition))
            {
                SpawnCrabMonster(spawnPosition);
                spawned++;
            }
        }
        
        Debug.Log($"CrabMonsterSpawner: Spawned {spawned}/{spawnCount} crab monsters in {attempts} attempts.");
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Try to find ground level
        RaycastHit hit;
        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
        {
            spawnPosition = hit.point;
        }
        
        return spawnPosition;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        // Check distance from player
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(position, player.position);
            if (distanceToPlayer < minDistanceFromPlayer)
            {
                return false;
            }
        }
        
        // Check if position is on NavMesh (if required)
        if (checkNavMesh)
        {
            UnityEngine.AI.NavMeshHit navHit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(position, out navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return false;
            }
        }
        
        // Check if there's enough space (no overlapping colliders)
        Collider[] overlapping = Physics.OverlapSphere(position, 1f);
        foreach (Collider col in overlapping)
        {
            if (col.gameObject != gameObject && col.GetComponent<CrabMonsterAI>() != null)
            {
                return false; // Too close to another crab monster
            }
        }
        
        return true;
    }
    
    void SpawnCrabMonster(Vector3 position)
    {
        GameObject crabMonster = Instantiate(crabMonsterPrefab, position, Quaternion.identity);
        
        // Set random rotation
        crabMonster.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        
        // Ensure the crab monster has the AI component
        CrabMonsterAI ai = crabMonster.GetComponent<CrabMonsterAI>();
        if (ai == null)
        {
            ai = crabMonster.AddComponent<CrabMonsterAI>();
        }
        
        // Set player reference
        if (player != null)
        {
            ai.player = player;
        }
        
        Debug.Log($"Spawned crab monster at {position}");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Draw minimum distance from player
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
        }
    }
}