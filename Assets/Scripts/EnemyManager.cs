using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : ObjectManager
{
    [Header("Enemy Properties")]
    public EnemyMovement movement;
    public List<SubCell> pathFromSpawner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
        movement = GetComponent<EnemyMovement>() ?? gameObject.AddComponent<EnemyMovement>();
        movement.Setup();
    }

    // Update method removed - movement handled by EnemyMovement component
    void SetupEnemy(float attackModifier, float healthModifier, float speedModifier)
    {
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
                 playerManager.coins += Mathf.RoundToInt(cost * 0.5f);
        }
    }
}
