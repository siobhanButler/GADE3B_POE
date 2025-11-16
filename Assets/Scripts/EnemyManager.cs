using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : ObjectManager
{
    [Header("Enemy Properties")]
    public EnemyMovement movement;
    public List<SubCell> pathFromSpawner;

    public LootItem[] lootItems;
    [Header("Loot Settings")]
    public LootProperty preferredLootProperty = LootProperty.Health;

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
        LootManager lootManager = FindFirstObjectByType<LootManager>();
        if(lootManager != null) lootManager.GrantRewards(this);

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
