using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] enemyPrefabs;      // Assign in inspector
    public float spawnInterval = 4f;    // Time in seconds between spawns
    public float waveInterval = 20f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemyQuantityToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves
    public bool wavesCompleted = false;

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    private SubCell parentCell;
    private PathGenerator pathGenerator;
    private GameManager gameManager;
    private float prevWaveStartHealth = -1f;
    private float waveStartTime = 0f;
    public float waveCompletionTime = 0f;
    private bool waveSpawnFinished = false;
    private List<GameObject> currentWaveEnemies = new List<GameObject>();

    public int enemyBudget = 100;
    public List<GameObject> enemiesToSpawn = new List<GameObject>();
    PathObj pathObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemyPrefabs[0].GetComponent<ObjectManager>() == null) Debug.LogError("nope");
        StartCoroutine(StartWaveWhenReady());

        gameManager = FindFirstObjectByType<GameManager>();
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

        

        enemyQuantityToSpawn = Random.Range(minEnemies, maxEnemies + 1);  //randomly assign how many enemies to spawn

        // Initialize wave tracking
        waveCompletionTime = 0f;
        waveStartTime = Time.time;
        waveSpawnFinished = false;
        if (currentWaveEnemies == null) currentWaveEnemies = new List<GameObject>();
        currentWaveEnemies.Clear();

        SetEnemiesToSpawn();
        // Begin timing coroutine
        StartCoroutine(TrackWaveCompletion());
        // Snapshot player health at the start of this wave for next wave's difficulty scaling
        prevWaveStartHealth = GetCurrentPlayerHealth();
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator StartWaveWhenReady()
    {
        // Wait until pathObj has towers registered or a short timeout
        int framesToWait = 180; // ~3 seconds at 60 FPS
        while (framesToWait-- > 0)
        {
            RefreshPathObj();
            if (pathObj.towers != null && pathObj.towers.Count > 0) break;
            yield return null;
        }
        StartWave();
    }

    IEnumerator SpawnRoutine()
    {
        // Spawn all enemies for this wave
        while (enemiesToSpawn.Count > 0)
        {
            SpawnEnemy(0);   //choose random enemy type to spawn
            yield return new WaitForSeconds(spawnInterval);
        }
        // Mark that this wave finished spawning
        waveSpawnFinished = true;
        
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
                // Increase waves amount for future levels based on player performance (health fraction)
                if (gameManager.playerManager != null && gameManager.playerManager.mainTowerHealth != null)
                {
                    float maxH = Mathf.Max(0.0001f, gameManager.playerManager.mainTowerHealth.maxHealth);
                    float frac = Mathf.Clamp01(gameManager.playerManager.mainTowerHealth.currentHealth / maxH);
                    gameManager.wavesAmount += frac;
                    Debug.Log($"SpawnerManager: Adjusted GameManager.wavesAmount by performance {frac:F2}. New wavesAmount={gameManager.wavesAmount:F2}");
                }
            }
        }
    }

    public void SpawnEnemy(int index)
    {
        if (pathGenerator == null)
        {
            Debug.LogError("PathGenerator is null! Cannot spawn enemy without path.");
            return;
        }

        // Ensure pathObj is up to date (paths may be generated after Setup)
        if (pathObj.pathCells == null || pathObj.pathCells.Count == 0)
        {
            pathObj = pathGenerator.GetPathObjForSpawner(parentCell);
        }

        List<SubCell> path = pathObj.pathCells;
        if (path == null || path.Count == 0) Debug.LogWarning("SpawnerManager SpawnEnemy(): Path is null or empty; enemy will still spawn but may not move");

        Vector3 spawnPos = transform.position;
        spawnPos.y += 3f;
        GameObject enemy = Instantiate(enemiesToSpawn[index], spawnPos, Quaternion.identity);
        EnemyManager enemyManager = enemy.GetComponent<EnemyManager>();
        if (enemyManager != null) enemyManager.pathFromSpawner = path;
        else Debug.LogWarning("SpawnerManager SpawnEnemy(): EnemyManager component missing on enemy prefab");
        // Track enemy for this wave's completion timing
        if (currentWaveEnemies != null) currentWaveEnemies.Add(enemy);
        enemiesToSpawn.RemoveAt(index);

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

    void SetEnemiesToSpawn()
    {
        enemyBudget = GetEnemyBudget();
        enemiesToSpawn.Clear();
        GameObject enemy = null;
        while (GetTotalEnemyCost(enemy) < enemyBudget)
        {
            enemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];  //select random enemy
            enemiesToSpawn.Add(enemy);
        }

        enemyQuantityToSpawn = enemiesToSpawn.Count;

        string enemyNames = "";
        foreach (GameObject eenemy in enemiesToSpawn)
        {
            enemyNames += enemy.name;
            enemyNames += " ";
        }
        Debug.Log($"SpawnerManager SetEnemiesToSpawn(): Actual Budget: {GetTotalEnemyCost(null)} \n Names: {enemyNames}");
    }

    int GetTotalEnemyCost(GameObject newEnemy)
    {
        int totalCost = 0;
        foreach (GameObject enemy in enemiesToSpawn)
        {
            totalCost += enemy.GetComponent<ObjectManager>().cost;
        }
        if(newEnemy != null) totalCost += newEnemy.GetComponent<ObjectManager>().cost;
        return totalCost;
    }

    int GetTotalTowerCost()
    {
        float totalTowerCost = 0f;
        RefreshPathObj();
        if (pathObj.towers == null || pathObj.towers.Count == 0) return 0;

        // Build a map of how many paths each tower participates in
        Dictionary<TowerManager, int> towerPathCounts = new Dictionary<TowerManager, int>();
        if (pathGenerator != null)
        {
            List<PathObj> allPaths = pathGenerator.GetAllPathObjects();
            if (allPaths != null)
            {
                for (int i = 0; i < allPaths.Count; i++)
                {
                    var p = allPaths[i];
                    if (p.towers == null) continue;
                    for (int t = 0; t < p.towers.Count; t++)
                    {
                        var tw = p.towers[t];
                        if (tw == null) continue;
                        if (towerPathCounts.ContainsKey(tw)) towerPathCounts[tw]++;
                        else towerPathCounts[tw] = 1;
                    }
                }
            }
        }

        // Sum each tower's cost divided by the number of paths it covers
        for (int i = 0; i < pathObj.towers.Count; i++)
        {
            TowerManager tower = pathObj.towers[i];
            if (tower == null) continue;
            int fullCost = tower.RecalculateCost();
            int pathCount = 1;
            if (towerPathCounts.TryGetValue(tower, out int c)) pathCount = Mathf.Max(1, c);
            totalTowerCost += (float)fullCost / (float)pathCount;
        }

        return Mathf.RoundToInt(totalTowerCost);
    }

    void RefreshPathObj()
    {
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

    int GetEnemyBudget()
    {
        RefreshPathObj();
        int baseBudget = GetTotalTowerCost();
        // Compute difficulty factor as average overlaps per path cell using float math
        float difficultyFactor = 1f;
        int cellCount = (pathObj.pathCells != null) ? pathObj.pathCells.Count : 0;
        if (cellCount > 0)
        {
            difficultyFactor = (float)pathObj.totalPathDifficulty / (float)cellCount;
            // Ensure we never zero-out the budget due to low difficulty
            difficultyFactor = Mathf.Max(0.25f, difficultyFactor);
        }
        // Ensure enemyBudget is at least 1
        int budget = Mathf.Max(1, Mathf.RoundToInt(baseBudget * difficultyFactor * GetPlayerHealth()));
        return budget;
    }

    IEnumerator TrackWaveCompletion()
    {
        // Wait until all enemies spawned for this wave have been destroyed
        // Start time already captured in waveStartTime
        while (true)
        {
            bool allDead = true;
            if (currentWaveEnemies != null)
            {
                for (int i = 0; i < currentWaveEnemies.Count; i++)
                {
                    var e = currentWaveEnemies[i];
                    if (e)
                    {
                        allDead = false;
                        break;
                    }
                }
            }
            if (waveSpawnFinished && allDead)
            {
                waveCompletionTime = Mathf.Max(0f, Time.time - waveStartTime);
                int denom = Mathf.Max(1, enemyQuantityToSpawn);
                spawnInterval = (waveCompletionTime / denom) / GetPlayerHealth();
                Debug.Log($"SpawnerManager: Wave {currentWave} complete. waveCompletionTime={waveCompletionTime:F2}s, enemies={denom}, next spawnInterval={spawnInterval:F2}s");
                yield break;
            }
            yield return null;
        }
    }

    float GetPlayerHealth()
    {
        // Piecewise scaling: no loss => >1, loss >= 25% => <1, otherwise neutral 1.
        if (gameManager == null || gameManager.playerManager == null || gameManager.playerManager.mainTowerHealth == null) return 1f;
        float current = gameManager.playerManager.mainTowerHealth.currentHealth;
        // For the first wave (or if no snapshot yet), keep multiplier neutral
        if (currentWave == 0 || prevWaveStartHealth <= 0f) return 1f;
        float denom = Mathf.Max(prevWaveStartHealth, 0.0001f);
        float damageFraction = Mathf.Clamp01((prevWaveStartHealth - current) / denom);
        // Treat very small differences as no change
        if (damageFraction <= 0.05f) return 1.85f;   // > 1 when no/little health lost
        if (damageFraction >= 0.25f) return 0.85f;    // < 1 when lost >= 25%
        return 1f;                                    // neutral otherwise
    }

    float GetCurrentPlayerHealth()
    {
        if (gameManager == null || gameManager.playerManager == null || gameManager.playerManager.mainTowerHealth == null) return 0f;
        return gameManager.playerManager.mainTowerHealth.currentHealth;
    }
}
