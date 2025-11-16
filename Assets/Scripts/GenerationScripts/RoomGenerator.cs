using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles procedural generation of tower defense rooms including:
/// - Room layout with configurable dimensions
/// - Corner removal for varied layouts
/// - Wall placement based on borders and removed areas
/// - Gameplay element placement (main tower, spawners, defense towers)
/// - Furniture placement with collision avoidance
/// - Enemy path generation
/// </summary>
public class RoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public int minRoomWidth = 5;        //x
    public int maxRoomWidth = 12;
    public int minRoomLength = 5;       //z
    public int maxRoomLength = 12;
    
    [Header("Cell Settings")]
    private float subCellSize = 1f; // Size of each sub-cell (1x1) - don't chnage, it will mess shit up :( 
    public int subCellsPerLargeCell = 10; // 9x9/10x10 sub-grid per large cell
    public float largeCellSize = 10f; // Size of each large cell
    
     [Header("Generation Settings")]
     [Range(0f, 1f)]
     public float cornerRemovalChance = 0.3f; // Chance to remove corners
     [Range(0f, 1f)]
     public float furniturePlacementChance = 0.4f;
     [Range(0f, 1f)]
     public float maxFurniturePercent = 0.4f;
     
     [Header("Spawner Settings")]
     public int minSpawnerCount = 2;
     public int maxSpawnerCount = 5;
    public int spawnerCount = 0;

     [Header("Defence Tower Settings")]
     public int minDefenceCount = 10;
     public int maxDefenceCount = 30;
     public int maxLocationDistance = 5; // max subcell steps from an EnemyPath
    
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject furniturePrefab;
    public GameObject chairPrefab;
    public GameObject mainTowerPrefab;
    public GameObject enemySpawnerPrefab;
    public GameObject defenseTowerLocationPrefab;

    [Header("Furniture Options")]
    public GameObject[] furniturePrefabs;   // Multiple furniture types to choose from
	[Tooltip("Editor/debug helper. When true, spawns a furniture instance at every subcell marked Furniture. Leave off for gameplay.")]
	public bool enableFurnitureDebugPlacement = false;
    
    // Private fields
    private GridSystem gridSystem;
    private CellManager cellManager;
    private PathGenerator pathGenerator;
    private int roomWidth;      //x
    private int roomLength;     //z
    private LargeCell[,] grid;  //grid[x,z]

    SubCell mainTowerCell;
    SubCell[] enemySpawnerCells;
    SubCell[] defenceTowerCells;
    
    void Start()
    {
        largeCellSize = subCellsPerLargeCell * subCellSize;
        GenerateRoom();
    }

    /// <summary>
    /// Setup room generation parameters based on level difficulty.
    /// Currently uses default values but can be expanded for difficulty scaling.
    /// </summary>
    /// <param name="level">Current level for difficulty scaling</param>
    public void Setup(int level)
    {
        /*
        // Room
        minRoomWidth = minRoomW;
        maxRoomWidth = maxRoomW;
        minRoomLength = minRoomL;
        maxRoomLength = maxRoomL;

        // Cells
        subCellsPerLargeCell = subCellsPerLarge;
        largeCellSize = largeCellS;

        // Generation
        cornerRemovalChance = Mathf.Clamp01(cornerChance);
        furniturePlacementChance = Mathf.Clamp01(furnitureChance);
        maxFurniturePercent = Mathf.Clamp01(maxFurniture);

        // Spawners
        minSpawnerCount = minSpawner;
        maxSpawnerCount = maxSpawner;

        // Defence Towers
        minDefenceCount = minDefence;
        maxDefenceCount = maxDefence;
        */

        // TODO: Implement difficulty scaling based on level
        // For now, using default inspector values
        
        // Example of how difficulty scaling could work:
        // float difficultyMultiplier = 1f + (level - 1) * 0.1f;
        // maxRoomWidth = Mathf.Min(maxRoomWidth, (int)(maxRoomWidth * difficultyMultiplier));
        // furniturePlacementChance = Mathf.Clamp01(furniturePlacementChance * difficultyMultiplier);
        
        Debug.Log($"RoomGenerator Setup(): Using default parameters for level {level}");
    }

/// <summary>
/// Main room generation pipeline that creates a complete tower defense room.
/// Executes all generation steps in the correct order.
/// </summary>
    public void GenerateRoom()
    {
        // Step 1: Determine room dimensions
        DetermineRoomDimensions();
        
        // Step 2: Generate floor mesh first
        GenerateFloorMesh();
        
        // Step 3: Process corners (remove cells)
        ProcessCorners();
        
        // Step 4: Set up neighbors and border states
        SetupNeighborsAndBorderStates();
        
        // Step 5: Place walls (on border and removed cells)
        PlaceWalls();
        
        // Step 6: Place gameplay elements (main tower and enemy spawners)
        PlaceGameplayElements();

        // Step 7: Place furniture (on walkable active cells)
        PlaceFurniture();

        // Step 8: Generate enemy paths (from enemy spawners to main tower)
        GenerateEnemyPaths();

        //Step 9: Place Defence Tower Locations
        PlaceDefenceTowerLocations();
    }
    
    void DetermineRoomDimensions()
    {
        roomWidth = Random.Range(minRoomWidth, maxRoomWidth + 1);
        roomLength = Random.Range(minRoomLength, maxRoomLength + 1);
        
        Debug.Log($"RoomGenerator DetermineRoomDimensions(): Generated room: {roomWidth}x{roomLength} large cells");
    }
    
    void GenerateFloorMesh()
    {
        var meshGen = GetComponent<MeshGenerator>() ?? gameObject.AddComponent<MeshGenerator>();
        grid = meshGen.CreateGrid(roomWidth, roomLength, subCellsPerLargeCell);
    }
    
    void SetupNeighborsAndBorderStates()
    {
        // Set up neighbor relationships for all cells
        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                LargeCell currentCell = grid[x, z];
                
                // Set neighbors (null if out of bounds)
                LargeCell north = (z + 1 < roomLength) ? grid[x, z + 1] : null;
                LargeCell south = (z - 1 >= 0) ? grid[x, z - 1] : null;
                LargeCell east = (x + 1 < roomWidth) ? grid[x + 1, z] : null;
                LargeCell west = (x - 1 >= 0) ? grid[x - 1, z] : null;

                // Diagonal neighbors
                LargeCell northEast = (x + 1 < roomWidth && z + 1 < roomLength) ? grid[x + 1, z + 1] : null;
                LargeCell southEast = (x + 1 < roomWidth && z - 1 >= 0) ? grid[x + 1, z - 1] : null;
                LargeCell southWest = (x - 1 >= 0 && z - 1 >= 0) ? grid[x - 1, z - 1] : null;
                LargeCell northWest = (x - 1 >= 0 && z + 1 < roomLength) ? grid[x - 1, z + 1] : null;
                
                currentCell.SetNeighbors(north, south, east, west, northEast, southEast, southWest, northWest);
                
                // Set border state for this cell
                currentCell.SetBorderState();
                
                // Also set sub-cell neighbors and border states (uses 8-direction neighbors across large cells)
                currentCell.SetSubCellNeighborsAndBorderStates();
            }
        }
        
        Debug.Log("RoomGenerator SetupNeighborsAndBorderStates(): Neighbors and border states set up for all cells");
    }
    
// ============================ CORNER METHODS ============================
    void ProcessCorners()
    {
        // Define the four corners of the room
        LargeCell[] corners = { 
            grid[0, 0],                                    // bottom left
            grid[roomWidth - 1, 0],                        // bottom right
            grid[0, roomLength - 1],                       // top left
            grid[roomWidth - 1, roomLength - 1]            // top right
        };
        
        // Process each corner
        foreach (LargeCell corner in corners)
        {
            if (Random.value < cornerRemovalChance)
            {
                int removalSize = Random.Range(1, 5);   // 1=1x1, 2=1x2, 3=2x1, 4=2x2 (skip 0)
                RemoveCornerCells(corner, removalSize);
                Debug.Log($"RoomGenerator ProcessCorners(): Removing corner at ({corner.x}, {corner.y}) with size {removalSize}");
            }
        }
    }
    
    void RemoveCornerCells(LargeCell corner, int removalType)
    {
        int width = 0;
        int height = 0;
        
        switch (removalType)
        {
            case 1: // 1x1
                width = 1; height = 1;
                break;
            case 2: // 1x2
                width = 1; height = 2;
                break;
            case 3: // 2x1
                width = 2; height = 1;
                break;
            case 4: // 2x2
                width = 2; height = 2;
                break;
        }
        
        if (width > 0 && height > 0)
        {
            // Use the corner's grid coordinates as the starting point
            int startX = corner.x;
            int startY = corner.y;
            
            for(int h = 0; h < height; h++)
            {
                for(int w = 0; w < width; w++)
                {
                    int cellX = startX + w;
                    int cellY = startY + h;
                    
                    // Make sure we don't go out of bounds
                    if (cellX < roomWidth && cellY < roomLength)
                    {
                        grid[cellX, cellY].state = CellState.Removed;
                        grid[cellX, cellY].bActive = false;
                        Debug.Log($"RoomGenerator RemoveCornerCells(): Removed cell at ({cellX}, {cellY})");
                    }
                }
            }
        }
    }
    
// ============================ WALL METHODS ============================
    void PlaceWalls()
    {
        foreach (LargeCell cell in grid)
        {
            if(cell.bActive == false || cell.state == CellState.Removed)
            {
                //place wall at large cell world position
                PlaceWallAtPosition(cell.worldPosition);
            }
            else
            {
                switch(cell.borderState)
                {
                    case CellBorderState.None:     //None
                        // No walls needed for interior cells
                        break;
                    case CellBorderState.North:     //North
                        //place wall at north edge of cell
                        PlaceWallAtPosition(cell.worldPosition + Vector3.forward * (largeCellSize));
                        break;
                    case CellBorderState.East:     //East
                        //place wall at east edge of cell
                        PlaceWallAtPosition(cell.worldPosition + Vector3.right * (largeCellSize));
                        break;
                    case CellBorderState.South:     //South
                        //place wall at south edge of cell
                        PlaceWallAtPosition(cell.worldPosition + Vector3.back * (largeCellSize));
                        break;
                    case CellBorderState.West:     //West
                        //place wall at west edge of cell
                        PlaceWallAtPosition(cell.worldPosition + Vector3.left * (largeCellSize));
                        break;
                    case CellBorderState.NECorner: //North-East corner
                        PlaceWallAtPosition(cell.worldPosition + Vector3.forward * (largeCellSize));
                        PlaceWallAtPosition(cell.worldPosition + Vector3.right * (largeCellSize));
                        break;
                    case CellBorderState.SECorner: //South-East corner
                        PlaceWallAtPosition(cell.worldPosition + Vector3.back * (largeCellSize));
                        PlaceWallAtPosition(cell.worldPosition + Vector3.right * (largeCellSize));
                        break;
                    case CellBorderState.SWCorner: //South-West corner
                        PlaceWallAtPosition(cell.worldPosition + Vector3.back * (largeCellSize));
                        PlaceWallAtPosition(cell.worldPosition + Vector3.left * (largeCellSize));
                        break;
                    case CellBorderState.NWCorner: //North-West corner
                        PlaceWallAtPosition(cell.worldPosition + Vector3.forward * (largeCellSize));
                        PlaceWallAtPosition(cell.worldPosition + Vector3.left * (largeCellSize));
                        break;
                }
            }
        }
    }
    
    void PlaceWallAtPosition(Vector3 position)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
        wall.transform.SetParent(transform);
    }
    
// ============================ GAMEPLAY ELEMENT METHODS ============================
    void PlaceGameplayElements()
    {
        LargeCell centerCell = grid[Mathf.RoundToInt(roomWidth/2), Mathf.RoundToInt(roomLength/2)];
        SubCell centerSubCell = centerCell.subCells[Mathf.RoundToInt(subCellsPerLargeCell/2), Mathf.RoundToInt(subCellsPerLargeCell/2)];
        Vector3 centerPos = centerSubCell.worldPosition;
        centerPos.y += 3f;      //POE later: change when custom modle sare imported

        GameObject mainTower = Instantiate(mainTowerPrefab, centerPos, Quaternion.identity);
        mainTower.transform.SetParent(transform);
        centerCell.state = CellState.MainTower;
        centerSubCell.state = CellState.MainTower;
        mainTowerCell = centerSubCell;
        
        // Place enemy spawners at edges
        PlaceEnemySpawners();
    }
    
    void PlaceEnemySpawners()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();

        spawnerCount = Random.Range(minSpawnerCount, maxSpawnerCount + 1); // Random number between min and max (inclusive)
        enemySpawnerCells = new SubCell[spawnerCount];
        List<SubCell> validSubCells = GetValidSpawnerSubCells();
         
        for (int i = 0; i < spawnerCount && validSubCells.Count > 0; i++)
        {
            // Pick a random valid position
            int index = Random.Range(0, validSubCells.Count);
            
            Vector3 worldPos = validSubCells[index].worldPosition + Vector3.up * 0.5f;
            GameObject spawner = Instantiate(enemySpawnerPrefab, worldPos, Quaternion.identity);
            spawner.transform.SetParent(transform);
            spawner.GetComponent<SpawnerManager>().Setup(validSubCells[index]);

            validSubCells[index].state = CellState.EnemySpawner;
            validSubCells[index].parentCell.state = CellState.EnemySpawner;
            enemySpawnerCells[i] = validSubCells[index];
            validSubCells.RemoveAt(index); // Remove to avoid duplicates

            if (gameManager != null)
            {
                gameManager.spawnerManagers.Add(spawner.GetComponent<SpawnerManager>());
            }
        }
         
        Debug.Log($"RoomGenerator PlaceEnemySpawners(): Successfully placed {spawnerCount} spawners");
    }

    List<SubCell> GetValidSpawnerSubCells()
    {
        List<SubCell> validSubCells = new List<SubCell>();
        
        // Check all large cells for border cells
        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                LargeCell cell = grid[x, z];
                
                // Skip invalid tiles (removed corners)
                if (cell.state == CellState.Removed) continue;
                
                // Check if this large cell is on the border
                if (cell.borderState != CellBorderState.None)
                {
                    // Add all sub-cells of this border cell as valid spawner positions
                    for (int sx = 0; sx < subCellsPerLargeCell; sx++)
                    {
                        for (int sz = 0; sz < subCellsPerLargeCell; sz++)
                        {
                            SubCell subCell = cell.subCells[sx, sz];
                            if (subCell != null && subCell.borderState != CellBorderState.None)
                            {
                                validSubCells.Add(subCell);
                            }
                        }
                    }
                }
            }
        }
        
        return validSubCells;
    }  
    
    void PlaceDefenceTowerLocations()
    {
		var paths = pathGenerator != null ? pathGenerator.GetAllPathObjects() : null;
		if (paths == null || paths.Count == 0)
		{
			// Fallback to previous behavior if no paths are available
			int fallbackCount = Random.Range(minDefenceCount, maxDefenceCount + 1);
			defenceTowerCells = new SubCell[fallbackCount];
			List<SubCell> valid = GetValidLocationSubCells();
			for (int i = 0; i < fallbackCount && valid.Count > 0; i++)
			{
				int idx = Random.Range(0, valid.Count);
				SpawnDefenceLocationAt(valid[idx]);
				defenceTowerCells[i] = valid[idx];
				valid.RemoveAt(idx);
			}
			Debug.Log($"RoomGenerator PlaceDefenceTowerLocations(): Placed {fallbackCount} locations (fallback, no paths).");
			return;
		}

		// Distribute total locations roughly evenly across paths, but with randomness
		int totalDesired = Random.Range(minDefenceCount, maxDefenceCount + 1);
		int numPaths = paths.Count;
		int perPathMin = Mathf.Max(0, minDefenceCount / Mathf.Max(1, numPaths));
		int perPathMax = Mathf.Max(perPathMin, Mathf.CeilToInt((float)maxDefenceCount / Mathf.Max(1, numPaths)));

		List<SubCell> placed = new List<SubCell>();
		int totalPlaced = 0;

		// First pass: attempt per-path random within [perPathMin, perPathMax]
		for (int p = 0; p < numPaths; p++)
		{
			int targetForPath = Random.Range(perPathMin, perPathMax + 1);
			// Make sure we don't exceed totalDesired too early
			if (totalPlaced + targetForPath > totalDesired)
			{
				targetForPath = Mathf.Max(0, totalDesired - totalPlaced);
			}
			if (targetForPath == 0) continue;

			List<SubCell> candidates = GetValidLocationSubCellsForPath(paths[p]);
			// Remove any already used cells
			if (candidates != null && candidates.Count > 0)
				candidates.RemoveAll(sc => placed.Contains(sc));

			int placedForPath = 0;
			while (candidates != null && candidates.Count > 0 && placedForPath < targetForPath)
			{
				int idx = Random.Range(0, candidates.Count);
				SubCell choice = candidates[idx];
				SpawnDefenceLocationAt(choice);
				placed.Add(choice);
				placedForPath++;
				totalPlaced++;
				candidates.RemoveAt(idx);
			}
			if (totalPlaced >= totalDesired) break;
		}

		// If we still have remaining quota, fill from any path candidates
		if (totalPlaced < totalDesired)
		{
			List<SubCell> anyCandidates = new List<SubCell>();
			for (int p = 0; p < numPaths; p++)
			{
				var list = GetValidLocationSubCellsForPath(paths[p]);
				if (list != null) anyCandidates.AddRange(list);
			}
			anyCandidates.RemoveAll(sc => placed.Contains(sc));
			while (totalPlaced < totalDesired && anyCandidates.Count > 0)
			{
				int idx = Random.Range(0, anyCandidates.Count);
				SubCell sc = anyCandidates[idx];
				SpawnDefenceLocationAt(sc);
				placed.Add(sc);
				totalPlaced++;
				anyCandidates.RemoveAt(idx);
			}
		}

		// Save results
		defenceTowerCells = placed.ToArray();
		Debug.Log($"RoomGenerator PlaceDefenceTowerLocations(): Placed {totalPlaced} locations across {numPaths} paths.");
    }

    List<SubCell> GetValidLocationSubCells()
    {
        List<SubCell> validSubCells = new List<SubCell>();

        // Build a distance map from all EnemyPath sub-cells using multi-source BFS
        Dictionary<SubCell, int> distanceFromPath = ComputeSubCellDistancesFromPaths();

        // Any floor subcell with distance in [1, maxLocationDistance] is valid
        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                LargeCell cell = grid[x, z];
                if (cell == null || cell.state == CellState.Removed) continue;

                for (int sx = 0; sx < subCellsPerLargeCell; sx++)
                {
                    for (int sz = 0; sz < subCellsPerLargeCell; sz++)
                    {
                        SubCell subCell = cell.subCells[sx, sz];
                        if (subCell == null) continue;
                        if (subCell.state != CellState.Floor) continue; // only place on floor

                        int dist;
                        if (distanceFromPath != null && distanceFromPath.TryGetValue(subCell, out dist))
                        {
                            if (dist >= 1 && dist <= maxLocationDistance)
                            {
                                validSubCells.Add(subCell);
                            }
                        }
                    }
                }
            }
        }

        return validSubCells;
    } 

	List<SubCell> GetValidLocationSubCellsForPath(PathObj pathObj)
	{
		List<SubCell> valid = new List<SubCell>();
		if (pathObj.pathCells == null || pathObj.pathCells.Count == 0) return valid;

		Dictionary<SubCell, int> distance = ComputeSubCellDistancesFromSeeds(pathObj.pathCells);

		for (int x = 0; x < roomWidth; x++)
		{
			for (int z = 0; z < roomLength; z++)
			{
				LargeCell cell = grid[x, z];
				if (cell == null || cell.state == CellState.Removed) continue;

				for (int sx = 0; sx < subCellsPerLargeCell; sx++)
				{
					for (int sz = 0; sz < subCellsPerLargeCell; sz++)
					{
						SubCell subCell = cell.subCells[sx, sz];
						if (subCell == null) continue;
						if (subCell.state != CellState.Floor) continue; // only place on floor

						int dist;
						if (distance != null && distance.TryGetValue(subCell, out dist))
						{
							if (dist >= 1 && dist <= maxLocationDistance)
							{
								valid.Add(subCell);
							}
						}
					}
				}
			}
		}

		return valid;
	}

    // Compute minimum subcell-distance (8-directional) from all EnemyPath subcells up to maxLocationDistance
    Dictionary<SubCell, int> ComputeSubCellDistancesFromPaths()
    {
        Queue<SubCell> queue = new Queue<SubCell>();
        Dictionary<SubCell, int> distances = new Dictionary<SubCell, int>();

        // Seed the queue with all EnemyPath subcells at distance 0
        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                LargeCell cell = grid[x, z];
                if (cell == null || cell.subCells == null) continue;

                for (int sx = 0; sx < subCellsPerLargeCell; sx++)
                {
                    for (int sz = 0; sz < subCellsPerLargeCell; sz++)
                    {
                        SubCell sc = cell.subCells[sx, sz];
                        if (sc != null && sc.state == CellState.EnemyPath)
                        {
                            if (!distances.ContainsKey(sc))
                            {
                                distances[sc] = 0;
                                queue.Enqueue(sc);
                            }
                        }
                    }
                }
            }
        }

        // If no paths exist, nothing is valid
        if (queue.Count == 0)
        {
            return distances;
        }

        // BFS with cutoff at maxLocationDistance to fill distances
        while (queue.Count > 0)
        {
            SubCell current = queue.Dequeue();
            int currentDist = distances[current];
            if (currentDist >= maxLocationDistance) continue; // don't expand further

            SubCell[] neighbors = GetSubCellNeighbors(current);
            for (int i = 0; i < neighbors.Length; i++)
            {
                SubCell n = neighbors[i];
                if (n == null) continue;
                if (n.state == CellState.Removed || n.state == CellState.Wall) continue;
                if (distances.ContainsKey(n)) continue;

                distances[n] = currentDist + 1;
                queue.Enqueue(n);
            }
        }

        return distances;
    }

    // Compute distance map from a provided list of seed subcells (e.g., for a single path)
    Dictionary<SubCell, int> ComputeSubCellDistancesFromSeeds(List<SubCell> seeds)
    {
        Queue<SubCell> queue = new Queue<SubCell>();
        Dictionary<SubCell, int> distances = new Dictionary<SubCell, int>();
        if (seeds == null || seeds.Count == 0) return distances;

        // Seed
        for (int i = 0; i < seeds.Count; i++)
        {
            SubCell sc = seeds[i];
            if (sc == null) continue;
            if (!distances.ContainsKey(sc))
            {
                distances[sc] = 0;
                queue.Enqueue(sc);
            }
        }

        // BFS limited by maxLocationDistance
        while (queue.Count > 0)
        {
            SubCell current = queue.Dequeue();
            int currentDist = distances[current];
            if (currentDist >= maxLocationDistance) continue;

            SubCell[] neighbors = GetSubCellNeighbors(current);
            for (int i = 0; i < neighbors.Length; i++)
            {
                SubCell n = neighbors[i];
                if (n == null) continue;
                if (n.state == CellState.Removed || n.state == CellState.Wall) continue;
                if (distances.ContainsKey(n)) continue;

                distances[n] = currentDist + 1;
                queue.Enqueue(n);
            }
        }

        return distances;
    }

    // 8-directional neighbors for subcells
    SubCell[] GetSubCellNeighbors(SubCell cell)
    {
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

// ============================ ENEMY PATH METHODS ============================
    void GenerateEnemyPaths()
    {
        // Initialize path generator
        pathGenerator = GetComponent<PathGenerator>() ?? gameObject.AddComponent<PathGenerator>();
        pathGenerator.Initialize(grid, roomWidth, roomLength, subCellsPerLargeCell);
        
        // Generate paths from all spawners to the main tower using stored references
        pathGenerator.GenerateEnemyPaths(mainTowerCell, enemySpawnerCells);
        
        // Ensure all existing towers (including main tower) are registered to paths immediately
        TryRegisterExistingTowersToPaths();

        Debug.Log("RoomGenerator GenerateEnemyPaths(): Enemy paths generated successfully");
    }

    void TryRegisterExistingTowersToPaths()
    {
        if (pathGenerator == null) return;

        // Register the main tower (tagged "MainTower") if present
        GameObject mainTowerObj = GameObject.FindGameObjectWithTag("MainTower");
        if (mainTowerObj != null)
        {
            TowerManager tm = mainTowerObj.GetComponent<TowerManager>();
            if (tm != null)
            {
                pathGenerator.RegisterTowerOverlaps(tm);
            }
        }

        // Register any defence towers already placed in the scene
        var defenceTowers = GameObject.FindGameObjectsWithTag("DefenceTower");
        if (defenceTowers != null)
        {
            for (int i = 0; i < defenceTowers.Length; i++)
            {
                var go = defenceTowers[i];
                if (go == null) continue;
                TowerManager tm = go.GetComponent<TowerManager>();
                if (tm == null) continue;
                pathGenerator.RegisterTowerOverlaps(tm);
            }
        }
    }

// ============================ FURNITURE METHODS ============================
    void PlaceFurniture()
    {
        int maxFurnitureAmount = Mathf.RoundToInt(maxFurniturePercent * (roomLength * roomWidth));
        Debug.Log("RoomGenerator PlaceFurniture(): Max furniture is " + maxFurnitureAmount);
        int furnitureAmount = 0;
        // Place furniture, based on random value, on walkable active cells 
        for (int x = 1; x < roomWidth; x++)
        {
            for (int y = 1; y < roomLength; y++)
            {
                LargeCell cell = grid[x, y];
				if (cell != null
					&& cell.IsWalkable()
					&& !cell.IsRemoved()
					&& cell.state != CellState.MainTower
					&& cell.state != CellState.EnemySpawner
					&& cell.state != CellState.EnemyPath
					&& !CellAlreadyHasFurniture(cell))
                {
                    if (Random.value < furniturePlacementChance && furnitureAmount < maxFurnitureAmount)
                    {
                        // Pick a random furniture prefab that has a FurnitureObj component
                        GameObject selectedPrefab = SelectRandomFurniturePrefab();
                        if (selectedPrefab == null)
                        {
                            Debug.LogWarning("RoomGenerator PlaceFurniture(): No valid furniture prefabs assigned. Skipping.");
                            continue;
                        }
                        FurnitureObj furnitureObj = selectedPrefab.GetComponent<FurnitureObj>();
                        if (furnitureObj == null)
                        {
                            Debug.LogWarning($"RoomGenerator PlaceFurniture(): Selected prefab {selectedPrefab.name} has no FurnitureObj. Skipping.");
                            continue;
                        }
                        grid = furnitureObj.SetGridState(grid, cell);
                        Vector3 furniturePos = cell.worldPosition + furnitureObj.spawnOffset;
                        GameObject furniture = Instantiate(selectedPrefab, furniturePos, Quaternion.identity);
                        furniture.transform.SetParent(transform);
                        furnitureAmount++;
                    }
                }
            }
        }
		if (enableFurnitureDebugPlacement) DebugPlaceFurnitureSubCells();
    }

    /// <summary>
    /// Debug method that places furniture prefabs on all sub-cells that have been marked with CellState.Furniture.
    /// This method is called after the main furniture placement logic to ensure all furniture-marked sub-cells
    /// have corresponding visual furniture objects in the scene.
    /// </summary>
    void DebugPlaceFurnitureSubCells()
    {
		// Check if at least one furniture prefab is assigned to avoid null reference errors
		if ((furniturePrefabs == null || furniturePrefabs.Length == 0) && furniturePrefab == null)
        {
            Debug.LogWarning("RoomGenerator DebugPlaceFurnitureSubCells(): No furniture prefabs assigned.");
            return;
        }

        // Iterate through all large cells in the grid
        for (int x = 0; x < roomWidth; x++)
        {
            for (int z = 0; z < roomLength; z++)
            {
                LargeCell cell = grid[x, z];
                
                // Skip cells that are null or don't have sub-cells initialized
                if (cell == null || cell.subCells == null)
                {
                    continue;
                }

                // Iterate through all sub-cells within the current large cell
                for (int sx = 0; sx < subCellsPerLargeCell; sx++)
                {
                    for (int sz = 0; sz < subCellsPerLargeCell; sz++)
                    {
                        SubCell subCell = cell.subCells[sx, sz];
                        
                        // Check if sub-cell exists and has been marked for furniture placement
                        if (subCell != null && subCell.state == CellState.Furniture)
                        {
                            // Get the world position of the sub-cell
                            Vector3 worldPos = subCell.worldPosition;
                            
                            // Instantiate a random furniture prefab at the sub-cell's position (debug visualization)
                            GameObject debugPrefab = SelectRandomFurniturePrefab() ?? furniturePrefab;
                            if (debugPrefab == null) continue;
                            GameObject instance = Instantiate(debugPrefab, worldPos, Quaternion.identity);
                            
                            // Parent the furniture to this room generator for organization
                            instance.transform.SetParent(transform);
                        }
                    }
                }
            }
        }
    }

    GameObject SelectRandomFurniturePrefab()
    {
		if (furniturePrefabs == null || furniturePrefabs.Length == 0) return null;

		// Build weighted list based on FurnitureObj.likelihood
		float totalWeight = 0f;
		for (int i = 0; i < furniturePrefabs.Length; i++)
		{
			var go = furniturePrefabs[i];
			if (go == null) continue;
			var fo = go.GetComponent<FurnitureObj>();
			if (fo == null) continue;
			float w = Mathf.Clamp(fo.likelihood, 0.1f, 1f);
			totalWeight += w;
		}
		if (totalWeight <= 0f)
		{
			// Fallback: first valid in array
			for (int i = 0; i < furniturePrefabs.Length; i++)
			{
				var cand = furniturePrefabs[i];
				if (cand != null && cand.GetComponent<FurnitureObj>() != null) return cand;
			}
			return null;
		}

		float roll = Random.Range(0f, totalWeight);
		float cumulative = 0f;
		for (int i = 0; i < furniturePrefabs.Length; i++)
		{
			var go = furniturePrefabs[i];
			if (go == null) continue;
			var fo = go.GetComponent<FurnitureObj>();
			if (fo == null) continue;
			float w = Mathf.Clamp(fo.likelihood, 0.1f, 1f);
			cumulative += w;
			if (roll <= cumulative)
			{
				return go;
			}
		}
		// Safety fallback
		for (int i = 0; i < furniturePrefabs.Length; i++)
		{
			var cand = furniturePrefabs[i];
			if (cand != null && cand.GetComponent<FurnitureObj>() != null) return cand;
		}
		return null;
    }

	bool CellAlreadyHasFurniture(LargeCell cell)
	{
		if (cell == null) return false;
		// If the large cell state is already Furniture, treat as occupied
		if (cell.state == CellState.Furniture) return true;
		// Otherwise scan subcells for any furniture markers
		if (cell.subCells == null) return false;
		int width = cell.subCells.GetLength(0);
		int length = cell.subCells.GetLength(1);
		for (int sx = 0; sx < width; sx++)
		{
			for (int sz = 0; sz < length; sz++)
			{
				var sc = cell.subCells[sx, sz];
				if (sc != null && sc.state == CellState.Furniture) return true;
			}
		}
		return false;
	}

	void SpawnDefenceLocationAt(SubCell subCell)
	{
		if (subCell == null || defenseTowerLocationPrefab == null) return;
		Vector3 worldPos = subCell.worldPosition + Vector3.up * 0.1f;
		GameObject defenceTower = Instantiate(defenseTowerLocationPrefab, worldPos, Quaternion.identity);
		defenceTower.transform.SetParent(transform);
		subCell.state = CellState.DefenseTower;
		// Only mark parent cell if it was floor to avoid overwriting special states
		if (subCell.parentCell != null && subCell.parentCell.state == CellState.Floor)
		{
			subCell.parentCell.state = CellState.DefenseTower;
		}
	}
}

/* ============================ UNUSED/OLD METHODS ============================

    void InitializeGridSystem()
    {
        gridSystem = GetComponent<GridSystem>() ?? gameObject.AddComponent<GridSystem>();
        gridSystem.Initialize(roomWidth, roomLength, largeCellSize, subCellSize, subCellsPerLargeCell);
        
        // Initialize cell manager
        cellManager = GetComponent<CellManager>() ?? gameObject.AddComponent<CellManager>();
        cellManager.gridWidth = roomWidth;
        cellManager.gridHeight = roomLength;
        cellManager.cellSize = largeCellSize;
        cellManager.subCellsPerLargeCell = subCellsPerLargeCell;
        cellManager.InitializeGrid();
    }

    void PlaceWallsAndDoors()
    {
        // Use cell manager to get cells that need walls
        List<LargeCell> cellsNeedingWalls = cellManager.GetCellsNeedingWalls();
        
        // Place walls based on cell neighbor analysis
        foreach (LargeCell cell in cellsNeedingWalls)
        {
            WallDirection wallDirections = cell.GetWallDirections();
            PlaceWallsForCell(cell, wallDirections);
        }
        
        // Place doors on perimeter walls
        PlaceDoorsOnPerimeter();
    }
    
    void PlaceWallsForCell(LargeCell cell, WallDirection wallDirections)
    {
        Vector3 cellPos = cell.worldPosition;
        
        // Place walls based on directions needed
        if ((wallDirections & WallDirection.North) != 0)
        {
            PlaceWallAtPosition(cellPos + Vector3.forward * (largeCellSize / 2f));
        }
        if ((wallDirections & WallDirection.South) != 0)
        {
            PlaceWallAtPosition(cellPos + Vector3.back * (largeCellSize / 2f));
        }
        if ((wallDirections & WallDirection.East) != 0)
        {
            PlaceWallAtPosition(cellPos + Vector3.right * (largeCellSize / 2f));
        }
        if ((wallDirections & WallDirection.West) != 0)
        {
            PlaceWallAtPosition(cellPos + Vector3.left * (largeCellSize / 2f));
        }
    }

    void PlaceDoorsOnPerimeter()
    {
        // Get all cells that are on the perimeter and have walls
        List<LargeCell> perimeterCells = new List<LargeCell>();
        
        for (int x = 0; x < roomWidth; x++)
        {
            for (int y = 0; y < roomLength; y++)
            {
                LargeCell cell = cellManager.GetLargeCell(x, y);
                if (cell != null && cell.NeedsWall() && cell.IsWalkable())
                {
                    // Check if this is a perimeter cell
                    if (x == 0 || x == roomWidth - 1 || y == 0 || y == roomLength - 1)
                    {
                        perimeterCells.Add(cell);
                    }
                }
            }
        }
        
        // Place exactly 2 doors on different walls
        if (perimeterCells.Count >= 2)
        {
            // Shuffle and take first 2
            for (int i = perimeterCells.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = perimeterCells[i];
                perimeterCells[i] = perimeterCells[j];
                perimeterCells[j] = temp;
            }
            
            // Place doors
            PlaceDoorForCell(perimeterCells[0]);
            PlaceDoorForCell(perimeterCells[1]);
        }
    }
    
    void PlaceDoorForCell(LargeCell cell)
    {
        WallDirection wallDirections = cell.GetWallDirections();
        Vector3 cellPos = cell.worldPosition;
        
        // Choose a random wall direction for the door
        List<WallDirection> availableWalls = new List<WallDirection>();
        if ((wallDirections & WallDirection.North) != 0) availableWalls.Add(WallDirection.North);
        if ((wallDirections & WallDirection.South) != 0) availableWalls.Add(WallDirection.South);
        if ((wallDirections & WallDirection.East) != 0) availableWalls.Add(WallDirection.East);
        if ((wallDirections & WallDirection.West) != 0) availableWalls.Add(WallDirection.West);
        
        if (availableWalls.Count > 0)
        {
            WallDirection doorDirection = availableWalls[Random.Range(0, availableWalls.Count)];
            Vector3 doorPosition = cellPos;
            
            switch (doorDirection)
            {
                case WallDirection.North:
                    doorPosition += Vector3.forward * (largeCellSize / 2f);
                    break;
                case WallDirection.South:
                    doorPosition += Vector3.back * (largeCellSize / 2f);
                    break;
                case WallDirection.East:
                    doorPosition += Vector3.right * (largeCellSize / 2f);
                    break;
                case WallDirection.West:
                    doorPosition += Vector3.left * (largeCellSize / 2f);
                    break;
            }
            
            // Remove any existing wall at this position
            RemoveWallAtPosition(doorPosition);
            
            // Place door
            GameObject door = Instantiate(doorPrefab, doorPosition, Quaternion.identity);
            door.transform.SetParent(transform);
        }
    }
    
    void RemoveWallAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Wall"))
            {
                Destroy(col.gameObject);
                break;
            }
        }
    }

     bool IsSubCellInRemovedLargeCell(int subX, int subZ)
    {
        // Convert sub-cell coordinates to large cell coordinates
        int largeX = subX / subCellsPerLargeCell;
        int largeZ = subZ / subCellsPerLargeCell;
        
        // Check if the large cell containing this sub-cell is removed
        return gridSystem.IsLargeCellRemoved(largeX, largeZ);
    }

    bool IsAdjacentToWall(int subX, int subZ)
    {
        // Check if this sub-cell is adjacent to a wall
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = new Vector2Int(subX, subZ) + dir;
            if (gridSystem.GetSubCellType(neighbor.x, neighbor.y) == GridSystem.CellType.Wall)
            {
                return true;
            }
        }
        
        return false;
    }


*/
