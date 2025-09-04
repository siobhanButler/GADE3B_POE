using UnityEngine;

[RequireComponent(typeof(MeshFilter))] //Require a MeshFilter component
[RequireComponent(typeof(MeshRenderer))] //Require a MeshRenderer component

public class MeshGenerator : MonoBehaviour
{
    //PROPERTIES
    Mesh mesh; //Mesh component
    MeshRenderer meshRenderer; //MeshRenderer component

    Vector3[] vertices; //Array of vertices (●)
    int[] triangles; //Array of triangles (△)
    Vector2[] uvs; //UV coordinates for texturing

    public int xSize = 10;
    public int zSize = 10;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        //CreateShape();  //Create the shape (2 triangles △ = 1 quad □)
        //UpdateMesh();  //Update the mesh
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

    public void UpdateMesh()
    {
        mesh.Clear();   //Clear the mesh

        //Assign vertices and triangles to the mesh (created in CreateShape())
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();  //Recalculate the normals of the mesh
    }
}
