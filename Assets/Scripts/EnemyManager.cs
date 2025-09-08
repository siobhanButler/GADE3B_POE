using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : ObjectManager
{
    [Header("Enemy Properties")]
    public EnemyMovement movement;
    public List<SubCell> pathFromSpawner;
    public int coinReward = 10;     //coins rewarded upon death

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
        movement = GetComponent<EnemyMovement>() ?? gameObject.AddComponent<EnemyMovement>();
        movement.Setup();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnDeath()
    {
        // Find the PlayerManager and add coins
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
                // Add coins when enemy dies (you can adjust the amount as needed)
                 playerManager.coins += coinReward;
        }
    }
}
