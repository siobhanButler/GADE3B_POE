using UnityEngine;

public class TowerManager : ObjectManager
{
    public int cost = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
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
            Destroy(gameObject);
        }
    }
}
