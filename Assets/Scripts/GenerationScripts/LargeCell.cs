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
    public bool bWalkable;
    public CellBorderState borderState;
    
    // Neighbors
    public LargeCell north;         // (x, z+1)
    public LargeCell south;         // (x, z-1)
    public LargeCell east;          // (x+1, z)
    public LargeCell west;          // (x-1, z)
    public LargeCell northEast;     // (x+1, z+1)
    public LargeCell southEast;     // (x+1, z-1)
    public LargeCell southWest;     // (x-1, z-1)
    public LargeCell northWest;     // (x-1, z+1)
    
    // Constructor
    public LargeCell(int x, int y, Vector3 worldPos, int subCellCount)
    {
        this.x = x;
        this.y = y;
        this.worldPosition = worldPos;
        this.bActive = true;
        this.bWalkable = true;
        this.state = CellState.Floor;
        
        // Initialize sub-cells
        this.subCells = new SubCell[subCellCount, subCellCount];
        for (int sx = 0; sx < subCellCount; sx++)
        {
            for (int sy = 0; sy < subCellCount; sy++)
            {
                Vector3 subWorldPos = new Vector3(
                    worldPos.x - (subCellCount / 2f) + sx + 0.5f,
                    worldPos.y,
                    worldPos.z - (subCellCount / 2f) + sy + 0.5f
                );
                subCells[sx, sy] = new SubCell(sx, sy, subWorldPos, this);
            }
        }
        
        // Set up sub-cell neighbors AFTER all sub-cells are created
        for (int sx = 0; sx < subCellCount; sx++)
        {
            for (int sy = 0; sy < subCellCount; sy++)
            {
                subCells[sx, sy].SetupNeighbors(subCells, sx, sy);
            }
        }
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
            // Set all northmost sub-cells to North border
            int northEdge = subCells.GetLength(1) - 1; // Top row (highest Z)
            for (int x = 0; x < subCells.GetLength(0); x++)
            {
                if (subCells[x, northEdge] != null)
                {
                    subCells[x, northEdge].borderState = CellBorderState.North;
                }
            }
        }
        else if (!hasEastNeighbor)
        {
            borderState = CellBorderState.East;
            // Set all eastmost sub-cells to East border
            int eastEdge = subCells.GetLength(0) - 1; // Right column (highest X)
            for (int z = 0; z < subCells.GetLength(1); z++)
            {
                if (subCells[eastEdge, z] != null)
                {
                    subCells[eastEdge, z].borderState = CellBorderState.East;
                }
            }
        }
        else if (!hasSouthNeighbor)
        {
            borderState = CellBorderState.South;
            // Set all southmost sub-cells to South border
            int southEdge = 0; // Bottom row (lowest Z)
            for (int x = 0; x < subCells.GetLength(0); x++)
            {
                if (subCells[x, southEdge] != null)
                {
                    subCells[x, southEdge].borderState = CellBorderState.South;
                }
            }
        }
        else if (!hasWestNeighbor)
        {
            borderState = CellBorderState.West;
            // Set all westmost sub-cells to West border
            int westEdge = 0; // Left column (lowest X)
            for (int z = 0; z < subCells.GetLength(1); z++)
            {
                if (subCells[westEdge, z] != null)
                {
                    subCells[westEdge, z].borderState = CellBorderState.West;
                }
            }
        }
        else
        {
            borderState = CellBorderState.None; // Interior cell
        }
    }
    
    // Set neighbors
    public void SetNeighbors(LargeCell pNorth, LargeCell pSouth, LargeCell pEast, LargeCell pWest, LargeCell pNorthEast, LargeCell pSouthEast, LargeCell pSouthWest, LargeCell pNorthWest)
    {
        north = pNorth;
        south = pSouth;
        east = pEast;
        west = pWest;
        northEast = pNorthEast;
        southEast = pSouthEast;
        southWest = pSouthWest;
        northWest = pNorthWest;
    }
    
    public void SetSubCellNeighborsAndBorderStates()
    {
        // width = X dimension (first index), length = Z dimension (second index)
        int cellWidth = subCells.GetLength(0);
        int cellLength = subCells.GetLength(1);

        int lastX = cellWidth - 1;
        int lastZ = cellLength - 1;

        // Set up neighbor relationships for all cells
        for (int x = 0; x < cellWidth; x++)
        {
            for (int z = 0; z < cellLength; z++)
            {
                SubCell currentCell = subCells[x, z];

                // Cardinal neighbors with cross-large-cell stitching
                SubCell subNorth = (z + 1 < cellLength)
                    ? subCells[x, z + 1]
                    : (north != null ? north.subCells[x, 0] : null);

                SubCell subSouth = (z - 1 >= 0)
                    ? subCells[x, z - 1]
                    : (south != null ? south.subCells[x, lastZ] : null);

                SubCell subEast = (x + 1 < cellWidth)
                    ? subCells[x + 1, z]
                    : (east != null ? east.subCells[0, z] : null);

                SubCell subWest = (x - 1 >= 0)
                    ? subCells[x - 1, z]
                    : (west != null ? west.subCells[lastX, z] : null);

                // Diagonal neighbors with comprehensive cross-large-cell stitching
                SubCell subNorthEast;
                if (x + 1 < cellWidth && z + 1 < cellLength)
                {
                    subNorthEast = subCells[x + 1, z + 1];
                }
                else if (x + 1 < cellWidth && z + 1 >= cellLength)
                {
                    subNorthEast = (north != null) ? north.subCells[x + 1, 0] : null;
                }
                else if (x + 1 >= cellWidth && z + 1 < cellLength)
                {
                    subNorthEast = (east != null) ? east.subCells[0, z + 1] : null;
                }
                else
                {
                    subNorthEast = (northEast != null) ? northEast.subCells[0, 0] : null;
                }

                SubCell subSouthEast;
                if (x + 1 < cellWidth && z - 1 >= 0)
                {
                    subSouthEast = subCells[x + 1, z - 1];
                }
                else if (x + 1 < cellWidth && z - 1 < 0)
                {
                    subSouthEast = (south != null) ? south.subCells[x + 1, lastZ] : null;
                }
                else if (x + 1 >= cellWidth && z - 1 >= 0)
                {
                    subSouthEast = (east != null) ? east.subCells[0, z - 1] : null;
                }
                else
                {
                    subSouthEast = (southEast != null) ? southEast.subCells[0, lastZ] : null;
                }

                SubCell subSouthWest;
                if (x - 1 >= 0 && z - 1 >= 0)
                {
                    subSouthWest = subCells[x - 1, z - 1];
                }
                else if (x - 1 >= 0 && z - 1 < 0)
                {
                    subSouthWest = (south != null) ? south.subCells[x - 1, lastZ] : null;
                }
                else if (x - 1 < 0 && z - 1 >= 0)
                {
                    subSouthWest = (west != null) ? west.subCells[lastX, z - 1] : null;
                }
                else
                {
                    subSouthWest = (southWest != null) ? southWest.subCells[lastX, lastZ] : null;
                }

                SubCell subNorthWest;
                if (x - 1 >= 0 && z + 1 < cellLength)
                {
                    subNorthWest = subCells[x - 1, z + 1];
                }
                else if (x - 1 >= 0 && z + 1 >= cellLength)
                {
                    subNorthWest = (north != null) ? north.subCells[x - 1, 0] : null;
                }
                else if (x - 1 < 0 && z + 1 < cellLength)
                {
                    subNorthWest = (west != null) ? west.subCells[lastX, z + 1] : null;
                }
                else
                {
                    subNorthWest = (northWest != null) ? northWest.subCells[lastX, 0] : null;
                }

                currentCell.SetNeighbors(subNorth, subSouth, subEast, subWest, subNorthEast, subSouthEast, subSouthWest, subNorthWest);

                // Set border state for this cell
                currentCell.SetBorderState();
            }
        }

        Debug.Log("Neighbors and border states set up for all cells");
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
