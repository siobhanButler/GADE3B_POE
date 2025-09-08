using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))] //Require a MeshFilter component
[RequireComponent(typeof(MeshRenderer))] //Require a MeshRenderer component
[RequireComponent(typeof(MeshCollider))] //Require a MeshRenderer component

public class MeshGenerator : MonoBehaviour
{
    //PROPERTIES
    Mesh mesh; //Mesh component
    MeshRenderer meshRenderer; //MeshRenderer component

    Vector3[] vertices; //Array of vertices (●)
    int[] triangles;    //Array of triangles (△)
    Vector2[] uvs;      //UV coordinates for texturing
    
    List<Vector3> allVertices; //Combined vertices from all cells
    List<int> allTriangles;    //Combined triangles from all cells

    public int xSize = 9;       //overrides RoomGen's cell size
    public int zSize = 9;

    private LargeCell[,] grid;  //grid goes grid[x,z] where z is the row and x is the column

    public LargeCell[,] CreateGrid(int xRoomSize, int zRoomSize, int subCellsPerLargeCell)
    {
        //instantiating here instead of start() because it was happening too late
        mesh = new Mesh();
        mesh.name = "Floor Mesh";
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        grid = new LargeCell[xRoomSize, zRoomSize];
        
        xSize = zSize = subCellsPerLargeCell;

        //Initialize lists for combined mesh data
        allVertices = new List<Vector3>();
        allTriangles = new List<int>();

        //z first then x, so that it matches the CreateCellShape()'s left to right, bottom to top method
        for(int z = 0; z < zRoomSize; z++)
        {
            for(int x = 0; x < xRoomSize; x++)
            {
                CreateCellShape(x, z);  //x and z determine the cell starting position
                //UpdateMesh will not be called here
            }
        }

        //After all cells are created, update the mesh with combined data
        UpdateCombinedMesh();

        //Update Neighbours not required here
        /*
        for(int z = 0; z < zRoomSize; z++)
        {
            for(int x = 0; x < xRoomSize; x++)
            {
                //Update Neighbours
                UpdateNeighbours(x, z);
            }
        }
        */

        return grid;
    }

    public void CreateCellShape(int xCellPosition, int zCellPosition) //xCellPosition and zCellPosition determine the starting positions of the cells
    {
        int xStartPosition = xCellPosition * xSize;
        int zStartPosition = zCellPosition * zSize;

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];   //vertexCount = (xSize + 1) * (zSize + 1)

        //loop through vertices, assigning them a position on the grid(i) and in space (Vector3), going from left to right
        int i = 0;
        for(int z = 0; z <= zSize; z++)  //<= because its zSize+1
        {
            for(int x = 0; x <= xSize; x++)  //<= because its xSize+1
            {
                vertices[i] = new Vector3(x+xStartPosition, 0, z+zStartPosition);   //adjusting the location based on the starting position
                i++;
            }
        }
        
        //Create new LargeCell using newly created vertices to calculate the center
        Vector3 center = (vertices[0] 
                         + vertices[xSize] 
                         + vertices[zSize * (xSize + 1)] 
                         + vertices[(zSize * (xSize + 1)) + xSize]) / 4;
        grid[xCellPosition, zCellPosition] = new LargeCell(xCellPosition, zCellPosition, center, xSize);

        //Store the starting index for this cell's vertices in the combined list
        int vertexStartIndex = allVertices.Count;
        //Add vertices array to the combined list
        allVertices.AddRange(vertices);
        
        //Create the sub-cell quads
        triangles = new int[xSize * zSize * 6];
        int vert = 0 + vertexStartIndex;    //using vertexStartIndex to account for vertices already in the combined list
        int tris = 0;

        for(int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                //triangle 1
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                //triangle 2
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                //Add this new subCell to Large cell sub-cell array (-vertexStartIndex to account for the previous vert + vertexStartIndex adjustment)
                Vector3 subCellCenter = (vertices[vert-vertexStartIndex] 
                                        + vertices[vert-vertexStartIndex + 1] 
                                        + vertices[vert-vertexStartIndex + xSize + 1] 
                                        + vertices[vert-vertexStartIndex + xSize + 2]) / 4;
                
                // Bounds check to prevent IndexOutOfRangeException
                if (x < grid[xCellPosition, zCellPosition].subCells.GetLength(0) && z < grid[xCellPosition, zCellPosition].subCells.GetLength(1))
                {
                    grid[xCellPosition, zCellPosition].subCells[x, z] = 
                    new SubCell(x, z, subCellCenter, grid[xCellPosition, zCellPosition]);
                }
                else
                {
                    Debug.Log("MeshGenerator CreateCellShape(): IndexOutOfRangeException");
                }

                vert++;
                tris += 6;
            }
            vert++;
        }
        
        //Add triangles array to the combined list
        allTriangles.AddRange(triangles);
    }

    public void UpdateCombinedMesh()
    {
        //Clear the mesh
        mesh.Clear();

        //Assign combined vertices and triangles to the mesh
        mesh.vertices = allVertices.ToArray();
        mesh.triangles = allTriangles.ToArray();

        // Generate UVs based on world XZ so textures can tile/repeat across the whole floor
        // This maps 1 UV unit per 1 world unit, enabling a texture set to 1x1 tiling to visibly repeat per large-cell
        if (allVertices != null && allVertices.Count > 0)
        {
            Vector2[] generatedUVs = new Vector2[allVertices.Count];
            for (int i = 0; i < allVertices.Count; i++)
            {
                Vector3 v = allVertices[i];
                generatedUVs[i] = new Vector2(v.x, v.z);    //create vector2 based on the vertice's x and y coordinates
            }
            mesh.uv = generatedUVs;
        }

        mesh.RecalculateNormals();  //Recalculate the normals of the mesh
        mesh.RecalculateBounds();   //for mesh collider

        //For Physics
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;   // clear first to force Unity to refresh
            meshCollider.sharedMesh = mesh;
        }
    }


/*  ======================== UNUSED METHODS ========================

    public void UpdateNeighbours(int x, int z)
    {
        //z is row so arrayName.GetLength(0); x is column so arrayName.GetLength(1)
        //North (x, z + 1)
        if(z+1 >= 0 && z+1 < grid.GetLength(1))
        {
            grid[x,z].north = grid[x,z+1];
        }
        else
        {
            grid[x,z].north = null;
        }
        //South (x, z - 1)
        if(z-1 >= 0 && z-1 < grid.GetLength(1))
        {
            grid[x,z].south = grid[x,z-1];
        }
        else
        {
            grid[x,z].south = null;
        }
        //East (x + 1, z)
        if(x+1 >= 0 && x+1 < grid.GetLength(0))
        {
            grid[x,z].east = grid[x+1,z];
        }
        else
        {
            grid[x,z].east = null;
        }
        //West (x - 1, z)
        if(x-1 >= 0 && x-1 < grid.GetLength(0))
        {
            grid[x,z].west = grid[x-1,z];
        }
        else
        {
            grid[x,z].west = null;
        }

        grid[x,z].SetBorderState();

        //  (0,2) - (1,2) - (2,2)       X - N - X          ()   - (x,z+1) -   ()
        //  (0,1) - (1,1) - (2,1)       W - O - E       (x-1,z) -  (x,z)  - (x+1,z)
        //  (0,1) - (1,0) - (2,0)       X - S - X          ()   - (x,z-1) -   ()
    }

    public void CreateQuad()
    {
        //Specify verticies array's elements
        vertices = new Vector3[]
        {
            //new Vector3(x,y,z)            // Diagram shows the numbered vertices ● (x → and z is ↓ (and y is up ↑, so not visible))
            new Vector3(0,0,0), //0         //        0     1   (x)
            new Vector3(0,0,1), //1         //    0   1●   ●3
            new Vector3(1,0,0), //2         //    1   0●   ●2   
            new Vector3(1,0,1), //3         //    (z)   
        };

        //Specify triangles array's elements
        triangles = new int[]
        {
            //Vertices in clockwise order (to determine culling)
            0, 1, 2,
            1, 3, 2     //could also be 2, 1, 3
            //    1● → ●3
            //    ↑  ╲  ↓
            //    0● ← ●2   
        };

    }

    public void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];   //vertexCount = (xSize + 1) * (zSize + 1)

        //loop through vertices, assigning them a position on the grid, going from left to right
        int i = 0;
        for(int z = 0; z <= zSize; z++)  //<= because its zSize+1
        {
            for(int x = 0; x <= xSize; x++)  //<= because its xSize+1
            {
                vertices[i] = new Vector3(x, 0, z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for(int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
        
        //    (0,2) - (1,2)  - (2,2)
        //    |         |        |
        //    (0,1) - (1,1)  - (2,1)
        //    |         |        |
        //    (0,0) - (1,0) - (2,0)  
    }

    //OLD UPDATE MESH METHOD
    public void UpdateMesh()
    {
        mesh.Clear();   //Clear the mesh

        //Assign vertices and triangles to the mesh (created in CreateShape())
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();  //Recalculate the normals of the mesh
    }
*/
}
