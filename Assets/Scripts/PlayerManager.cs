using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int coins = 100;
    public float health = 100;    //from main tower health
    public Health mainTowerHealth;

    // Start method removed - initialization handled in Setup()

    // Update is called once per frame
    void Update()
    {
        UpdateHealth();
    }

    public void Setup(int pCoins)
    {
        coins = pCoins;
    }

    public void UpdateHealth()
    {
        //Update player/ main tower health
        if (mainTowerHealth == null)
        {
            GameObject mainTower = GameObject.FindWithTag("MainTower");
            if (mainTower != null)
            {
                mainTowerHealth = mainTower.GetComponent<Health>();
                if (mainTowerHealth != null)
                {
                    health = mainTowerHealth.currentHealth;
                }
            }
        }
        else
        {
            health = mainTowerHealth.currentHealth;
        }
    }
}
