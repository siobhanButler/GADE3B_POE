using UnityEngine;

public class SubCell
{
    public Vector3 worldPosition;
    public int localX, localZ;   // position inside the LargeCell
    public CellState state;
    public CellBorderState borderState;
    public LargeCell parentCell;
    public TowerManager[] inRangeTowers;    //if it is a path, this will be populated. Determines the path difficulty (can make this a list)
    
    // Neighbors (inside the same LargeCell)
    public SubCell north;
    public SubCell south;
    public SubCell east;
    public SubCell west;
    public SubCell northEast;     // (x+1, z+1)
    public SubCell southEast;     // (x+1, z-1)
    public SubCell southWest;     // (x-1, z-1)
    public SubCell northWest;     // (x-1, z+1)
    
    // Constructor
    public SubCell(int x, int z, Vector3 worldPos, LargeCell parentCell)
    {
        this.localX = x;
        this.localZ = z;
        this.worldPosition = worldPos;
        this.state = CellState.Floor;
        this.borderState = CellBorderState.None;
        this.parentCell = parentCell;
    }
    
    // Set neighbors
    public void SetNeighbors(SubCell pNorth, SubCell pSouth, SubCell pEast, SubCell pWest, SubCell pNorthEast, SubCell pSouthEast, SubCell pSouthWest, SubCell pNorthWest)
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

    public void SetBorderState()
    {
        // Check if this cell is on the border (has null neighbors)
        bool hasNorthNeighbor = north != null;
        bool hasEastNeighbor = east != null;
        bool hasSouthNeighbor = south != null;
        bool hasWestNeighbor = west != null;

/*
        int subCellLength = parentCell.subCells.GetLength(0);
        int subCellWidth = parentCell.subCells.GetLength(1);
        
        if(localZ == subCellLength - 1)
        {
            if(localX == 0)
            {
                borderState = CellBorderState.NWCorner;
            }
            else if(localX == subCellWidth - 1)
            {
                borderState = CellBorderState.NECorner;
            }
            else
            {
                borderState = CellBorderState.North;
            }
        }
        else if(localZ == 0)
        {
            if(localX == 0)
            {
                borderState = CellBorderState.SWCorner;
            }
            else if(localX == subCellWidth - 1)
            {
                borderState = CellBorderState.SECorner;
            }
            else
            {
                borderState = CellBorderState.South;
            }
        }
        else if(localX == 0)
        {
            borderState = CellBorderState.West;
        }
        else if(localX == subCellWidth - 1)
        {
            borderState = CellBorderState.East;
        }
        */

        
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
    
    // Get all neighbors as array
    public SubCell[] GetNeighbors()
    {
        return new SubCell[] { north, south, east, west };
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
        return state == CellState.Removed;
    }
    
    // Set up neighbors within the same large cell
    public void SetupNeighbors(SubCell[,] subCells, int x, int y)
    {
        // North neighbor
        north = (y + 1 < subCells.GetLength(1)) ? subCells[x, y + 1] : null;
        
        // South neighbor
        south = (y - 1 >= 0) ? subCells[x, y - 1] : null;
        
        // East neighbor
        east = (x + 1 < subCells.GetLength(0)) ? subCells[x + 1, y] : null;
        
        // West neighbor
        west = (x - 1 >= 0) ? subCells[x - 1, y] : null;
        
        // Debug logging
        int neighborCount = 0;
        if (north != null) neighborCount++;
        if (south != null) neighborCount++;
        if (east != null) neighborCount++;
        if (west != null) neighborCount++;
        
        Debug.Log($"SubCell at ({x},{y}) has {neighborCount} neighbors within large cell");
    }
}
