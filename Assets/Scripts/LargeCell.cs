using UnityEngine;

public class LargeCell
{
    public Vector3 worldPosition; // center in world space
    public int x, y;             // grid coords
    public bool active;          // whether this big cell is removed
    public SubCell[,] subCells;  // 9x9 or 10x10 sub cells
    
    
    // Cell state and type
    public CellState state;
    public bool bActive;        //removed or not
    public CellBorderState borderState;
    
    // Neighbors
    public LargeCell north;
    public LargeCell south;
    public LargeCell east;
    public LargeCell west;
    
    // Constructor
    public LargeCell(int x, int y, Vector3 worldPos, int subCellCount)
    {
        this.x = x;
        this.y = y;
        this.worldPosition = worldPos;
        this.bActive = true;
        this.state = CellState.Floor;
        
        // Initialize sub-cells
        this.subCells = new SubCell[subCellCount, subCellCount];
        /*
        for (int sx = 0; sx < subCellCount; sx++)
        {
            for (int sy = 0; sy < subCellCount; sy++)
            {
                Vector3 subWorldPos = new Vector3(
                    worldPos.x - (subCellCount / 2f) + sx + 0.5f,
                    worldPos.y,
                    worldPos.z - (subCellCount / 2f) + sy + 0.5f
                );
                subCells[sx, sy] = new SubCell(sx, sy, subWorldPos);
            }
        }
        */
    }

    public void SetBorderState()
    {
        // Check if this cell is on the border (has null neighbors)
        bool hasNorthNeighbor = north != null;
        bool hasEastNeighbor = east != null;
        bool hasSouthNeighbor = south != null;
        bool hasWestNeighbor = west != null;
        
        // Determine border state based on missing neighbors
        if (!hasNorthNeighbor && !hasEastNeighbor)
        {
            borderState = CellBorderState.NECorner;
        }
        else if (!hasEastNeighbor && !hasSouthNeighbor)
        {
            borderState = CellBorderState.SECorner;
        }
        else if (!hasSouthNeighbor && !hasWestNeighbor)
        {
            borderState = CellBorderState.SWCorner;
        }
        else if (!hasWestNeighbor && !hasNorthNeighbor)
        {
            borderState = CellBorderState.NWCorner;
        }
        else if (!hasNorthNeighbor)
        {
            borderState = CellBorderState.North;
        }
        else if (!hasEastNeighbor)
        {
            borderState = CellBorderState.East;
        }
        else if (!hasSouthNeighbor)
        {
            borderState = CellBorderState.South;
        }
        else if (!hasWestNeighbor)
        {
            borderState = CellBorderState.West;
        }
        else
        {
            borderState = CellBorderState.None; // Interior cell
        }
    }
    
    // Set neighbors
    public void SetNeighbors(LargeCell pNorth, LargeCell pSouth, LargeCell pEast, LargeCell pWest)
    {
        north = pNorth;
        south = pSouth;
        east = pEast;
        west = pWest;
    }
    
    // Get all neighbors as array
    public LargeCell[] GetNeighbors()
    {
        return new LargeCell[] { north, south, east, west };
    }
    
    // Check if this cell needs a wall (borders with removed/inactive cell)
    public bool NeedsWall()
    {
        if (!active) return false; // Don't place walls on removed cells
        
        // Check if any neighbor is removed/inactive
        LargeCell[] neighbors = GetNeighbors();
        foreach (LargeCell neighbor in neighbors)
        {
            if (neighbor == null || !neighbor.active)
            {
                return true;
            }
        }
        return false;
    }
    
    // Get wall directions needed
    public WallDirection GetWallDirections()
    {
        WallDirection walls = WallDirection.None;
        
        if (north == null || !north.active) walls |= WallDirection.North;
        if (south == null || !south.active) walls |= WallDirection.South;
        if (east == null || !east.active) walls |= WallDirection.East;
        if (west == null || !west.active) walls |= WallDirection.West;
        
        return walls;
    }
    
    // Helper methods to check cell properties
    public bool IsWalkable()
    {
        return state == CellState.Floor || state == CellState.Door || state == CellState.EnemyPath;
    }
    
    public bool HasWall()
    {
        return state == CellState.Wall;
    }
    
    public bool HasDoor()
    {
        return state == CellState.Door;
    }
    
    public bool IsRemoved()
    {
        return state == CellState.Removed || !bActive;
    }
}

// Enums for cell management
public enum CellState
{
    Floor,      //Active (default state)
    Removed,    //Inactive (e.g. removed corner)     
    Wall,
    Door,
    Furniture,
    MainTower,
    EnemySpawner,
    DefenseTower,
    EnemyPath
}

public enum CellBorderState
{
    None = 0,       //not a border
    North = 1,  
    East = 2,    
    South = 3,  
    West = 4,

    NECorner = 5,
    SECorner = 6,
    SWCorner = 7,
    NWCorner = 8,
}

[System.Flags]
public enum WallDirection
{
    None = 0,
    North = 1,
    South = 2,
    East = 4,
    West = 8
}
