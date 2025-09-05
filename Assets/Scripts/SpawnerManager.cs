using System.Collections;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;      // Assign in inspector
    public float spawnInterval = 1.5f;    // Time in seconds between spawns
    public float waveInterval = 10f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemiesToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Setup();
        StartWave();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Setup()
    {
        currentWave = 0;
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
        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemiesToSpawn--;
    } 
}
