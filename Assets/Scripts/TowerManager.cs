using UnityEngine;

public class TowerManager : ObjectManager, IClickable
{
    public GameObject attackRangeSphere;
    public int pathCellsInRange;
    public int pathsInRange;
    public int level = 0;

    public towerUpgradeManager towerUpgradeManager;
    public UpgradeRequirement[] upgradeRequirements;
    public Mesh[] levelMeshes;  //corresponds to the level
    private GameManager gameManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
        ComputePathCellsInRange();

        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null) Debug.LogError("TowerManager Start(): GameManager not found in scene");

        if(towerUpgradeManager == null) towerUpgradeManager = FindFirstObjectByType<towerUpgradeManager>();
        if (towerUpgradeManager == null) Debug.LogError("TowerManager Start(): TowerUpgradeManager not found");

        gameObject.layer = LayerMask.NameToLayer("Clickable");

        // Ensure the range sphere collider does not intercept click raycasts
        if (attack != null && attack.rangeCollider != null)
        {
            attack.rangeCollider.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        level = 0;  //start/default level is always 1

        transform.position = transform.position + new Vector3(0, -3, 0);
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
            pathsInRange = 0;
            return;
        }

        // Delegate overlap registration to path system (which also updates path difficulty)
        pathCellsInRange = pathGenerator.RegisterTowerOverlaps(this);
        pathsInRange = pathGenerator.GetTowerPathCount(this);
    }

    public int RecalculateCost()
    {
        cost = Mathf.RoundToInt(((attackDamage * attackSpeed * attackRadius) + health.currentHealth) * (specialityModifier + 1));
        if (cost < 0) cost = 0;
        return cost;
    }

    public void OnClick()
    {
        Debug.Log($"TowerManager OnClick(): {name} was clicked!");
        if (towerUpgradeManager == null) return;
        towerUpgradeManager.UpgradeTower(this);
    }

    public void UpdateStats()
    {
        health.maxHealth = maxHealth;
        attack.attackDamage = attackDamage;
        attack.attackSpeed = attackSpeed;
        attack.rangeRadius = attackRadius;
        attack.rangeCollider.radius = attackRadius;
        attackRangeSphere.transform.localScale = new Vector3(attackRadius * 2, attackRadius * 2, attackRadius * 2);

        //swap out to the new mesh
        MeshFilter meshFilter =  GetComponent<MeshFilter>();
        if (meshFilter != null) meshFilter.mesh = levelMeshes[level];
    }
}
