using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Component that can be added to surfaces to define their type
public class SurfaceType : MonoBehaviour
{
    [Header("Surface Configuration")]
    public string surfaceTypeName = "Default";
    
    [Header("Terrain Layer Support")]
    public bool isTerrainSurface = false;
    public TerrainLayerMapping[] terrainLayerMappings;
    
    [Header("Visual Indicators (Optional)")]
    [TextArea(2, 4)]
    public string description = "Add this component to GameObjects to define their surface type for footstep sounds. For terrain objects, enable 'Is Terrain Surface' and drag terrain layers to create mappings.";
    
    private Terrain terrain;
    private TerrainData terrainData;
    
    private void Awake()
    {
        if (isTerrainSurface)
        {
            terrain = GetComponent<Terrain>();
            if (terrain != null)
            {
                terrainData = terrain.terrainData;
            }
        }
    }
    
    private void OnValidate()
    {
        // Ensure surface type name is not empty
        if (string.IsNullOrEmpty(surfaceTypeName))
        {
            surfaceTypeName = "Default";
        }
        
        // Auto-detect terrain component
        if (isTerrainSurface && terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }
    }
    
    /// <summary>
    /// Get the surface type at a specific world position on the terrain
    /// </summary>
    /// <param name="worldPosition">World position to check</param>
    /// <returns>Surface type name for the dominant terrain layer at that position</returns>
    public string GetSurfaceTypeAtPosition(Vector3 worldPosition)
    {
        if (!isTerrainSurface || terrain == null || terrainData == null)
        {
            return surfaceTypeName;
        }
        
        // Convert world position to terrain coordinates
        Vector3 terrainPosition = worldPosition - terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        
        // Calculate normalized position (0-1) on terrain
        float normalizedX = terrainPosition.x / terrainSize.x;
        float normalizedZ = terrainPosition.z / terrainSize.z;
        
        // Clamp to terrain bounds
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);
        
        // Get terrain layer weights at this position
        string dominantSurfaceType = GetDominantTerrainLayer(normalizedX, normalizedZ);
        
        return !string.IsNullOrEmpty(dominantSurfaceType) ? dominantSurfaceType : surfaceTypeName;
    }
    
    private string GetDominantTerrainLayer(float normalizedX, float normalizedZ)
    {
        if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
        {
            return surfaceTypeName;
        }
        
        // Get alphamap resolution
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        
        // Convert normalized coordinates to alphamap coordinates
        int x = Mathf.FloorToInt(normalizedX * (alphamapWidth - 1));
        int y = Mathf.FloorToInt(normalizedZ * (alphamapHeight - 1));
        
        // Get the alphamap data
        float[,,] alphaMap = terrainData.GetAlphamaps(x, y, 1, 1);
        
        // Find the dominant layer
        float maxWeight = 0f;
        int dominantLayerIndex = 0;
        
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            float weight = alphaMap[0, 0, i];
            if (weight > maxWeight)
            {
                maxWeight = weight;
                dominantLayerIndex = i;
            }
        }
        
        // Get the surface type for the dominant layer
        string mappedSurfaceType = GetSurfaceTypeForLayer(dominantLayerIndex);
        return mappedSurfaceType;
    }
    
    private string GetSurfaceTypeForLayer(int layerIndex)
    {
        if (terrainData.terrainLayers == null || layerIndex >= terrainData.terrainLayers.Length)
        {
            return surfaceTypeName;
        }
        
        TerrainLayer terrainLayer = terrainData.terrainLayers[layerIndex];
        
        if (terrainLayer == null)
        {
            return surfaceTypeName;
        }
        
        // Check if we have a mapping for this terrain layer
        if (terrainLayerMappings != null)
        {
            foreach (var mapping in terrainLayerMappings)
            {
                if (mapping.terrainLayer == terrainLayer)
                {
                    return mapping.surfaceTypeName;
                }
            }
        }
        
        // Fallback: try to determine surface type from terrain layer name
        string layerName = terrainLayer.name.ToLower();
        
        if (layerName.Contains("grass")) return "Grass";
        if (layerName.Contains("dirt") || layerName.Contains("soil")) return "Dirt";
        if (layerName.Contains("sand")) return "Sand";
        if (layerName.Contains("stone") || layerName.Contains("rock")) return "Stone";
        if (layerName.Contains("gravel")) return "Gravel";
        if (layerName.Contains("mud")) return "Mud";
        if (layerName.Contains("snow")) return "Snow";
        if (layerName.Contains("metal")) return "Metal";
        if (layerName.Contains("wood")) return "Wood";
        if (layerName.Contains("concrete")) return "Concrete";
        
        return surfaceTypeName;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Auto-populate terrain layer mappings based on current terrain layers
    /// </summary>
    [ContextMenu("Auto-Generate Terrain Layer Mappings")]
    public void AutoGenerateTerrainLayerMappings()
    {
        if (!isTerrainSurface)
        {
            Debug.LogWarning("Enable 'Is Terrain Surface' first!");
            return;
        }
        
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }
        
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("No terrain found on this GameObject!");
            return;
        }
        
        TerrainLayer[] layers = terrain.terrainData.terrainLayers;
        if (layers == null || layers.Length == 0)
        {
            Debug.LogWarning("No terrain layers found on this terrain!");
            return;
        }
        
        List<TerrainLayerMapping> mappings = new List<TerrainLayerMapping>();
        
        foreach (var layer in layers)
        {
            if (layer != null)
            {
                TerrainLayerMapping mapping = new TerrainLayerMapping
                {
                    terrainLayer = layer,
                    surfaceTypeName = GetSurfaceTypeForLayer(System.Array.IndexOf(layers, layer))
                };
                mappings.Add(mapping);
            }
        }
        
        terrainLayerMappings = mappings.ToArray();
        
        Debug.Log($"Generated {mappings.Count} terrain layer mappings!");
        
        // Mark object as dirty for editor
        EditorUtility.SetDirty(this);
    }
    #endif
}

/// <summary>
/// Maps terrain layers to surface types for footstep sounds
/// </summary>
[System.Serializable]
public class TerrainLayerMapping
{
    [Header("Terrain Layer")]
    public TerrainLayer terrainLayer;
    
    [Header("Surface Type")]
    public string surfaceTypeName = "Default";
    
    [Header("Visual Info")]
    public string layerDescription = "Drag terrain layer asset here, then set the surface type name";
}