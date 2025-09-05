using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;      // Assign in inspector
    public float spawnInterval = 4f;    // Time in seconds between spawns
    public float waveInterval = 10f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemiesToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    private SubCell parentCell;
    private PathGenerator pathGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartWave();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
            Debug.Log("All waves completed!");
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

        GameObject enemy = Instantiate(enemyPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        enemy.GetComponent<EnemyManager>().pathFromSpawner = path;
        enemiesToSpawn--;
    } 
}
