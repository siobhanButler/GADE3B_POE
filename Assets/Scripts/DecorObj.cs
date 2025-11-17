using UnityEngine;

public class DecorObj : MonoBehaviour
{
    [Header("Identity")]
    public string decorName;

    [Header("Footprint (in subcells)")]
    public int width = 1;     // X size in subcells
    public int height = 1;    // Z size in subcells

    [Header("Spawn Settings")]
    [Range(0.1f, 1f)] public float likelihood = 1f;
    public Vector3 additionalSpawnOffset; // optional local offset (e.g., to adjust pivot Y)

    [Header("Rotation")]
    public bool canBeRotated = false;

    [SerializeField] private LayerMask decorMask;
    public GameObject prefab;

    void Awake()
    {
        // Ensure this object (and children) are on the Decor layer if it exists
        int decorLayer = LayerMask.NameToLayer("Decor");
        if (decorLayer >= 0)
        {
            decorMask = 1 << decorLayer;
            SetLayerRecursively(transform, decorLayer);
        }
        else
        {
            Debug.LogWarning("DecorObj Awake(): Layer 'Decor' not found. Please add it to Project Settings > Tags and Layers.");
        }

        EnsureTriggerCollider();
        prefab = this.gameObject;
    }

    void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    void EnsureTriggerCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        MeshCollider meshCol = col as MeshCollider;
        if (meshCol != null)
        {
            meshCol.convex = true;
        }
        col.isTrigger = true;
    }

    // Returns true if the rectangular footprint fits entirely in the large cell and does not overlap furniture-marked subcells.
    public bool CanPlaceInCell(LargeCell cell, int anchorSubX, int anchorSubZ)
    {
        if (cell == null || cell.subCells == null) return false;
        if (cell.IsRemoved()) return false;
        if (width <= 0 || height <= 0) return false;

        int subW = cell.subCells.GetLength(0);
        int subH = cell.subCells.GetLength(1);

        if (anchorSubX < 0 || anchorSubZ < 0) return false;
        if (anchorSubX + width > subW) return false;
        if (anchorSubZ + height > subH) return false;

        for (int dx = 0; dx < width; dx++)
        {
            for (int dz = 0; dz < height; dz++)
            {
                SubCell sc = cell.subCells[anchorSubX + dx, anchorSubZ + dz];
                if (sc == null) return false;
                if (sc.state != CellState.Floor) return false; // must be free floor (not furniture, tower, etc.)
            }
        }
        return true;
    }

    // Marks the footprint subcells as Furniture to reserve those subcells (does not change the LargeCell state).
    public void ApplyToGrid(LargeCell cell, int anchorSubX, int anchorSubZ)
    {
        if (cell == null || cell.subCells == null) return;

        int subW = cell.subCells.GetLength(0);
        int subH = cell.subCells.GetLength(1);
        int maxX = Mathf.Min(anchorSubX + width, subW);
        int maxZ = Mathf.Min(anchorSubZ + height, subH);

        for (int sx = anchorSubX; sx < maxX; sx++)
        {
            for (int sz = anchorSubZ; sz < maxZ; sz++)
            {
                SubCell sc = cell.subCells[sx, sz];
                if (sc != null)
                {
                    sc.state = CellState.Furniture;
                }
            }
        }
    }

    // Convenience overload mirroring FurnitureObj-style usage; leaves large-cell state unchanged.
    public LargeCell[,] SetGridState(LargeCell[,] grid, LargeCell sourceCell, int anchorSubX, int anchorSubZ)
    {
        if (grid == null || sourceCell == null) return grid;
        ApplyToGrid(sourceCell, anchorSubX, anchorSubZ);
        return grid;
    }

    // Computes the world-space spawn position centered on the rectangular footprint anchored at (anchorSubX, anchorSubZ).
    public Vector3 GetSpawnPosition(LargeCell cell, int anchorSubX, int anchorSubZ)
    {
        if (cell == null) return transform.position;
        if (cell.subCells == null) return cell.worldPosition + additionalSpawnOffset;

        int subCountX = cell.subCells.GetLength(0);
        int subCountZ = cell.subCells.GetLength(1);

        float localCenterX = -subCountX / 2f + anchorSubX + (width / 2f);
        float localCenterZ = -subCountZ / 2f + anchorSubZ + (height / 2f);

        Vector3 centered = new Vector3(cell.worldPosition.x + localCenterX, cell.worldPosition.y, cell.worldPosition.z + localCenterZ);
        return centered + additionalSpawnOffset;
    }

    // Returns the rotation that should be applied on spawn.
    public Quaternion GetSpawnRotation(Quaternion baseRotation)
    {
        if (!canBeRotated) return baseRotation;
        float y = Random.Range(0f, 360f);
        return Quaternion.Euler(0f, y, 0f) * baseRotation;
    }

    public Quaternion GetSpawnRotation()
    {
        return GetSpawnRotation(Quaternion.identity);
    }

    // Optional utility if spawning code wants to apply after Instantiate.
    public void ApplyRotationOnSpawn(Transform target)
    {
        if (target == null) return;
        target.rotation = GetSpawnRotation(target.rotation);
    }
}
