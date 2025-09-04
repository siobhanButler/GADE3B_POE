using UnityEngine;

public class SubCell
{
    public Vector3 worldPosition;
    public int localX, localY;   // position inside the LargeCell
    public CellState state;
    public LargeCell parentCell;
    
    // Neighbors (inside the same LargeCell)
    public SubCell north;
    public SubCell south;
    public SubCell east;
    public SubCell west;
    
    // Constructor
    public SubCell(int x, int y, Vector3 worldPos, LargeCell parentCell)
    {
        this.localX = x;
        this.localY = y;
        this.worldPosition = worldPos;
        this.state = CellState.Floor;
        this.parentCell = parentCell;
    }
    
    // Set neighbors
    public void SetNeighbors(SubCell north, SubCell south, SubCell east, SubCell west)
    {
        this.north = north;
        this.south = south;
        this.east = east;
        this.west = west;
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
}
