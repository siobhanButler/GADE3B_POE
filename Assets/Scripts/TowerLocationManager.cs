using UnityEngine;

public class TowerLocationManager : MonoBehaviour, IClickable
{
    public GameManager gameManager;
    public GameObject tower;
    public bool isOccupied = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("TowerLocationManager is active: " + gameObject.activeInHierarchy);
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            if (gameManager == null) Debug.LogError("TowerLocationManager Start(): GameManager component missing on 'GameManager' object");
    }

    // Update method removed - no per-frame logic needed

    public void OnClick()
    {
        Debug.Log($"{name} was clicked!");
        if (gameManager != null && gameManager.uiManager != null)
        {
            gameManager.uiManager.EnableTowerLocationPanel(true, this);
        }
        else Debug.LogWarning("TowerLocationManager OnClick(): GameManager or UIManager not available");
    }

    public void PurchaseTower(GameObject towerPrefab)
    {
        TowerManager towerManager = towerPrefab.GetComponent<TowerManager>();

        if (towerPrefab == null || towerManager == null || gameManager == null || gameManager.playerManager == null)
        {
            Debug.LogError("TowerLocationManager PurchaseTower(): towerPrefab is null, towerManager is null, gameManager is null, or gameManager.playerManager is null");
            return;
        }

        int cost = towerManager.cost;
        if (isOccupied)
        {
            if(!isTowerDead())  //if tower is not dead (check here so it doesnt need to be run in update and the tower doesnt have to communicate back)
            {
                Debug.Log("Location already occupied");
                return;
            }
            else
            {
                tower = null;
                isOccupied = false;
            }
            
        }

        if (gameManager.playerManager.coins < cost) //can't afford :(
        {
            return;
        }
        gameManager.playerManager.coins -= cost;
        Vector3 spawnPos = transform.position;
        spawnPos.y += 3f;
        tower = Instantiate(towerPrefab, spawnPos, Quaternion.identity);
        
        isOccupied = true;
    }

    void OnDestroy()
    {
        // Clean up when the tower location is destroyed
        if (tower != null)
        {
            Destroy(tower);
        }
    }

    bool isTowerDead(){
        if(tower != null && tower)
        {
            Health health = tower.GetComponent<Health>();
            if (health == null)
            {   Debug.LogError("TowerLocationManager isTowerDead(): Health component missing on tower"); 
                return true; 
            }
            return health.isDead;
        }
        else
        {
            return true;        //tower was destroyed, so its dead
        }
    }
}
