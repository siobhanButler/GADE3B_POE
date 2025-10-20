using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : ObjectManager
{
    [Header("Enemy Properties")]
    public EnemyMovement movement;
    public List<SubCell> pathFromSpawner;

    public event Action<int> OnEnemyDeath;
    bool initialized = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureInitialized();
    }

    // Update method removed - movement handled by EnemyMovement component
    public void SetupEnemy(float attackModifier, float healthModifier, float speedModifier)
    {
        EnsureInitialized();
        movement.speed *= speedModifier;
        health.maxHealth *= healthModifier;
        attack.attackDamage *= attackModifier;
    }

    public override void OnDeath()
    {
        // Find the PlayerManager and add coins
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
                // Add coins when enemy dies (you can adjust the amount as needed)
                 playerManager.coins += Mathf.RoundToInt(cost);
        }

        OnEnemyDeath?.Invoke(movement != null ? movement.currentPathIndex : 0);
    }

    void EnsureInitialized()
    {
        if (!initialized)
        {
            Setup();
            initialized = true;
        }
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>() ?? gameObject.AddComponent<EnemyMovement>();
            movement.Setup();
        }
    }
}
