using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;      // Assign in inspector
    public float spawnInterval = 4f;    // Time in seconds between spawns
    public float waveInterval = 20f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemiesToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves
    public bool wavesCompleted = false;

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    private SubCell parentCell;
    private PathGenerator pathGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartWave();
    }

    // Update method removed - spawning handled by coroutines

    public void Setup(SubCell parent)
    {
        parentCell = parent;
        currentWave = 0;
        
        // Get reference to PathGenerator (should be on the same GameObject or parent)
        pathGenerator = GetComponent<PathGenerator>();
        if (pathGenerator == null)
        {
            pathGenerator = GetComponentInParent<PathGenerator>();
        }
        
        if (pathGenerator == null)
        {
            Debug.LogError("PathGenerator not found! SpawnerManager needs a PathGenerator to function properly.");
        }
    }

    public void StartWave()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("SpawnerManager StartWave(): enemyPrefab is not assigned");
            return;
        }

        if (minEnemies < 0 || maxEnemies < minEnemies)
        {
            Debug.LogWarning("SpawnerManager StartWave(): Invalid enemy count range; correcting values");
            minEnemies = Mathf.Max(0, minEnemies);
            maxEnemies = Mathf.Max(minEnemies, maxEnemies);
        }

        enemiesToSpawn = Random.Range(minEnemies, maxEnemies + 1);  //randomly assign how many enemies to spawn
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // Spawn all enemies for this wave
        while (enemiesToSpawn > 0)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
        
        // Wave completed, wait for next wave
        currentWave++;
        if (currentWave < numberOfWaves)
        {
            yield return new WaitForSeconds(waveInterval); // pause between waves
            StartWave();
        }
        else
        {
            wavesCompleted = true;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.WinGame();
            }
        }
    }

    public void SpawnEnemy()
    {
        if (pathGenerator == null)
        {
            Debug.LogError("PathGenerator is null! Cannot spawn enemy without path.");
            return;
        }
        
        List<SubCell> path = pathGenerator.GetPathForSpawner(parentCell);
        if (path == null || path.Count == 0) Debug.LogWarning("SpawnerManager SpawnEnemy(): Path is null or empty; enemy will still spawn but may not move");

        Vector3 spawnPos = transform.position;
        spawnPos.y += 3f;
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        EnemyManager enemyManager = enemy.GetComponent<EnemyManager>();
        if (enemyManager != null) enemyManager.pathFromSpawner = path;
        else Debug.LogWarning("SpawnerManager SpawnEnemy(): EnemyManager component missing on enemy prefab");
        enemiesToSpawn--;

        //Add spawned enemy to GameManager's enemy list
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.enemies.Add(enemy);
        }
        else{
            Debug.LogWarning("SpawnerManager SpawnEnemy(): GameManager is null, cannot add enemy to list");
        }
    } 
}
