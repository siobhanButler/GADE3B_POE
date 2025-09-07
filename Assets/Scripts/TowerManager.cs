using UnityEngine;

public class TowerManager : ObjectManager
{
    public int cost = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
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
    }
}
