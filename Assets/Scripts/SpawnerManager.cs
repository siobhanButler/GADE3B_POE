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
    int wavesUndamaged = 0;
    int totalEnemyPathIndex = 0;
	int enemiesDiedCount = 0;

    float playerSkill= 0f;
    float attackModifier;
    float healthModifier;
    float speedModifier;

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
        totalEnemyPathIndex = 0;
		enemiesDiedCount = 0;

		// Snapshot player health at the start of this wave for next wave's difficulty scaling
		prevWaveStartHealth = GetCurrentPlayerHealth();
		SetEnemiesToSpawn();
		// Begin timing coroutine
		StartCoroutine(TrackWaveCompletion());
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
		if (enemyManager != null)
        {
			enemyManager.pathFromSpawner = path;
			// Ensure core components (health/attack/UI) are initialized before applying modifiers
			enemyManager.Setup();
			// Recompute modifiers at spawn time
			RecalculateModifiers();
			// Ensure movement exists before applying speed modifier
			EnemyMovement mv = enemy.GetComponent<EnemyMovement>();
			if (mv == null) mv = enemy.AddComponent<EnemyMovement>();
			enemyManager.movement = mv;
			mv.Setup();
			enemyManager.SetupEnemy(attackModifier, healthModifier, speedModifier * 0.5f);
			enemyManager.OnEnemyDeath += OnEnemyDeath;
        }
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
            enemy = ChooseEnemyWeightedByLikelihood();  // weighted random selection
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

    GameObject ChooseEnemyWeightedByLikelihood()
    {
        // Compute total weight
        float total = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            var om = enemyPrefabs[i] != null ? enemyPrefabs[i].GetComponent<ObjectManager>() : null;
            if (om != null && om.spawnLikelihood > 0f) total += om.spawnLikelihood;
        }
        if (total <= 0f)
        {
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }
        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            var om = enemyPrefabs[i] != null ? enemyPrefabs[i].GetComponent<ObjectManager>() : null;
            if (om == null || om.spawnLikelihood <= 0f) continue;
            acc += om.spawnLikelihood;
            if (r <= acc) return enemyPrefabs[i];
        }
        return enemyPrefabs[enemyPrefabs.Length - 1];
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

		// Add player's coins contribution: coins divided by number of tower locations
		int coinBonus = 0;
		if (gameManager != null && gameManager.playerManager != null)
		{
			int locationCount = 0;
			// Prefer tag if available; otherwise fall back to component search
			GameObject[] tagged = null;
			try { tagged = GameObject.FindGameObjectsWithTag("TowerLocation"); } catch { tagged = null; }
			if (tagged != null && tagged.Length > 0) locationCount = tagged.Length;
			else locationCount = FindObjectsByType<TowerLocationManager>(FindObjectsSortMode.None).Length;
			if (locationCount > 0)
			{
				coinBonus = Mathf.RoundToInt((float)gameManager.playerManager.coins / (float)locationCount);
			}
		}

		int finalBudget = Mathf.RoundToInt(totalTowerCost) + coinBonus;
		return finalBudget;
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
            difficultyFactor = Mathf.Max(0.75f, difficultyFactor);
        }
        // Modify likelihoods this wave based on player style for this path
        ApplyPlayerStyleWeights();
        // Ensure enemyBudget is at least 1
        playerSkill = GetPlayerHealth();
        int budget = Mathf.Max(1, Mathf.RoundToInt(baseBudget * difficultyFactor * playerSkill));
        Debug.Log($"SpawnerManager GetEnemyBudget(): Enemy Budget is: {budget}, based on baseBudget {baseBudget} * {difficultyFactor} * {playerSkill}");
        return budget;
    }

    enum PlayerStyle { Neutral, Offensive, Defensive }

    void ApplyPlayerStyleWeights()
    {
        PlayerStyle style = GetPlayerStyleForPath();
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject prefab = enemyPrefabs[i];
            if (prefab == null) continue;
            var om = prefab.GetComponent<ObjectManager>();
            if (om == null) continue;
            float baseLike = Mathf.Max(0f, om.spawnLikelihood);
            // x mapping: 0->neutral/plain, 1->offensive/tank, 2->defensive/hypnosis
            if (i == 0 && style == PlayerStyle.Neutral) om.spawnLikelihood = baseLike * 1.5f;
            else if (i == 1 && style == PlayerStyle.Offensive) om.spawnLikelihood = baseLike * 1.5f;
            else if (i == 2 && style == PlayerStyle.Defensive) om.spawnLikelihood = baseLike * 1.5f;
            else om.spawnLikelihood = baseLike * 1f;
        }
        
    }

    PlayerStyle GetPlayerStyleForPath()
    {
        // Sum all towers' max health and offensive power along this path
        float totalMaxHealth = 0f;
        float totalOffense = 0f;
        if (pathObj.towers != null)
        {
            for (int i = 0; i < pathObj.towers.Count; i++)
            {
                var tw = pathObj.towers[i];
                if (tw == null) continue;
                // max health
                if (tw.health != null) totalMaxHealth += Mathf.Max(0f, tw.health.maxHealth);
                else totalMaxHealth += Mathf.Max(0f, tw.maxHealth);
                // offensive power proxy
                if (tw.attack != null) totalOffense += Mathf.Max(0f, tw.attack.attackDamage * tw.attack.attackSpeed * tw.attack.rangeRadius);
                else totalOffense += Mathf.Max(0f, tw.attackDamage * tw.attackSpeed * tw.attackRadius);
            }
        }

		attackModifier = (totalOffense > 0f) ? (totalMaxHealth / totalOffense) : 1f;     // >1 means more health than offense so enemies get a damage boost
		healthModifier = (totalMaxHealth > 0f) ? (totalOffense / totalMaxHealth) : 1f;     // >1 means more offense than health so enemies get a health boost
		// Speed from average completion rate
		float avgCompletion = GetAveragePathCompletionRate();
		speedModifier = Mathf.Lerp(0.8f, 1.2f, avgCompletion) * Mathf.Clamp(playerSkill, 0.5f, 2.5f);

        // Decide style
        if (totalMaxHealth > totalOffense) return PlayerStyle.Defensive;
        if (totalOffense > totalMaxHealth) return PlayerStyle.Offensive;
        return PlayerStyle.Neutral;
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
                spawnInterval = (waveCompletionTime / denom) * 0.3f;
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
        float multiplier = 1f; //Mathf.Pow(y,x) y^x Mathf.Exp(x) is e^x
        if (damageFraction < 0.2f)
        {
            multiplier = -Mathf.Exp(8.04f) * Mathf.Pow(damageFraction - 0.2f, 5f) + 1f;   // > 1 when no/little health lost
            wavesUndamaged++;
        }
        else wavesUndamaged = 0;

        if (damageFraction == 0.2f) multiplier = 1f;
        if (damageFraction > 0.2f) multiplier = - Mathf.Exp(1.1f) * Mathf.Pow(damageFraction - 0.2f, 5f) + 1f;    // < 1 when lost >= 25%
        Debug.Log($"SpawnerManager GetPlayerHealth(): HealthLost: {damageFraction}, so multiplier is {multiplier} and waves undamaged is {wavesUndamaged}");
        playerSkill = multiplier + wavesUndamaged;
        return multiplier + wavesUndamaged;                                    // neutral otherwise
    }

    float GetCurrentPlayerHealth()
    {
        if (gameManager == null || gameManager.playerManager == null || gameManager.playerManager.mainTowerHealth == null) return 0f;
        return gameManager.playerManager.mainTowerHealth.currentHealth;
    }

	void OnEnemyDeath(int lastIndex)
    {
		totalEnemyPathIndex += Mathf.Max(0, lastIndex);
		enemiesDiedCount++;
		// Optionally refresh speed modifier mid-wave for subsequent spawns
		RecalculateModifiers();
    }

	float SetPathCompletionRate()
    {
		int denomEnemies = Mathf.Max(1, enemiesDiedCount);
		int pathLen =  pathObj.pathCells.Count;
		float pathCompletionRate = ((float)totalEnemyPathIndex / (float)denomEnemies) / (float)pathLen;

        return pathCompletionRate;
    }

	float GetAveragePathCompletionRate()
	{
		return Mathf.Clamp01(SetPathCompletionRate());
	}

	void RecalculateModifiers()
	{
		// Recompute style-based modifiers and speed whenever needed
		// This relies on GetPlayerStyleForPath totals, so reuse that computation path
		float dummyTotalHealth = 0f; // not used directly here, but keeps logic grouped
		PlayerStyle style = GetPlayerStyleForPath();
		// The assignments for attackModifier/healthModifier were already computed in GetPlayerStyleForPath()
		// We recompute speed only here
		float avgCompletion = GetAveragePathCompletionRate();
		speedModifier = Mathf.Lerp(0.8f, 1.2f, avgCompletion) * Mathf.Clamp(playerSkill, 0.5f, 2.5f);
	}
}
