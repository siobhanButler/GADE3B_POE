using System.Collections.Generic;
using UnityEngine;

public class FurnitureObj : MonoBehaviour
{
    public string furnitureName;
    public int width = 1;          //z, How many large grid tiles does it take up
    public int length = 1;         //x, How many large grid tiles does it take up
    public int height = 0;         //y
    public List<GridIndex> occupiedIndexes = new List<GridIndex>();     //indexes of the taken sub-cells
    public Vector3 spawnOffset;        //where the asset must be placed in reference to the source cell position cell.position + location
    public FurnitureType type;              //solid, walkable (underneath) etc
    public GameObject prefab;        // the actual prefab
    public bool[] takenSmallCellGrid;   //10x10 grid of small cells, true = occupied by furniture, false = free
    public bool[,] takenSmallCellGrid2D;                //optional: (length*subCellsPerAxis) x (width*subCellsPerAxis)
    public int subCellsPerAxis = 10;                    //number of subcells along one big-cell edge

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int expected = (subCellsPerAxis * subCellsPerAxis) * (length * width);
        if (takenSmallCellGrid == null)
        {
            takenSmallCellGrid = new bool[expected];
        }
        else if (takenSmallCellGrid.Length != expected)
        {
            bool[] resized = new bool[expected];
            int copyLength = Mathf.Min(takenSmallCellGrid.Length, resized.Length);
            System.Array.Copy(takenSmallCellGrid, resized, copyLength);
            takenSmallCellGrid = resized;
        }

        //finding the average of the cells (built for square/rectangular furiture)
        float x = width / (width* length);
        float z = length/ (width * length);
        spawnOffset = new Vector3(x, height, z);

        prefab = this.gameObject;
    }

    // Update method removed - no per-frame logic needed

    public void ConvertTakenGridToOccupiedIndexes()
    {
        occupiedIndexes.Clear();

        for (int bigZ = 0; bigZ < length; bigZ++)
        {
            for (int bigX = 0; bigX < width; bigX++)
            {
                for (int sz = 0; sz < subCellsPerAxis; sz++)
                {
                    for (int sx = 0; sx < subCellsPerAxis; sx++)
                    {
                        int index = sz * subCellsPerAxis + sx;
                        if (index >= 0 && index < takenSmallCellGrid.Length && takenSmallCellGrid[index])
                        {
                            occupiedIndexes.Add(new GridIndex(bigX, bigZ, sx, sz));
                        }
                    }
                }
            }
        }
    }

    // Use this when providing a 2D grid that spans across all covered big cells.
    // Dimensions should be: rows = length * subCellsPerAxis (z), cols = width * subCellsPerAxis (x)
    public void ConvertTakenGrid2DToOccupiedIndexes()
    {
        occupiedIndexes.Clear();
        if (takenSmallCellGrid2D == null)
        {
            return;
        }

        int rows = takenSmallCellGrid2D.GetLength(0); // z
        int cols = takenSmallCellGrid2D.GetLength(1); // x

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (!takenSmallCellGrid2D[z, x])
                {
                    continue;
                }

                int bigZ = z / subCellsPerAxis;
                int bigX = x / subCellsPerAxis;
                int sz = z % subCellsPerAxis;
                int sx = x % subCellsPerAxis;

                if (bigZ < length && bigX < width)
                {
                    occupiedIndexes.Add(new GridIndex(bigX, bigZ, sx, sz));
                }
            }
        }
    }

    public LargeCell[,] SetGridState(LargeCell[,] grid, LargeCell sourceCell)
    {
        if (occupiedIndexes == null || occupiedIndexes.Count == 0)
        {
            if (takenSmallCellGrid2D != null)
            {
                ConvertTakenGrid2DToOccupiedIndexes();
            }
            else if (takenSmallCellGrid != null)
            {
                ConvertTakenGridToOccupiedIndexes();
            }
        }
        int gridWidth = grid.GetLength(0);
        int gridLength = grid.GetLength(1);

        for (int x = sourceCell.x; x < sourceCell.x + width; x++)  //x = sourceCell.x so we start on source cell
        {
            if (x < 0 || x >= gridWidth) { continue; }

            for (int z = sourceCell.y; z < sourceCell.y + length; z++)
            {
                if (z < 0 || z >= gridLength) { continue; }
                if(type == FurnitureType.Solid)   //if furniture takes up whole big cell, big cell can be tagged Furniture
                {
                    grid[x, z].state = CellState.Furniture;
                    foreach(SubCell sCell in grid[x, z].subCells)
                    {
                        sCell.state = CellState.Furniture;
                    }
                }
                else
                {
                    // Loop through subcells of this big cell correctly
                    for (int sx = 0; sx < grid[x, z].subCells.GetLength(0); sx++)
                    {
                        for (int sz = 0; sz < grid[x, z].subCells.GetLength(1); sz++)
                        {
                            GridIndex index = new GridIndex(x - sourceCell.x, z - sourceCell.y, sx, sz);
                            if (occupiedIndexes.Contains(index))
                            {
                                grid[x, z].subCells[sx, sz].state = CellState.Furniture;
                            }
                        }
                    }
                }
            }
        }
        return grid;
    }

    //width= 1, length = 1, Taken cells aka CellState.Furniture cells (large cell size = 10x10): 
    //      [1,1], [2,1], [1,2], [2,2]
    //      [7,1], [8,1], [7,2], [8,2]
    //      [1,7], [2,7], [1,8], [2,8]
    //      [7,7], [8,7], [7,8], [8,8]
    //  0 0 0 0 0 0 0 0 0 0
    //  0 1 1 0 0 0 0 1 1 0
    //  0 1 1 0 0 0 0 1 1 0
    //  0 0 0 0 0 0 0 0 0 0
    //  0 0 0 0 0 0 0 0 0 0
    //  0 0 0 0 0 0 0 0 0 0
    //  0 0 0 0 0 0 0 0 0 0
    //  0 1 1 0 0 0 0 1 1 0
    //  0 1 1 0 0 0 0 1 1 0
    //  0 0 0 0 0 0 0 0 0 0

}

[System.Serializable]
public enum FurnitureType
{
    Solid,      //can't have items placed underneath --> large cell will become furniture cell
    Walkable,
}

[System.Serializable]
public struct GridIndex
{
    public int x;   //width
    public int z;   //length
    public int sx;
    public int sz;

    public GridIndex(int x, int z, int sx, int sz)
    {
        this.x = x;
        this.z = z;
        this.sx = sx;
        this.sz = sz;
    }
}

/*  
    public Vector3 GetSpawnPosition(LargeCell[,] grid, LargeCell sourceCell)
    {
        Vector3 position = new Vector3(0,0,0);
        for (int w = sourceCell.x; w <= width + sourceCell.x; x++)  //x = sourceCell.x so we start on source cell
        {
            for (int l = sourceCell.z; l <= length + sourceCell.z; z++)
            {
                position += grid[w,l].worldPosition;
            }
        }
        position = position / (length * width);     //get the average of all locations
        return position;
    }
*/
