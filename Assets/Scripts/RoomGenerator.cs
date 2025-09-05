using UnityEngine;
using System.Collections.Generic;

public class RoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public int minRoomWidth = 5;
    public int maxRoomWidth = 12;
    public int minRoomLength = 5;
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
    
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject furniturePrefab;
    public GameObject mainTowerPrefab;
    public GameObject enemySpawnerPrefab;
    public GameObject defenseTowerPrefab;
    
    // Private fields
    private GridSystem gridSystem;
    private CellManager cellManager;
    private PathGenerator pathGenerator;
    private int roomWidth;      //x
    private int roomLength;     //z
    private LargeCell[,] grid;  //grid[x,z]

    SubCell mainTowerCell;
    SubCell[] enemySpawnerCells;
    
    void Start()
    {
        largeCellSize = subCellsPerLargeCell * subCellSize;
        GenerateRoom();
    }
    
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
        
        // Step 7: Place gameplay elements (main tower and enemy spawners)
        PlaceGameplayElements();

        // Step 8: Generate enemy paths (from enemy spawners to main tower)
        GenerateEnemyPaths();

        // Step 6: Place furniture (on walkable active cells)
        PlaceFurniture();
    }
    
    void DetermineRoomDimensions()
    {
        roomWidth = Random.Range(minRoomWidth, maxRoomWidth + 1);
        roomLength = Random.Range(minRoomLength, maxRoomLength + 1);
        
        Debug.Log($"Generated room: {roomWidth}x{roomLength} large cells");
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
        
        Debug.Log("Neighbors and border states set up for all cells");
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
                Debug.Log($"Removing corner at ({corner.x}, {corner.y}) with size {removalSize}");
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
                        Debug.Log($"Removed cell at ({cellX}, {cellY})");
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
        int spawnerCount = Random.Range(minSpawnerCount, maxSpawnerCount + 1); // Random number between min and max (inclusive)
        enemySpawnerCells = new SubCell[spawnerCount];
        List<SubCell> validSubCells = GetValidSpawnerSubCells();
         
        Debug.Log($"Attempting to place {spawnerCount} spawners from {validSubCells.Count} valid positions");
         
        for (int i = 0; i < spawnerCount && validSubCells.Count > 0; i++)
        {
            // Pick a random valid position
            int index = Random.Range(0, validSubCells.Count);
            
            Vector3 worldPos = validSubCells[index].worldPosition;
            GameObject spawner = Instantiate(enemySpawnerPrefab, worldPos, Quaternion.identity);
            spawner.transform.SetParent(transform);

            validSubCells[index].state = CellState.EnemySpawner;
            validSubCells[index].parentCell.state = CellState.EnemySpawner;
            enemySpawnerCells[i] = validSubCells[index];
            validSubCells.RemoveAt(index); // Remove to avoid duplicates
        }
         
        Debug.Log($"Successfully placed {spawnerCount - validSubCells.Count} spawners");
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
    
    // ============================ FURNITURE METHODS ============================
    void PlaceFurniture()
    {
        int maxFurnitureAmount = Mathf.RoundToInt(maxFurniturePercent * (roomLength * roomWidth));
        Debug.Log("Max furniture is " + maxFurnitureAmount);
        int furnitureAmount = 0;
        // Place furniture, based on random value, on walkable active cells 
        for (int x = 1; x < roomWidth; x++)
        {
            for (int y = 1; y < roomLength; y++)
            {
                LargeCell cell = grid[x, y];
                if (cell != null && cell.IsWalkable() && !cell.IsRemoved() && cell.state != CellState.MainTower && cell.state != CellState.EnemySpawner && cell.state != CellState.EnemyPath)
                {
                    if (Random.value < furniturePlacementChance && furnitureAmount < maxFurnitureAmount)
                    {
                        cell.state = CellState.Furniture;
                        GameObject furniture = Instantiate(furniturePrefab, cell.worldPosition, Quaternion.identity);
                        furniture.transform.SetParent(transform);
                        furnitureAmount++;
                    }
                }
            }
        }
    }

    // ============================ ENEMY PATH METHODS ============================
    void GenerateEnemyPaths()
    {
        // Initialize path generator
        pathGenerator = GetComponent<PathGenerator>() ?? gameObject.AddComponent<PathGenerator>();
        pathGenerator.Initialize(grid, roomWidth, roomLength, subCellsPerLargeCell);
        
        // Generate paths from all spawners to the main tower using stored references
        pathGenerator.GenerateEnemyPaths(mainTowerCell, enemySpawnerCells);
        
        Debug.Log("Enemy paths generated successfully");
    }

//----------------------------------------------------------------------------------------------------------------------------
//                                           METHODS NOT IN USE
/*

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

---------------------------------------------------------------------------------------------------------------------------------------
    */
}

