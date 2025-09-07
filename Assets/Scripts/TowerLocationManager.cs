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

    void OnMouseDown()
    {
        Debug.Log("OnMouseDown called!");
        
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
            Debug.Log("Location already occupied");
            return;
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
}
