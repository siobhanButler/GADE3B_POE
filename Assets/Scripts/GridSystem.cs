using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    // Grid dimensions
    private int largeCellWidth;
    private int largeCellLength;
    private float largeCellSize;
    private float subCellSize;
    private int subCellsPerLargeCell;
    
    // Grid data
    private bool[,] largeCellRemoved; // Tracks removed large cells
    private CellType[,] subCellTypes; // Tracks sub-cell types
    private GameObject[,] subCellObjects; // References to objects in sub-cells
    
    // Sub-cell types
    public enum CellType
    {
        Empty,
        Wall,
        Door,
        Furniture,
        MainTower,
        EnemySpawner,
        DefenseTower,
        EnemyPath,
        Occupied
    }
    
    public void Initialize(int largeWidth, int largeLength, float cellSize, float subCellSize, int subCellsPerLarge)
    {
        largeCellWidth = largeWidth;
        largeCellLength = largeLength;
        largeCellSize = cellSize;
        this.subCellSize = subCellSize;
        subCellsPerLargeCell = subCellsPerLarge;
        
        // Initialize arrays
        largeCellRemoved = new bool[largeWidth, largeLength];
        subCellTypes = new CellType[largeWidth * subCellsPerLarge, largeLength * subCellsPerLarge];
        subCellObjects = new GameObject[largeWidth * subCellsPerLarge, largeLength * subCellsPerLarge];
    }
    
    // Large cell operations
    public void SetLargeCellRemoved(int x, int z)
    {
        if (IsValidLargeCell(x, z))
        {
            largeCellRemoved[x, z] = true;
            
            // Mark all sub-cells in this large cell as walls
            int subXStart = x * subCellsPerLargeCell;
            int subZStart = z * subCellsPerLargeCell;
            
            for (int sx = subXStart; sx < subXStart + subCellsPerLargeCell; sx++)
            {
                for (int sz = subZStart; sz < subZStart + subCellsPerLargeCell; sz++)
                {
                    if (IsValidSubCell(sx, sz))
                    {
                        subCellTypes[sx, sz] = CellType.Wall;
                    }
                }
            }
        }
    }
    
    public bool IsLargeCellRemoved(int x, int z)
    {
        return IsValidLargeCell(x, z) && largeCellRemoved[x, z];
    }
    
    public Vector3 GetLargeCellWorldPosition(int x, int z)
    {
        return new Vector3(
            x * largeCellSize + largeCellSize / 2f,
            0f,
            z * largeCellSize + largeCellSize / 2f
        );
    }
    
    // Sub-cell operations
    public void SetSubCellType(int x, int z, CellType type)
    {
        if (IsValidSubCell(x, z))
        {
            subCellTypes[x, z] = type;
        }
    }
    
    public CellType GetSubCellType(int x, int z)
    {
        return IsValidSubCell(x, z) ? subCellTypes[x, z] : CellType.Wall;
    }
    
    public bool IsSubCellWalkable(int x, int z)
    {
        CellType type = GetSubCellType(x, z);
        return type == CellType.Empty || type == CellType.EnemyPath;
    }
    
    public bool IsSubCellPlaceable(int x, int z)
    {
        return IsValidSubCell(x, z) && subCellTypes[x, z] == CellType.Empty;
    }
    
    public Vector3 GetSubCellWorldPosition(int x, int z)
    {
        return new Vector3(
            x * subCellSize + subCellSize / 2f,
            0f,
            z * subCellSize + subCellSize / 2f
        );
    }
    
    public void SetSubCellObject(int x, int z, GameObject obj)
    {
        if (IsValidSubCell(x, z))
        {
            subCellObjects[x, z] = obj;
        }
    }
    
    public GameObject GetSubCellObject(int x, int z)
    {
        return IsValidSubCell(x, z) ? subCellObjects[x, z] : null;
    }
    
    // Utility methods
    public bool IsValidLargeCell(int x, int z)
    {
        return x >= 0 && x < largeCellWidth && z >= 0 && z < largeCellLength;
    }
    
    public bool IsValidSubCell(int x, int z)
    {
        return x >= 0 && x < largeCellWidth * subCellsPerLargeCell && 
               z >= 0 && z < largeCellLength * subCellsPerLargeCell;
    }
    
    public Vector2Int WorldToSubCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / subCellSize);
        int z = Mathf.FloorToInt(worldPosition.z / subCellSize);
        return new Vector2Int(x, z);
    }
    
    public Vector2Int WorldToLargeCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / largeCellSize);
        int z = Mathf.FloorToInt(worldPosition.z / largeCellSize);
        return new Vector2Int(x, z);
    }
    
    // Pathfinding helper methods
    public List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = cell + dir;
            if (IsValidSubCell(neighbor.x, neighbor.y) && IsSubCellWalkable(neighbor.x, neighbor.y))
            {
                neighbors.Add(neighbor);
            }
        }
        
        return neighbors;
    }
    
    // Debug visualization
    public void DrawGridGizmos()
    {
        Gizmos.color = Color.white;
        
        // Draw large cell grid
        for (int x = 0; x <= largeCellWidth; x++)
        {
            Vector3 start = new Vector3(x * largeCellSize, 0, 0);
            Vector3 end = new Vector3(x * largeCellSize, 0, largeCellLength * largeCellSize);
            Gizmos.DrawLine(start, end);
        }
        
        for (int z = 0; z <= largeCellLength; z++)
        {
            Vector3 start = new Vector3(0, 0, z * largeCellSize);
            Vector3 end = new Vector3(largeCellWidth * largeCellSize, 0, z * largeCellSize);
            Gizmos.DrawLine(start, end);
        }
        
        // Draw sub-cell grid
        Gizmos.color = Color.gray;
        
        for (int x = 0; x <= largeCellWidth * subCellsPerLargeCell; x++)
        {
            Vector3 start = new Vector3(x * subCellSize, 0.01f, 0);
            Vector3 end = new Vector3(x * subCellSize, 0.01f, largeCellLength * largeCellSize);
            Gizmos.DrawLine(start, end);
        }
        
        for (int z = 0; z <= largeCellLength * subCellsPerLargeCell; z++)
        {
            Vector3 start = new Vector3(0, 0.01f, z * subCellSize);
            Vector3 end = new Vector3(largeCellWidth * largeCellSize, 0.01f, z * subCellSize);
            Gizmos.DrawLine(start, end);
        }
    }
}
