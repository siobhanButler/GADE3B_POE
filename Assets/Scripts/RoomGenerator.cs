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
    public float largeCellSize = 9f; // Size of each large cell
    public float subCellSize = 1f; // Size of each sub-cell (1x1)
    public int subCellsPerLargeCell = 9; // 9x9 sub-grid per large cell
    
     [Header("Generation Settings")]
     [Range(0f, 1f)]
     public float cornerRemovalChance = 0.3f; // Chance to remove corners
     [Range(0f, 1f)]
     public float furniturePlacementChance = 0.4f;
     
     [Header("Spawner Settings")]
     public int minSpawnerCount = 2;
     public int maxSpawnerCount = 5;
    
    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject furniturePrefab;
    public GameObject mainTowerPrefab;
    public GameObject enemySpawnerPrefab;
    public GameObject defenseTowerPrefab;
    
    // Private fields
    private GridSystem gridSystem;
    private CellManager cellManager;
    private int roomWidth;
    private int roomLength; 
    
    void Start()
    {
        GenerateRoom();
    }
    
    public void GenerateRoom()
    {
        // Step 1: Determine room dimensions
        DetermineRoomDimensions();
        // Step 2: Initialize grid system
        InitializeGridSystem();
        // Step 3: Process corners (remove cells) - MUST happen before mesh generation
        ProcessCorners();
        // Step 4: Generate floor mesh - Now happens after corners are processed
        GenerateFloorMesh();
        // Step 5: Place walls and doors
        PlaceWallsAndDoors();
        // Step 6: Place furniture
        PlaceFurniture();
        // Step 7: Place gameplay elements
        PlaceGameplayElements();
        // Step 8: Generate enemy paths
        //GenerateEnemyPaths();
    }
    
    void DetermineRoomDimensions()
    {
        roomWidth = Random.Range(minRoomWidth, maxRoomWidth + 1);
        roomLength = Random.Range(minRoomLength, maxRoomLength + 1);
        
        Debug.Log($"Generated room: {roomWidth}x{roomLength} large cells");
    }
    
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
    
    void GenerateFloorMesh()
    {
        var meshGen = GetComponent<MeshGenerator>() ?? gameObject.AddComponent<MeshGenerator>();
        meshGen.xSize = roomWidth * subCellsPerLargeCell;
        meshGen.zSize = roomLength * subCellsPerLargeCell;
        
        meshGen.CreateShape();
        meshGen.UpdateMesh();
    }
    
    void ProcessCorners()
    {
        // Process each corner of the room
        Vector2[] corners = {
            new Vector2(0, 0), // Bottom-left
            new Vector2(roomWidth - 1, 0), // Bottom-right
            new Vector2(0, roomLength - 1), // Top-left
            new Vector2(roomWidth - 1, roomLength - 1) // Top-right
        };
        
        //Randomize if corner should be removed or not
        foreach (Vector2 corner in corners)
        {
            if (Random.value < cornerRemovalChance)
            {
                int removalSize = Random.Range(0, 5); // 0=none, 1=1x1, 2=1x2, 3=2x1, 4=2x2
                RemoveCornerCells(new Vector2Int((int)corner.x, (int)corner.y), removalSize);
            }
        }
    }
    
    void RemoveCornerCells(Vector2Int corner, int removalType)
    {
        int width = 0, height = 0;
        
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
            List<Vector2Int> cellsToRemove = new List<Vector2Int>();
            
            // Collect cells to remove
            for (int x = corner.x; x < corner.x + width && x < roomWidth; x++)
            {
                for (int z = corner.y; z < corner.y + height && z < roomLength; z++)
                {
                    cellsToRemove.Add(new Vector2Int(x, z));
                    gridSystem.SetLargeCellRemoved(x, z);
                }
            }
            
            // Remove cells using cell manager
            cellManager.RemoveCells(cellsToRemove);
        }
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
    
    void PlaceWallAtPosition(Vector3 position)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
        wall.transform.SetParent(transform);
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
    
    
    void PlaceFurniture()
    {
        // Place furniture in interior cells using cell manager
        for (int x = 1; x < roomWidth - 1; x++)
        {
            for (int y = 1; y < roomLength - 1; y++)
            {
                LargeCell cell = cellManager.GetLargeCell(x, y);
                if (cell != null && cell.IsWalkable() && !cell.IsRemoved())
                {
                    if (Random.value < furniturePlacementChance)
                    {
                        cell.state = CellState.Furniture;
                        GameObject furniture = Instantiate(furniturePrefab, cell.worldPosition, Quaternion.identity);
                        furniture.transform.SetParent(transform);
                    }
                }
            }
        }
    }
    
    void PlaceGameplayElements()
    {
        // Place main tower at center
        Vector3 centerPos = gridSystem.GetSubCellWorldPosition(
            roomWidth * subCellsPerLargeCell / 2,
            roomLength * subCellsPerLargeCell / 2
        );
        GameObject mainTower = Instantiate(mainTowerPrefab, centerPos, Quaternion.identity);
        mainTower.transform.SetParent(transform);
        
        // Place enemy spawners at edges
        PlaceEnemySpawners();
    }
    
         void PlaceEnemySpawners()
     {
         int spawnerCount = Random.Range(minSpawnerCount, maxSpawnerCount + 1); // Random number between min and max (inclusive)
         List<Vector2Int> validSpawnerPositions = GetValidSpawnerPositions();
         
         Debug.Log($"Attempting to place {spawnerCount} spawners from {validSpawnerPositions.Count} valid positions");
         
         for (int i = 0; i < spawnerCount && validSpawnerPositions.Count > 0; i++)
         {
             // Pick a random valid position
             int index = Random.Range(0, validSpawnerPositions.Count);
             Vector2Int spawnerPos = validSpawnerPositions[index];
             validSpawnerPositions.RemoveAt(index); // Remove to avoid duplicates
             
             Vector3 worldPos = gridSystem.GetSubCellWorldPosition(spawnerPos.x, spawnerPos.y);
             GameObject spawner = Instantiate(enemySpawnerPrefab, worldPos, Quaternion.identity);
             spawner.transform.SetParent(transform);
         }
         
         Debug.Log($"Successfully placed {spawnerCount - validSpawnerPositions.Count} spawners");
     }
    
    List<Vector2Int> GetValidSpawnerPositions()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        // Check all sub-cells for adjacency to walls
        for (int x = 0; x < roomWidth * subCellsPerLargeCell; x++)
        {
            for (int z = 0; z < roomLength * subCellsPerLargeCell; z++)
            {
                // Skip invalid tiles (removed corners)
                if (IsSubCellInRemovedLargeCell(x, z)) continue;
                
                if (IsAdjacentToWall(x, z))
                {
                    validPositions.Add(new Vector2Int(x, z));
                }
            }
        }
        
        return validPositions;
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
    
    void GenerateEnemyPaths()
    {
        // This will be implemented in Pathfinding.cs
        // For now, just log that paths need to be generated
        Debug.Log("Enemy paths need to be generated");
    }
}

