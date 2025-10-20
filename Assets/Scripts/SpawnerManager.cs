using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] enemyPrefabs;      // Assign in inspector
    public float[] spawnLikelihoods;
    public float spawnInterval = 3f;    // Time in seconds between spawns
    public float waveInterval = 20f;   // Time in seconds between waves
    public int minEnemies = 5;          // Minimum number of enemies to spawn
    public int maxEnemies = 20;         // Maximum number of enemies to spawn
    public int enemiesToSpawn;          // Number of enemies to spawn
    public int numberOfWaves = 3;       // Total number of waves
    public bool wavesCompleted = false;
    public int baseEnemyCost = 30;
public float difficultyModifier = 0.5f;

    private Coroutine spawnCoroutine;
    private int currentWave = 0;

    private GameManager gameManager;
    private TowerManager mainTower;
    private SubCell parentCell;
    private PathGenerator pathGenerator;
    PathObj pathObj;

    private List<EnemyManager> enemies = new List<EnemyManager>();
    private List<TowerManager> towers = new List<TowerManager>();      //towersOnPath
    private int enemyBudget;
    private int towerBudget;
    private Health mainTowerHealth;

    private float difficulty        //=> enemyDamageTaken / towerDamageTaken; (difficulty of the wave)
    {
        get
        {
            float d = (towerDamageTaken > 0f) ? (enemyDamageTaken / towerDamageTaken) : 1f; // Neutral difficulty if no data yet
            if (float.IsNaN(d) || float.IsInfinity(d)) return 1f;
            // Clamp difficulty based on wave: wave 1 => [0.5, 1.5], wave 2 => [0.1, 2.0], and so on
            int waveIndex = Mathf.Max(1, currentWave + 1); // currentWave is 0-based while spawning
            float minAllowed = Mathf.Max(0.1f, 1f - (0.5f + 0.4f * (waveIndex - 1)));
            float maxAllowed = 1f + (0.5f + 0.5f * (waveIndex - 1));
            return Mathf.Clamp(d, minAllowed, maxAllowed);
        }
    }
    private float towerDamageTaken = 1f;
    private float enemyDamageTaken = 1f;
    private int towerDeathCount = 1;
    private int enemyDeathCount = 1;

// Start is called once before the first execution of Update after the MonoBehaviour is created
void Start()
    {
        EnsureMainTowerHealth();
        numberOfWaves = numberOfWaves * gameManager.currentLevel;
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
        EnsureMainTowerHealth();
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
            // Single calc: add 0.5s per 5% health lost = floor((1 - frac)*20)*0.5
            float interval = spawnInterval;
            if (mainTowerHealth == null) EnsureMainTowerHealth();
            if (mainTowerHealth != null && mainTowerHealth.maxHealth > 0f)
            {
                interval += Mathf.FloorToInt((1f - (mainTowerHealth.currentHealth / mainTowerHealth.maxHealth)) * 20f) * 0.5f;
            }

            Debug.Log($"SpawnerManager SpawnRoutine(): Pausing between waves for {interval} seconds.");
            // scale spawn interval based on tower health; every 5% lost adds +0.5 sec
            yield return new WaitForSeconds(interval);
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

        //Debug.Log($"SpawnerManager SpawnEnemy(): Spawning enemy {enemyPrefab.name}");

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
            var speedMod = Mathf.Clamp((1f + (difficulty - 1f) * 0.5f), 0.1f, 2f); // half as effective
            enemyManager.SetupEnemy(difficulty, difficulty , speedMod);
            Debug.Log($"SpawnerManager CalculateDifficulty: difficulty = {difficulty} = enemyDamageTaken / towerDamageTaken = {enemyDamageTaken} / {towerDamageTaken}");
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
        GameObject enemy = null;
        for (int attempt = 0; attempt <= 10; attempt++)
        {
            enemy = ChooseEnemyWeightedByLikelihood();  //randomly select enemy from prefabs
            if (enemyBudget + enemy.GetComponent<EnemyManager>().cost <= towerBudget)
            {
                //ELSE: apply modifiers to reduce enemy cost within range
                return enemy;
            }
        }
        return enemyPrefabs[0];
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

    int RefreshTowerBudget()
    {
        RefreshTowers();

        towerBudget = 0;    //reset towerBudget
        foreach (TowerManager tower in towers)
        {
            if (tower == null) continue;
            towerBudget += tower.GetCurrentCost() / tower.pathsInRange;     //if tower is on multiple paths, divide cost by number of paths
        }

        // Distribute player coins across all paths
        int totalPaths = 0;
        if (pathGenerator != null)
        {
            var allPaths = pathGenerator.GetAllPathObjects();
            totalPaths = (allPaths != null) ? allPaths.Count : 0;
        }
        if (gameManager != null && gameManager.playerManager != null && totalPaths > 0)
        {
            towerBudget += Mathf.RoundToInt((float)gameManager.playerManager.coins / (float)totalPaths);
        }
        Debug.Log($"SpawnerManager RefreshTowerBudget(): Tower Budget is: {towerBudget}");
        return towerBudget;
    }

    void RefreshEnemies()
    {
        //Remove any null or dead enemies
        List<EnemyManager> enemiesToRemove = new List<EnemyManager>();
        foreach (EnemyManager enemy in enemies)
        {
            if (enemy == null) enemiesToRemove.Add(enemy);
            else {
                enemy.health.OnDamageTaken += OnEnemyDamageTaken;  // add listeners
                enemy.OnDeathEvent += OnEnemyDeath;                     //add listeners
            }
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

        foreach (TowerManager tower in towers) 
        {
            tower.health.OnDamageTaken += OnTowerDamageTaken;  //add listener
            tower.OnDeathEvent += OnTowerDeath;
        }
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

    void EnsureMainTowerHealth()
    {
        // Prefer cached from GameManager's playerManager
        if (mainTowerHealth == null && gameManager != null && gameManager.playerManager != null)
        {
            mainTowerHealth = gameManager.playerManager.mainTowerHealth;
            if (mainTowerHealth != null) return;
        }
        // Fallback: find by tag in scene
        try
        {
            GameObject mainTowerObj = GameObject.FindGameObjectWithTag("MainTower");
            if (mainTowerObj != null)
            {
                mainTowerHealth = mainTowerObj.GetComponent<Health>();
                if (mainTowerHealth == null) mainTowerHealth = mainTowerObj.GetComponentInChildren<Health>();
            }
        }
        catch { /* ignore */ }
    }

    void SetMinAndMaxEniemies()
    {
        RefreshTowerBudget();

        minEnemies = Mathf.RoundToInt((towerBudget/baseEnemyCost) * 0.8f);      //TODO: Replace 0.8 and 1.2 with player adpatable difficulty modifier
        maxEnemies = Mathf.RoundToInt((towerBudget / baseEnemyCost) * 1.2f);
    }

    void OnEnemyDamageTaken(float damage, Health health)
    {
        enemyDamageTaken += damage / health.maxHealth;      
    }

    void OnTowerDamageTaken(float damage, Health health)
    {
        towerDamageTaken += damage / health.maxHealth;
    }

    void OnEnemyDeath(ObjectManager objectManager)
    {
        enemyDeathCount++;
        if (objectManager is EnemyManager enemy)
        {
            enemies.Remove(enemy);
            enemy.OnDeathEvent -= OnEnemyDeath;
            enemy.health.OnDamageTaken -= OnEnemyDamageTaken;
        }
    }

    void OnTowerDeath(ObjectManager objectManager)
    {
        towerDeathCount++;
        if (objectManager is TowerManager tower)
        {
            towers.Remove(tower);
            tower.OnDeathEvent -= OnTowerDeath;
            tower.health.OnDamageTaken -= OnTowerDamageTaken;
        }
    }
}
