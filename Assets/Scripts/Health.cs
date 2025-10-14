using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;  // Maximum health
    public float currentHealth;
    public bool isDead = false;     // Is the entity dead

    public ObjectUIManager healthBarUIManager;

    // Raised whenever this entity takes damage. Args: (damageTaken, currentHealth, maxHealth)
    public event Action<float, Health> OnDamageTaken;

    // Start and Update methods removed - initialization handled in Setup()

    public void Setup(ObjectUIManager p_healthBarUIManager, float pMaxHealth)
    {
        maxHealth = pMaxHealth;
        //Default settings
        currentHealth = maxHealth;
        isDead = false;

        healthBarUIManager = p_healthBarUIManager;
    }

    public void TakeDamage(float damage)
    {
        if (OnDamageTaken != null)  //if there are listeners 
        {
            // Let listeners handle damage routing (shielding, transfers, etc.)
            OnDamageTaken.Invoke(damage, this);
        }
        else
        {
            // No listeners: apply default damage
            ApplyRawDamage(damage);
        }
    }

    // Apply damage without raising events (used by systems that already handle routing)
    public void ApplyRawDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth < 0f) currentHealth = 0f;
        healthBarUIManager.UpdateHealthBar(currentHealth, maxHealth);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return; // Cannot heal if dead

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        healthBarUIManager.UpdateHealthBar(currentHealth, maxHealth);  //update health bar UI
    }

    void Die()
    {
        isDead = true;
        GetComponentInParent<ObjectManager>()?.OnDeath();
        
        // Destroy the object after a short delay to allow for any death animations
        Destroy(gameObject, 0.1f);
    }
}
