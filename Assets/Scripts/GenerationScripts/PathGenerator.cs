using UnityEngine;
using System.Collections.Generic;

public class PathGenerator : MonoBehaviour
{
    [Header("Path Settings")]
    [Range(0f, 1f)]
    public float jitterAmount = 0.3f; // How much to deviate from shortest path
    [Range(0.1f, 2f)]
    public float jitterFrequency = 0.5f; // How often to apply jitter
    public int maxPathAttempts = 3; // Max attempts to find a valid path
    
    [Header("Visual Settings")]
    public GameObject pathPrefab; // Prefab to spawn on each path sub-cell
    public float pathHeight = 0.1f; // Height above floor for path visualization
    
    private LargeCell[,] grid;
    private int roomWidth;
    private int roomLength;
    private int subCellsPerLargeCell;

    private Dictionary<SubCell, PathManager> spawnerPaths = new Dictionary<SubCell, PathManager>();
    
    public void Initialize(LargeCell[,] grid, int roomWidth, int roomLength, int subCellsPerLargeCell)
    {
        this.grid = grid;
        this.roomWidth = roomWidth;
        this.roomLength = roomLength;
        this.subCellsPerLargeCell = subCellsPerLargeCell;
    }
    
    public void GenerateEnemyPaths(SubCell mainTower, SubCell[] enemySpawners)  // Generate paths from spawners to main tower
    {
        if (enemySpawners == null || enemySpawners.Length == 0 || mainTower == null)
        {
            Debug.LogWarning("PathGenerator GenerateEnemyPaths(): No spawners or main tower provided for path generation");
            return;
        }
        
        // Generate a path from each spawner to the main tower
        foreach (SubCell spawner in enemySpawners)
        {
            if (spawner != null)
            {
                spawnerPaths[spawner] = GeneratePathFromSpawnerToTower(spawner, mainTower);
            }
        }
    }
    
    private PathManager GeneratePathFromSpawnerToTower(SubCell start, SubCell goal)   // Generate path from spawner to tower
    {
        List<SubCell> path = FindPathWithJitter(start, goal);
        
        if (path != null && path.Count > 0)
        {
            // Mark path cells and create visual representation
            foreach (SubCell subCell in path)
            {
                if (subCell.state != CellState.MainTower && subCell.state != CellState.EnemySpawner)
                {
                    subCell.state = CellState.EnemyPath;
                    subCell.parentCell.state = CellState.EnemyPath;
                }
            }
            
            CreatePathVisualization(path);
            PathManager pathManager = new PathManager();
            pathManager.path = path;
            return pathManager;
        }
        else
        {
            Debug.LogWarning($"PathGenerator GeneratePathFromSpawnerToTower(): Failed to generate path from spawner to tower");
            return null;
        }
    }
    
    private List<SubCell> FindPathWithJitter(SubCell start, SubCell goal)
    {
        // Try multiple pathfinding attempts with different jitter values
        for (int attempt = 0; attempt < maxPathAttempts; attempt++)
        {
            float currentJitter = jitterAmount * (attempt + 1) / maxPathAttempts;
            
            List<SubCell> path = AStarPathfinding(start, goal, currentJitter);
            
            if (path != null && path.Count > 0)
            {
                // Path found successfully
                return path;
            }
            // Else Pathfinding attempt failed, trying next attempt
        }
        
        // If all attempts fail, try without jitter
        List<SubCell> finalPath = AStarPathfinding(start, goal, 0f);
        if (!(finalPath != null && finalPath.Count > 0))
        {
            Debug.LogError("PathGenerator FindPathWithJitter(): All pathfinding attempts failed!");
        }
        return finalPath;
    }

    private List<SubCell> AStarPathfinding(SubCell start, SubCell goal, float jitter)
    {
        List<SubCell> openSet = new List<SubCell>();
        HashSet<SubCell> closedSet = new HashSet<SubCell>();
        Dictionary<SubCell, SubCell> cameFrom = new Dictionary<SubCell, SubCell>();
        Dictionary<SubCell, float> gScore = new Dictionary<SubCell, float>();
        Dictionary<SubCell, float> fScore = new Dictionary<SubCell, float>();

        openSet.Add(start);
        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        int iterations = 0;
        int maxIterations = 1000; // Prevent infinite loops

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Find node with lowest fScore
            SubCell current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore.ContainsKey(openSet[i]) && fScore[openSet[i]] < fScore[current])
                {
                    current = openSet[i];
                }
            }

            if (current == goal)
            {
                // Path found successfully
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // Check all neighbors
            SubCell[] neighbors = GetNeighbors(current);

            // Process neighbors
            foreach (SubCell neighbor in neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor) || !IsWalkable(neighbor))
                {
                    continue;   // Skip invalid neighbors
                }

                float tentativeGScore = gScore[current] + Distance(current, neighbor);

                // Apply jitter to make path less direct
                if (jitter > 0f && Random.value < jitterFrequency)
                {
                    tentativeGScore += Random.Range(-jitter, jitter) * 10f;
                }

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        if (iterations >= maxIterations)
        {
            Debug.LogError($"PathGenerator AStarPathfinding(): Pathfinding exceeded maximum iterations ({maxIterations})");
            return null; // No path found due to iteration limit
        }
        else
        {
            Debug.LogError($"PathGenerator AStarPathfinding(): No path found after {iterations} iterations. Open set empty.");
            return null; // No path found
        }
    }
    
    private SubCell[] GetNeighbors(SubCell cell)
    {
        // Use the 8-direction neighbor links already established on SubCell
        // These include cross-large-cell connections set up by LargeCell.SetSubCellNeighborsAndBorderStates
        return new SubCell[]
        {
            cell.north,
            cell.south,
            cell.east,
            cell.west,
            cell.northEast,
            cell.southEast,
            cell.southWest,
            cell.northWest
        };
    }
    
    private bool IsWalkable(SubCell cell)
    {
        if (cell == null) return false;
        
        // Allow walking on floor, doors, existing enemy paths, and the goal (main tower)
        return cell.state == CellState.Floor || 
               cell.state == CellState.Door || 
               cell.state == CellState.EnemyPath || 
               cell.state == CellState.MainTower;
    }
    
    private float Heuristic(SubCell a, SubCell b)
    {
        return Vector3.Distance(a.worldPosition, b.worldPosition);
    }
    
    private float Distance(SubCell a, SubCell b)
    {
        return Vector3.Distance(a.worldPosition, b.worldPosition);
    }
    
    private List<SubCell> ReconstructPath(Dictionary<SubCell, SubCell> cameFrom, SubCell current)
    {
        List<SubCell> path = new List<SubCell>();
        path.Add(current);
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }
    
    private void CreatePathVisualization(List<SubCell> path)
    {
        if (path.Count < 2) return;
        
        // Create a parent object to organize all path prefabs
        GameObject pathParent = new GameObject("EnemyPath");
        pathParent.transform.SetParent(transform);
        
        // Spawn path prefab on each path sub-cell
        foreach (SubCell cell in path)
        {
            if (cell.state == CellState.EnemyPath)
            {
                SpawnPathPrefab(cell.worldPosition, pathParent);
            }
        }
    }
    
    private void SpawnPathPrefab(Vector3 position, GameObject parent)
    {
        if (pathPrefab == null)
        {
            Debug.LogWarning("PathGenerator SpawnPathPrefab(): Path prefab is not assigned! Please assign a path prefab in the inspector.");
            return;
        }
        
        // Spawn the path prefab at the sub-cell position
        Vector3 spawnPosition = position + Vector3.up * pathHeight;
        GameObject pathInstance = Instantiate(pathPrefab, spawnPosition, Quaternion.identity);
        pathInstance.name = "PathTile";
        pathInstance.transform.SetParent(parent.transform);
        
        // You can add additional customization here if needed
        // For example, random rotation, scaling, etc.
    }

    public PathManager GetPathForSpawner(SubCell spawner)
    {
        return spawnerPaths.ContainsKey(spawner) ? spawnerPaths[spawner] : null;
    }
}
