using ScriplableObject;

namespace _script
{
    using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ArtefactGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] float maxPlacementAttempts = 100;
    [SerializeField] float raycastYOffset = 5f; 
    [SerializeField] LayerMask terrainLayer; 

    [Header("References")]
    [SerializeField] Transform playerStartPoint; 
    [SerializeField] TerrainGen terrainGenScript; 

    [Header("Artefact Prefabs")] public Artefact[] artefactPrefabs;

    [Header("Path Visualization")]
    [SerializeField] GameObject pathLineRendererPrefab; 
    [SerializeField] bool isPathVisualizationEnabled = true; 


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

        // Loop through all artefacts
        for (int i = 0; i < artefactPrefabs.Length; i++)
        {
            GenerateArtefactType(artefactPrefabs[i]);
        }
    }
    

    private void GenerateArtefactType(Artefact artefact)
    {
        int placedCount = 0;
        int attempts = 0;

        while (placedCount < artefact.spawnCount && attempts < maxPlacementAttempts)
        {
            attempts++;

            Vector3 potentialPos = GetRandomPositionOnMap(); 
            
            //Snap position to terrain and get its properties (Height, Slope)
            if (TryGetTerrainProperties(potentialPos, out Vector3 snappedPos, out float normalizedHeight, out float slopeAngle))
            {
                if (CheckPlacementRules(artefact, normalizedHeight, slopeAngle))
                {
                    if (IsAccessible(playerStartPoint.position, snappedPos, out NavMeshPath path))
                    {
                        var artefactInstance = Instantiate(artefact.prefab, snappedPos, artefact.prefab.transform.rotation, transform);
                        artefactInstance.transform.parent = gameObject.transform;
                        placedCount++;
                        
                        if (isPathVisualizationEnabled)
                        {
                            VisualizePath(path);
                        }
                    }
                }
            }
        }
        Debug.Log($"Successfully placed {placedCount} instances of {artefact.name}.");
    }


    private Vector3 GetRandomPositionOnMap()
    {
        float randX = Random.Range(0f, terrainGenScript.Width);
        float randZ = Random.Range(0f, terrainGenScript.Length);
        
        // Use a high Y value for raycasting down
        return new Vector3(randX, 50f, randZ);
    }
    
    private bool CheckPlacementRules(Artefact artefact, float normalizedHeight, float slopeAngle)
    {
        // if the value is more than 0(is set) + condition
        if (artefact.minSpawnHeight >= 0 && normalizedHeight < artefact.minSpawnHeight)
        {
            return false;
        }

        if (artefact.maxSpawnHeight >= 0 && normalizedHeight > artefact.maxSpawnHeight)
        {
            return false;
        }

        if (artefact.minSlopeAngle >= 0 && slopeAngle < artefact.minSlopeAngle)
        {
            return false;
        }
        if (artefact.maxSlopeAngle >= 0f && slopeAngle > artefact.maxSlopeAngle)
        {
            return false;
        }

        return true;
        
    }
    

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
    

    private void VisualizePath(NavMeshPath path)
    {
        if (path.corners.Length < 2) return;

        // Instantiate a LineRenderer object
        GameObject lineObj = Instantiate(pathLineRendererPrefab, transform);
        LineRenderer line = lineObj.GetComponent<LineRenderer>();

        if (line != null)
        {
            
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
        }
    }
}
}