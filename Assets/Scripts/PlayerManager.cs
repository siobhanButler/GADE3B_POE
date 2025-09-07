using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int coins = 100;
    public float health = 100;    //from main tower health
    public Health mainTowerHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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

    public void Setup()
    {

    }

    public void UpdateHealth()
    {
        
    }
}
