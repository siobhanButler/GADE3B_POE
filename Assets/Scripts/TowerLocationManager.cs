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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        Debug.Log($"{name} was clicked!");
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.uiManager.EnableTowerLocationPanel(true, this);
        }
    }

    public void PurchaseTower(GameObject towerPrefab)
    {
        int cost = towerPrefab.GetComponent<TowerManager>().cost;
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
            return tower.GetComponent<Health>().isDead;
        }
        else
        {
            return true;        //tower was destroyed, so its dead
        }
    }
}
