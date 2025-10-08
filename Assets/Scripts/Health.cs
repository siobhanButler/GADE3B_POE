using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;  // Maximum health
    public float currentHealth;
    public bool isDead = false;     // Is the entity dead

    public ObjectUIManager healthBarUIManager;

    // Start and Update methods removed - initialization handled in Setup()

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
        GetComponentInParent<ObjectManager>()?.OnDeath();
        
        // Destroy the object after a short delay to allow for any death animations
        Destroy(gameObject, 0.1f);
    }
}
