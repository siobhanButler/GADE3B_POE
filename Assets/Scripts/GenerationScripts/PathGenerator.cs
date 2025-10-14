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
    [Header("Debug Visuals")]
    public bool debugVisualizePathDifficulty = true;
    public Color debugMinColor = new Color(1f, 1f, 0.6f);  // light
    public Color debugMaxColor = new Color(0.2f, 0.2f, 0.2f); // dark
    
    private LargeCell[,] grid;
    private int roomWidth;
    private int roomLength;
    private int subCellsPerLargeCell;

    private Dictionary<SubCell, PathObj> spawnerPaths = new Dictionary<SubCell, PathObj>();
    private Dictionary<SubCell, GameObject> pathTileObjects = new Dictionary<SubCell, GameObject>();
    
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
                spawnerPaths[spawner] = GeneratePathObjFromSpawnerToTower(spawner, mainTower);
            }
        }
    }
    
    private PathObj GeneratePathObjFromSpawnerToTower(SubCell start, SubCell goal)   // Generate path from spawner to tower
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

            PathObj pathObj = new PathObj(path);
            pathObj.RecalculateTotalPathDifficulty();
            return pathObj;
        }
        else
        {
            Debug.LogWarning($"PathGenerator GeneratePathFromSpawnerToTower(): Failed to generate path from spawner to tower");
            return new PathObj(null);
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
                SpawnPathPrefabForSubCell(cell, pathParent);
            }
        }

        // Initialize debug visuals
        RefreshPathDebugVisuals();
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

    private void SpawnPathPrefabForSubCell(SubCell cell, GameObject parent)
    {
        if (cell == null) return;
        SpawnPathPrefab(cell.worldPosition, parent);
        // Last spawned is the child at end
        var go = parent.transform.GetChild(parent.transform.childCount - 1).gameObject;
        pathTileObjects[cell] = go;
    }

    public List<SubCell> GetPathForSpawner(SubCell spawner)
    {
        if (!spawnerPaths.ContainsKey(spawner)) return null;
        return spawnerPaths[spawner].pathCells;
    }

    // Returns a unique list of all subcells that are part of any enemy path
    public List<SubCell> GetAllPathSubCells()
    {
        HashSet<SubCell> unique = new HashSet<SubCell>();
        foreach (var kvp in spawnerPaths)
        {
            var pathObj = kvp.Value;
            var cells = pathObj.pathCells;
            if (cells == null) continue;
            for (int i = 0; i < cells.Count; i++)
            {
                SubCell sc = cells[i];
                if (sc != null && sc.state == CellState.EnemyPath)
                {
                    unique.Add(sc);
                }
            }
        }
        return new List<SubCell>(unique);
    }

    public List<PathObj> GetAllPathObjects()
    {
        List<PathObj> list = new List<PathObj>();
        foreach (var kvp in spawnerPaths)
        {
            list.Add(kvp.Value);
        }
        return list;
    }

    public PathObj GetPathObjForSpawner(SubCell spawner)
    {
        if (spawnerPaths.ContainsKey(spawner))
        {
            return spawnerPaths[spawner];
        }
        return new PathObj(null);
    }

    // Registers a tower across all paths for overlapping subcells; returns total overlapped path subcells count
    public int RegisterTowerOverlaps(TowerManager tower)
    {
        if (tower == null || tower.attack == null || tower.attack.rangeCollider == null) return 0;
        int totalOverlapped = 0;
        // Compute accurate world-space sphere center and radius
        var sphere = tower.attack.rangeCollider;
        Vector3 center = sphere.transform.TransformPoint(sphere.center);
        float worldRadius = sphere.radius * Mathf.Max(Mathf.Abs(sphere.transform.lossyScale.x), Mathf.Abs(sphere.transform.lossyScale.y), Mathf.Abs(sphere.transform.lossyScale.z));
        float worldRadiusSqr = worldRadius * worldRadius;

        // We need to reassign struct after mutation
        var keys = new List<SubCell>(spawnerPaths.Keys);
        for (int k = 0; k < keys.Count; k++)
        {
            var key = keys[k];
            PathObj obj = spawnerPaths[key];
            var cells = obj.pathCells;
            if (cells == null) continue;

            bool addedToThisPath = false;
            for (int i = 0; i < cells.Count; i++)
            {
                SubCell cell = cells[i];
                if (cell == null) continue;
                Vector3 toPoint = cell.worldPosition - center;
                if (toPoint.sqrMagnitude > worldRadiusSqr) continue;

                totalOverlapped++;
                addedToThisPath = true;

                // Add tower to subcell registration
                if (cell.inRangeTowers == null)
                {
                    cell.inRangeTowers = new TowerManager[] { tower };
                }
                else
                {
                    bool exists = false;
                    for (int t = 0; t < cell.inRangeTowers.Length; t++)
                    {
                        if (cell.inRangeTowers[t] == tower) { exists = true; break; }
                    }
                    if (!exists)
                    {
                        var newArr = new TowerManager[cell.inRangeTowers.Length + 1];
                        for (int t = 0; t < cell.inRangeTowers.Length; t++) newArr[t] = cell.inRangeTowers[t];
                        newArr[newArr.Length - 1] = tower;
                        cell.inRangeTowers = newArr;
                    }
                }
            }

            if (addedToThisPath)
            {
                obj.AddTower(tower);
                obj.RecalculateTotalPathDifficulty();
            }
            spawnerPaths[key] = obj; // reassign mutated struct
        }

        // Update debug visuals after registration
        RefreshPathDebugVisuals();

        return totalOverlapped;
    }

    // Removes a tower from all subcells and path objects, and refreshes debug visuals
    public void UnregisterTowerOverlaps(TowerManager tower)
    {
        if (tower == null) return;

        var keys = new List<SubCell>(spawnerPaths.Keys);
        for (int k = 0; k < keys.Count; k++)
        {
            var key = keys[k];
            PathObj obj = spawnerPaths[key];
            var cells = obj.pathCells;
            bool removedFromAnyCell = false;

            if (cells != null)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    SubCell cell = cells[i];
                    if (cell == null || cell.inRangeTowers == null || cell.inRangeTowers.Length == 0) continue;

                    int keepCount = 0;
                    for (int t = 0; t < cell.inRangeTowers.Length; t++)
                    {
                        if (cell.inRangeTowers[t] != tower) keepCount++;
                    }

                    if (keepCount != cell.inRangeTowers.Length)
                    {
                        removedFromAnyCell = true;
                        if (keepCount == 0)
                        {
                            cell.inRangeTowers = null;
                        }
                        else
                        {
                            var newArr = new TowerManager[keepCount];
                            int idx = 0;
                            for (int t = 0; t < cell.inRangeTowers.Length; t++)
                            {
                                var tw = cell.inRangeTowers[t];
                                if (tw != tower)
                                {
                                    newArr[idx++] = tw;
                                }
                            }
                            cell.inRangeTowers = newArr;
                        }
                    }
                }
            }

            if (removedFromAnyCell)
            {
                obj.RemoveTower(tower);
                obj.RecalculateTotalPathDifficulty();
            }
            spawnerPaths[key] = obj;
        }

        RefreshPathDebugVisuals();
    }

    public void RefreshPathDebugVisuals()
    {
        if (!debugVisualizePathDifficulty) return;
        if (pathTileObjects == null || pathTileObjects.Count == 0) return;

        // Determine max overlaps across all path subcells for normalization
        int maxOverlap = 0;
        foreach (var kv in pathTileObjects)
        {
            SubCell sc = kv.Key;
            if (sc == null) continue;
            int c = sc.inRangeTowers != null ? sc.inRangeTowers.Length : 0;
            if (c > maxOverlap) maxOverlap = c;
        }

        // Avoid division by zero; if no overlaps, paint min color
        foreach (var kv in pathTileObjects)
        {
            GameObject go = kv.Value;
            if (go == null) continue;
            Renderer r = go.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            int count = kv.Key != null && kv.Key.inRangeTowers != null ? kv.Key.inRangeTowers.Length : 0;
            float t = (maxOverlap > 0) ? Mathf.Clamp01((float)count / (float)maxOverlap) : 0f;
            // Darker with more overlaps: lerp from light (min) to dark (max)
            Color c = Color.Lerp(debugMinColor, debugMaxColor, t);

            // Apply color; for debug it's fine to set material.color
            if (r.material != null)
            {
                r.material.color = c;
            }
        }
    }
}

public struct PathObj
{
    public List<SubCell> pathCells;
    public List<TowerManager> towers;      // all towers intersecting this path
    public int totalPathDifficulty;         // sum of each subcell's number of intersecting towers

    public PathObj(List<SubCell> cells)
    {
        pathCells = cells != null ? new List<SubCell>(cells) : new List<SubCell>();
        towers = new List<TowerManager>();
        totalPathDifficulty = 0;
    }

    public void AddPathCell(SubCell cell)
    {
        if (cell == null) return;
        if (pathCells == null) pathCells = new List<SubCell>();
        if (!pathCells.Contains(cell)) pathCells.Add(cell);
    }

    public bool AddTower(TowerManager tower)
    {
        if (tower == null) return false;
        if (towers == null) towers = new List<TowerManager>();
        if (towers.Contains(tower)) return false;
        towers.Add(tower);
        return true;
    }

    public bool RemoveTower(TowerManager tower)
    {
        if (towers == null || tower == null) return false;
        return towers.Remove(tower);
    }

    public int RecalculateTotalPathDifficulty()
    {
        int sum = 0;
        if (pathCells != null)
        {
            for (int i = 0; i < pathCells.Count; i++)
            {
                var sc = pathCells[i];
                if (sc == null) continue;
                int count = sc.inRangeTowers != null ? sc.inRangeTowers.Length : 0;
                sum += count;
            }
        }
        totalPathDifficulty = sum;
        return totalPathDifficulty;
    }
}
