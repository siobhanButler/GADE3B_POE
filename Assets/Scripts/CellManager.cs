using UnityEngine;
using System.Collections.Generic;

public class CellManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth;
    public int gridHeight;
    public float cellSize;
    public int subCellsPerLargeCell;
    
    // Grid data
    private LargeCell[,] largeCells;
    private GridSystem gridSystem;
    
    void Awake()
    {
        gridSystem = GetComponent<GridSystem>();
    }
    
    public void InitializeGrid()
    {
        // Initialize the large cell grid
        largeCells = new LargeCell[gridWidth, gridHeight];
        
        // Create all large cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = new Vector3(
                    x * cellSize + cellSize / 2f,
                    0f,
                    y * cellSize + cellSize / 2f
                );
                
                largeCells[x, y] = new LargeCell(x, y, worldPos, subCellsPerLargeCell);
            }
        }
        
        // Set up neighbor relationships
        SetupNeighbors();
        
        Debug.Log($"CellManager initialized with {gridWidth}x{gridHeight} large cells");
    }
    
    void SetupNeighbors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell currentCell = largeCells[x, y];
                
                // Get neighbors (null if at edge)
                LargeCell north = (y < gridHeight - 1) ? largeCells[x, y + 1] : null;
                LargeCell south = (y > 0) ? largeCells[x, y - 1] : null;
                LargeCell east = (x < gridWidth - 1) ? largeCells[x + 1, y] : null;
                LargeCell west = (x > 0) ? largeCells[x - 1, y] : null;
                
                currentCell.SetNeighbors(north, south, east, west);
            }
        }
    }
    
    public LargeCell GetLargeCell(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            return largeCells[x, y];
        }
        return null;
    }
    
    public void RemoveCells(List<Vector2Int> cellsToRemove)
    {
        foreach (Vector2Int cellPos in cellsToRemove)
        {
            if (IsValidCell(cellPos.x, cellPos.y))
            {
                LargeCell cell = largeCells[cellPos.x, cellPos.y];
                cell.active = false;
                cell.bActive = false;
                cell.state = CellState.Removed;
                
                // Also update the grid system
                if (gridSystem != null)
                {
                    gridSystem.SetLargeCellRemoved(cellPos.x, cellPos.y);
                }
            }
        }
    }
    
    public List<LargeCell> GetCellsNeedingWalls()
    {
        List<LargeCell> cellsNeedingWalls = new List<LargeCell>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell != null && cell.NeedsWall())
                {
                    cellsNeedingWalls.Add(cell);
                }
            }
        }
        
        return cellsNeedingWalls;
    }
    
    public List<LargeCell> GetAllActiveCells()
    {
        List<LargeCell> activeCells = new List<LargeCell>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell != null && cell.active && cell.bActive)
                {
                    activeCells.Add(cell);
                }
            }
        }
        
        return activeCells;
    }
    
    public List<LargeCell> GetWalkableCells()
    {
        List<LargeCell> walkableCells = new List<LargeCell>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell != null && cell.IsWalkable() && !cell.IsRemoved())
                {
                    walkableCells.Add(cell);
                }
            }
        }
        
        return walkableCells;
    }
    
    public List<LargeCell> GetPerimeterCells()
    {
        List<LargeCell> perimeterCells = new List<LargeCell>();
        
        // Top and bottom rows
        for (int x = 0; x < gridWidth; x++)
        {
            if (largeCells[x, 0] != null && largeCells[x, 0].active)
                perimeterCells.Add(largeCells[x, 0]);
            if (largeCells[x, gridHeight - 1] != null && largeCells[x, gridHeight - 1].active)
                perimeterCells.Add(largeCells[x, gridHeight - 1]);
        }
        
        // Left and right columns (excluding corners already added)
        for (int y = 1; y < gridHeight - 1; y++)
        {
            if (largeCells[0, y] != null && largeCells[0, y].active)
                perimeterCells.Add(largeCells[0, y]);
            if (largeCells[gridWidth - 1, y] != null && largeCells[gridWidth - 1, y].active)
                perimeterCells.Add(largeCells[gridWidth - 1, y]);
        }
        
        return perimeterCells;
    }
    
    public List<LargeCell> GetInteriorCells()
    {
        List<LargeCell> interiorCells = new List<LargeCell>();
        
        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int y = 1; y < gridHeight - 1; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell != null && cell.active && cell.bActive)
                {
                    interiorCells.Add(cell);
                }
            }
        }
        
        return interiorCells;
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.z / cellSize);
        return new Vector2Int(x, y);
    }
    
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x * cellSize + cellSize / 2f,
            0f,
            gridPosition.y * cellSize + cellSize / 2f
        );
    }
    
    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
    
    public bool IsCellActive(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            return largeCells[x, y].active && largeCells[x, y].bActive;
        }
        return false;
    }
    
    public bool IsCellWalkable(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            return largeCells[x, y].IsWalkable() && !largeCells[x, y].IsRemoved();
        }
        return false;
    }
    
    // Debug methods
    public void LogGridState()
    {
        Debug.Log($"CellManager Grid State: {gridWidth}x{gridHeight}");
        
        int activeCount = 0;
        int removedCount = 0;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell.active && cell.bActive)
                    activeCount++;
                else
                    removedCount++;
            }
        }
        
        Debug.Log($"Active cells: {activeCount}, Removed cells: {removedCount}");
    }
    
    // Gizmo drawing for debugging
    void OnDrawGizmos()
    {
        if (largeCells == null) return;
        
        Gizmos.color = Color.green;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                LargeCell cell = largeCells[x, y];
                if (cell != null && cell.active && cell.bActive)
                {
                    // Draw cell center
                    Gizmos.DrawWireCube(cell.worldPosition, Vector3.one * cellSize * 0.8f);
                }
                else if (cell != null)
                {
                    // Draw removed cells in red
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(cell.worldPosition, Vector3.one * cellSize * 0.8f);
                    Gizmos.color = Color.green;
                }
            }
        }
    }
}
