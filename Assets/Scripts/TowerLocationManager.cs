using UnityEngine;

public class TowerLocationManager : MonoBehaviour
{
    public GameObject towerPrefab;
    public int cost = 30;
    public GameManager gameManager;

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
        if (gameManager.playerMoney < cost) //can't afford :(
        {
            return;
        }
        gameManager.playerMoney -= cost;
        Instantiate(towerPrefab, transform.position, Quaternion.identity);
    }
}
