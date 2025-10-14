using UnityEngine;

public class TowerManager : ObjectManager
{
    public int pathCellsInRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
        ComputePathCellsInRange();
    }

    // Update method removed - no per-frame logic needed
    
    public override void OnDeath()
    {
        if (this.tag == "MainTower")
        {
            // Find the GameManager and trigger game over
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.GameOver();
            }
        }
        else if(this.tag == "DefenceTower")
        {
            TowerLocationManager towerLocationManager = GetComponentInParent<TowerLocationManager>();
            if (towerLocationManager != null)
            {
                towerLocationManager.isOccupied = false;
                if (towerLocationManager.tower == this.gameObject)
                {
                    towerLocationManager.tower = null;
                }
            }
            else{
                Debug.LogWarning("TowerManager OnDeath(): TowerLocationManager is null during tower death");
            }
            // Unregister this tower from path overlaps and refresh visuals before destruction
            PathGenerator pathGenerator = FindFirstObjectByType<PathGenerator>();
            if (pathGenerator != null)
            {
                pathGenerator.UnregisterTowerOverlaps(this);
            }

            Destroy(gameObject);
        }
    }

    void ComputePathCellsInRange()
    {
        PathGenerator pathGenerator = FindFirstObjectByType<PathGenerator>();
        if (pathGenerator == null || attack == null || attack.rangeCollider == null)
        {
            pathCellsInRange = 0;
            return;
        }

        // Delegate overlap registration to path system (which also updates path difficulty)
        pathCellsInRange = pathGenerator.RegisterTowerOverlaps(this);
    }

    public int RecalculateCost()
    {
        cost = Mathf.RoundToInt(((attackDamage * attackSpeed * attackRadius) + health.currentHealth) * (specialityModifier + 1));
        if (cost < 0) cost = 0;
        return cost;
    }
}
