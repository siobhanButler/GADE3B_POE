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
        if (isDead) return;
        // Always apply damage, then notify listeners for tracking or secondary effects
        ApplyRawDamage(damage);
        OnDamageTaken?.Invoke(damage, this);
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
        var om = GetComponentInParent<ObjectManager>();
        if (om != null)
        {
            om.OnDeath();
            // Raise event from within the type via protected helper
            var type = om.GetType();
            // call protected method via dynamic dispatch
            (om as ObjectManager).GetType().GetMethod("RaiseOnDeathEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(om, null);
        }
        
        // Destroy the object after a short delay to allow for any death animations
        Destroy(gameObject, 0.1f);
    }
}
