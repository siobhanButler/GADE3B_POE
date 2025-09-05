using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;  // Maximum health
    public float currentHealth;
    public bool isDead = false;     // Is the entity dead

    public ObjectUIManager healthBarUIManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Setup(ObjectUIManager p_healthBarUIManager)
    {
        //Default settings
        currentHealth = maxHealth;
        isDead = false;

        healthBarUIManager = p_healthBarUIManager;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;    //reduce currentHealth
        healthBarUIManager.UpdateHealthBar(currentHealth, maxHealth);  //update health bar UI

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        
        // Check if this object has the Enemy tag and add coins to player
        if (gameObject.CompareTag("Enemy"))
        {
            // Find the GameManager and add coins
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                // Add coins when enemy dies (you can adjust the amount as needed)
                gameManager.playerMoney += 10;
            }
        }
        
        // Destroy the object after a short delay to allow for any death animations
        Destroy(gameObject, 0.1f);
    }
}
