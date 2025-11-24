namespace _script
{
    using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ArtefactGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] int objectsPerType = 3;
    [SerializeField] float maxPlacementAttempts = 100;
    [SerializeField] float raycastYOffset = 5f; // Offset for raycasting down to terrain
    [SerializeField] LayerMask terrainLayer; // Assign the layer your terrain mesh is on

    [Header("References")]
    [SerializeField] Transform playerStartPoint; // The position used for path validation
    [SerializeField] TerrainGen terrainGenScript; // Reference to calculate min/max height

    [Header("Artefact Prefabs (6 Types)")]
    public GameObject[] artefactPrefabs = new GameObject[6];

    // Pathfinder Visualization Settings (Part 2b)
    [Header("Path Visualization")]
    [SerializeField] GameObject pathLineRendererPrefab; 
    [SerializeField] bool isPathVisualizationEnabled = true; // Toggle option

    // --- START: Main Entry Point ---

    void Start()
    {
        Invoke(nameof(StartGeneration), 3);
        
    }

    public void StartGeneration()
    {
        if (terrainGenScript == null || playerStartPoint == null)
        {
            Debug.LogError("Artefact Generator is missing references!");
            return;
        }

        // Loop through all 6 artefact types
        for (int i = 0; i < artefactPrefabs.Length; i++)
        {
            GenerateArtefactType(artefactPrefabs[i]);
        }
    }

    // --- CORE LOGIC: Placement and Validation ---

    private void GenerateArtefactType(GameObject prefab)
    {
        int placedCount = 0;
        int attempts = 0;
        string artefactName = prefab.name;

        while (placedCount < objectsPerType && attempts < maxPlacementAttempts)
        {
            attempts++;

            // 1. Get a random position on the map
            Vector3 potentialPos = GetRandomPositionOnMap(); 
            
            // 2. Snap position to terrain and get its properties (Height, Slope)
            if (TryGetTerrainProperties(potentialPos, out Vector3 snappedPos, out float normalizedHeight, out float slopeAngle))
            {
                // 3. CHECK PLACEMENT RULES (Part 2a)
                if (CheckPlacementRules(artefactName, normalizedHeight, slopeAngle))
                {
                    // 4. NAVMESH ACCESSIBILITY CHECK (Part 2b)
                    if (IsAccessible(playerStartPoint.position, snappedPos, out NavMeshPath path))
                    {
                        // SUCCESS: Place the object
                        Instantiate(prefab, snappedPos, Quaternion.identity, transform);
                        placedCount++;
                        
                        // 5. VISUALIZATION
                        if (isPathVisualizationEnabled)
                        {
                            VisualizePath(path);
                        }
                    }
                }
            }
        }
        Debug.Log($"Successfully placed {placedCount} instances of {artefactName}.");
    }

    // --- HELPER 1: Random Position Generation ---

    private Vector3 GetRandomPositionOnMap()
    {
        // Generates a random coordinate within the terrain bounds (e.g., 0 to 50)
        float randX = Random.Range(0f, terrainGenScript.Width);
        float randZ = Random.Range(0f, terrainGenScript.Length);
        
        // Use a high Y value for raycasting down
        return new Vector3(randX, 50f, randZ);
    }
    
    // --- HELPER 2: Check Terrain Rules (Crucial for Part 2a) ---
    // NOTE: This uses the rules planned in the previous response.
    private bool CheckPlacementRules(string name, float normalizedHeight, float slopeAngle)
    {
        // Use your planned rules (A1-A6) here. Normalized height is 0 (min) to 1 (max).
        switch (name)
        {
            case "Health Potion": // A1: Mid-level plains (Grass)
                return normalizedHeight > 0.35f && normalizedHeight < 0.55f;
            
            case "Ancient Coin Stack": // A2: Low-lying shore/sand
                return normalizedHeight > 0.28f && normalizedHeight < 0.31f;

            case "Treasure Chest": // A3: Flat, high-altitude (Mountain Plateau)
                return normalizedHeight > 0.75f && slopeAngle < 10f; 

            case "Poison Trap": // A4: Steep slopes
                return slopeAngle > 35f;

            case "Magic Pick-up": // A5: Highest peaks (Snow)
                return normalizedHeight > 0.9f;

            case "Weapon (Sword)": // A6: Any non-submerged land (Above deep water)
                return normalizedHeight > 0.2f;

            default:
                return false;
        }
    }
    
    // --- HELPER 3: Get Terrain Properties (Height & Slope) ---

    private bool TryGetTerrainProperties(Vector3 checkPos, out Vector3 snappedPos, out float normalizedHeight, out float slopeAngle)
    {
        snappedPos = Vector3.zero;
        normalizedHeight = 0f;
        slopeAngle = 0f;

        // Raycast down to the terrain to get the exact height and surface normal
        if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 60f, terrainLayer))
        {
            snappedPos = hit.point;
            
            // Calculate Normalized Height (required for the rules)
            float currentHeight = snappedPos.y;
            float min = terrainGenScript.minHeight;
            float max = terrainGenScript.maxHeight;
            normalizedHeight = Mathf.InverseLerp(min, max, currentHeight);
            
            // Calculate Slope Angle (required for the rules)
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            
            return true;
        }
        Debug.DrawRay(checkPos, Vector3.down * 60f, Color.red, 5f);
        return false;
    }

    // --- HELPER 4: NavMesh Accessibility Check (Crucial for Part 2b) ---

    private bool IsAccessible(Vector3 start, Vector3 end, out NavMeshPath path)
    {
        path = new NavMeshPath();
        
        // 1. Warp start/end points onto the NavMesh if they aren't already exactly on it
        NavMesh.SamplePosition(start, out NavMeshHit startHit, 5f, NavMesh.AllAreas);
        NavMesh.SamplePosition(end, out NavMeshHit endHit, 5f, NavMesh.AllAreas);

        // 2. Calculate the path between the player's start and the potential object location
        NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path);

        // 3. Return true only if the path is complete (i.e., not blocked by water/cliffs)
        return path.status == NavMeshPathStatus.PathComplete; 
    }
    
    // --- HELPER 5: Path Visualization (Crucial for Part 2b) ---

    private void VisualizePath(NavMeshPath path)
    {
        if (path.corners.Length < 2) return;

        // Instantiate a LineRenderer object
        GameObject lineObj = Instantiate(pathLineRendererPrefab, transform);
        LineRenderer line = lineObj.GetComponent<LineRenderer>();

        if (line != null)
        {
            // Set the path points (corners) to the LineRenderer
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
        }
        // NOTE: You can add a short timer to destroy the line after a few seconds if desired.
    }
}
}