using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] enemyPrefabs;      // Assign in inspector
    public float[] spawnLikelihoods;
    public float spawnInterval = 2f;    // Time in seconds between spawns
    public float waveInterval = 20f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemiesToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves
    public bool wavesCompleted = false;
    public int baseEnemyCost = 30;

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    private GameManager gameManager;
    private SubCell parentCell;
    private PathGenerator pathGenerator;
    PathObj pathObj;

    private List<EnemyManager> enemies = new List<EnemyManager>();
    private List<TowerManager> towers = new List<TowerManager>();      //towersOnPath
    private int enemyBudget;
    private int towerBudget;

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

        pathObj = pathGenerator.GetPathObjForSpawner(parentCell);

        gameManager = FindFirstObjectByType<GameManager>();
        // Sync wave count from GameManager (rounded from float-based progression)
        if (gameManager != null)
        {
            numberOfWaves = Mathf.Max(1, Mathf.RoundToInt(gameManager.wavesAmount));
            Debug.Log($"SpawnerManager: Starting wave {currentWave + 1}/{numberOfWaves} (wavesAmount={gameManager.wavesAmount:F2})");
        }
        else Debug.Log($"SpawnerManager: Starting wave {currentWave + 1}/{numberOfWaves} (GameManager null)");

        spawnLikelihoods = new float[enemyPrefabs.Length];
        for (int i = 0; i < spawnLikelihoods.Length; i++) 
        {
            spawnLikelihoods[i] = baseEnemyCost / enemyPrefabs[i].GetComponent<EnemyManager>().cost;
        }
    }

    public void StartWave()
    {
        if (enemyPrefabs == null)
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

        // Ensure towers are registered for this path before spawning begins
        EnsureTowersRegisteredOnPath();

        SetMinAndMaxEniemies();
        enemiesToSpawn = Random.Range(minEnemies, maxEnemies + 1);  //randomly assign how many enemies to spawn
        spawnCoroutine = StartCoroutine(SpawnRoutine());

        Debug.Log($"SpawnerManager StartWave(): min {minEnemies} and max {maxEnemies}, with enemiesToSpawn: {enemiesToSpawn}");
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
        enemiesToSpawn--;
        if (ShouldSpawnEnemy() == false) return;    //if should spawn enemy is false, return

        GameObject enemyPrefab = SelectEnemyWithinBudget();
        if (enemyPrefab == null) return;

        Debug.Log($"SpawnerManager SpawnEnemy(): Spawning enemy {enemyPrefab.name}");

        RefreshPathObj();         // Ensure fresh path object after paths are generated

        List<SubCell> path = pathObj.pathCells;
        if (path == null || path.Count == 0) Debug.LogWarning("SpawnerManager SpawnEnemy(): Path is null or empty; enemy will still spawn but may not move");

        Vector3 spawnPos = transform.position;
        spawnPos.y += 3f;
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        EnemyManager enemyManager = enemy.GetComponent<EnemyManager>();
        if (enemyManager != null)
        {
            enemyManager.pathFromSpawner = path;
            // Ensure core components are initialized prior to applying modifiers (mirror WaveManager)
            enemyManager.Setup();
            enemyManager.SetupEnemy(1f, 1f, 1f);
        }

        //Add spawned enemy to GameManager's and own enemy list
        gameManager.enemies.Add(enemy);
        enemies.Add(enemyManager);
    }

    bool ShouldSpawnEnemy() //(GameObject enemyToSpawn)
    {
        RefreshEnemyBudget();
        RefreshTowerBudget();

        if (enemyBudget + baseEnemyCost <= towerBudget)     //take base enemyCost (cheapest enemy) into account
        {
            Debug.Log("SpawnerManager ShouldSpawnEnemy: Addding an enemy is within budget.");
            return true;
        }
        else
        {
            Debug.Log("SpawnerManager ShouldSpawnEnemy: Addding an enemy is not within budget.");
            return false;
        }

        //int enemyCost = enemyToSpawn.GetComponent<EnemyManager>().cost;
        //if (enemyBudget + enemyCost < towerBudget)
    }

    GameObject SelectEnemyWithinBudget()
    {
        GameObject enemy = ChooseEnemyWeightedByLikelihood();  //randomly select enemy within prefabs
        if (enemyBudget + enemy.GetComponent<EnemyManager>().cost > towerBudget)
        {
            //apply modifiers to reduce enemy cost within range
            enemy = SelectEnemyWithinBudget();  //for now regenerate
        }
        return enemy;
    }

    GameObject ChooseEnemyWeightedByLikelihood()
    {
        // Compute total weight using adjusted likelihoods
        float total = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            float w = (spawnLikelihoods != null && i < spawnLikelihoods.Length) ? spawnLikelihoods[i] : 0f;
            if (w > 0f) total += w;
        }
        if (total <= 0f)
        {
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }
        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            float w = (spawnLikelihoods != null && i < spawnLikelihoods.Length) ? spawnLikelihoods[i] : 0f;
            if (w <= 0f) continue;
            acc += w;
            if (r <= acc) return enemyPrefabs[i];
        }
        return enemyPrefabs[enemyPrefabs.Length - 1];
    }

    void RefreshEnemyBudget()
    {
        RefreshEnemies();

        enemyBudget = 0;
        foreach (EnemyManager enemy in enemies)
        {
            if(enemy == null) continue;
            enemyBudget += enemy.GetCurrentCost();
        }

        Debug.Log($"SpawnerManager RefreshEnemyBudget(): Enemy Budget is: {enemyBudget}");
    }

    void RefreshTowerBudget()
    {
        RefreshTowers();

        towerBudget = 0;    //reset towerBudget
        foreach (TowerManager tower in towers)
        {
            if (tower == null) continue;
            towerBudget += tower.GetCurrentCost();
        }

        Debug.Log($"SpawnerManager RefreshTowerBudget(): Tower Budget is: {towerBudget}");
    }

    void RefreshEnemies()
    {
        //Remove any null or dead enemies
        List<EnemyManager> enemiesToRemove = new List<EnemyManager>();
        foreach (EnemyManager enemy in enemies)
        {
            if (enemy == null) enemiesToRemove.Add(enemy);
        }
        foreach(EnemyManager enemy in enemiesToRemove)
        {
            enemies.Remove(enemy);
        }
    }

    void RefreshTowers()
    {
        towers.Clear();
        RefreshPathObj();      

        if (pathObj.towers == null || pathObj.towers.Count == 0)
        {
            // Attempt to re-register towers defensively (handles timing between waves)
            EnsureTowersRegisteredOnPath();
            RefreshPathObj();
            if (pathObj.towers == null || pathObj.towers.Count == 0)
            {
                Debug.Log("SpawnerManager RefreshTowers(): No towers registered on path yet after re-registering");
                return;
            }
        }

        towers = pathObj.towers;
    }

    void RefreshPathObj()
    {
        // Re-fetch latest PathObj from generator because it's a struct and can become stale
        if (pathGenerator == null)
        {
            pathGenerator = GetComponent<PathGenerator>();
            if (pathGenerator == null) pathGenerator = GetComponentInParent<PathGenerator>();
        }
        if (pathGenerator != null && parentCell != null)
        {
            pathObj = pathGenerator.GetPathObjForSpawner(parentCell);
        }
    }

    void EnsureTowersRegisteredOnPath()
    {
        RefreshPathObj();
        if (pathGenerator == null) return;

        // Register main tower
        GameObject mainTowerObj = null;
        try { mainTowerObj = GameObject.FindGameObjectWithTag("MainTower"); } catch { mainTowerObj = null; }
        if (mainTowerObj != null)
        {
            TowerManager tm = mainTowerObj.GetComponent<TowerManager>();
            if (tm != null) pathGenerator.RegisterTowerOverlaps(tm);
        }

        // Register defence towers
        GameObject[] defenceTowers = null;
        try { defenceTowers = GameObject.FindGameObjectsWithTag("DefenceTower"); } catch { defenceTowers = null; }
        if (defenceTowers != null)
        {
            for (int i = 0; i < defenceTowers.Length; i++)
            {
                var go = defenceTowers[i];
                if (go == null) continue;
                TowerManager tm = go.GetComponent<TowerManager>();
                if (tm == null) continue;
                pathGenerator.RegisterTowerOverlaps(tm);
            }
        }
    }

    void SetMinAndMaxEniemies()
    {
        RefreshTowerBudget();

        minEnemies = Mathf.RoundToInt((towerBudget/baseEnemyCost) * 0.8f);      //TODO: Replace 0.8 and 1.2 with player adpatable difficulty modifier
        maxEnemies = Mathf.RoundToInt((towerBudget / baseEnemyCost) * 1.2f);
    }
}
