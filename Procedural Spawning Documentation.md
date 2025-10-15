## Procedural Spawning Documentation

### Overview
The spawning system dynamically scales enemy waves per spawner based on the player’s defenses, recent performance, and observed in-wave outcomes. Each wave:
- Computes an enemy budget using tower strength, path difficulty, player performance multiplier, and a coin-based bonus.
- Fills a list of enemies via weighted random selection until the budget is reached.
- Spawns enemies at a uniform interval, while applying real-time modifiers for speed/health/damage that adapt to the player’s play style and skill.

### How difficulty is scaled for a consistent challenge
Enemy budget per spawner is computed as:
```
baseBudget = GetTotalTowerCost()
  where GetTotalTowerCost():
    totalTowerCost = Σ over towers on this path [ tower.RecalculateCost() / (#paths tower participates in) ]
    coinBonus = playerCoins / numTowerLocations  (numTowerLocations from tag "TowerLocation", fallback to component search)
    return round(totalTowerCost) + coinBonus

difficultyFactor = max(0.75, totalPathDifficulty / pathCellCount)

playerSkill = GetPlayerHealth()   // performance multiplier (see below)

enemyBudget = max(1, round(baseBudget × difficultyFactor × playerSkill))
```
Notes:
- `difficultyFactor` ensures minimum challenge for low-difficulty paths.
- `coinBonus` injects a small “economy pressure” proportional to available tower slots.
- `playerSkill` reflects recent damage taken by the player’s main tower and rewards damage-free waves.

After each wave finishes, the system also derives pacing variables for the next wave:
```
waveCompletionTime = wall-clock seconds to clear the wave
spawnInterval = (waveCompletionTime / enemyQuantityToSpawn) × 0.3

// Time between waves now matches estimated traversal time of the path
pathLen = pathObj.pathCells.Count
tilesPerSecond = 1.5 × speedModifier   // tuned effective tiles/sec for average enemy movement
waveInterval = pathLen / tilesPerSecond
```

Next-wave trigger logic (done):
```
Let total = enemyQuantityToSpawn
Let dead  = number of destroyed enemies in current wave

If (all enemies dead) before waveInterval elapses:
  Start next wave after 25% of waveInterval

Else, once waveInterval has elapsed:
  Start next wave as soon as dead/total ≥ 0.75
```
Rationale (done): Prevents wave difficulty complications when enemies haven’t had time to reach towers and deal damage, and reduces player overwhelm by pacing waves more intelligently based on how the current wave is progressing.

### How spawning adapts to player skill and play style
1) Performance multiplier (playerSkill):
```
x = damageFraction = clamp01((prevWaveStartHealth - currentHealth) / prevWaveStartHealth)

if x < 0.2:
  multiplier = -exp(8.04 × (x - 0.2)^5) + 1        // >1 when little/no damage taken
  wavesUndamaged++
else if x == 0.2:
  multiplier = 1
  wavesUndamaged = 0
else:
  multiplier = -exp(1.1 × (x - 0.2)^5) + 1         // <1 when significant damage taken
  wavesUndamaged = 0

playerSkill = multiplier + wavesUndamaged
```

Player health metric (implemented 60/40 weighting):
```
// Source used by playerSkill's currentHealth term
totalTowersHealth = Σ currentHealth of all towers on the active path
mainTowerHealth   = currentHealth of the main tower

GetCurrentPlayerHealth = 0.4 × mainTowerHealth + 0.6 × totalTowersHealth
```
Rationale (done): Originally 50/50 was used; tuning showed it was still too lenient when many towers absorbed damage while the main tower stayed safe. Moving to 60/40 (path towers/main tower) maintains pressure in those cases while still reflecting core health loss.
Implications:
- Minimal or no health loss increases `playerSkill`, increasing the next wave’s budget.
- Consistently damage-free waves accrue `wavesUndamaged`, steadily increasing pressure.

2) In-wave adaptation using path completion (speed modifier):
```
On each enemy death → OnEnemyDeath(lastIndex):
  totalEnemyPathIndex += lastIndex
  enemiesDiedCount++

pathLen = pathObj.pathCells.Count
avgPathCompletion = clamp01(((float)totalEnemyPathIndex / max(1, enemiesDiedCount)) / pathLen)

speedModifier = lerp(0.8, 1.2, avgPathCompletion) × clamp(playerSkill, 0.5, 2.5)

// Applied at spawn via SetupEnemy(attackModifier, healthModifier, speedModifier * 0.5)
```
Implications:
- If enemies die early (low completion), subsequent spawns are slower.
- If enemies consistently reach far (high completion), subsequent spawns are faster.

3) Play style influence (weights and toughness):
- Spawn likelihoods are adapted per-path by the detected style:
```
totalOffense ≈ Σ (attackDamage × attackSpeed × rangeRadius) across towers on path
totalMaxHealth ≈ Σ tower max health across towers on path

style = Defensive if totalMaxHealth > totalOffense
      = Offensive if totalOffense  > totalMaxHealth
      = Neutral otherwise

ApplyPlayerStyleWeights():
  - Favors Neutral enemy when style = Neutral
  - Favors Tanky enemy when style = Offensive
  - Favors Hypnosis/control enemy when style = Defensive
  (weights ×1.5 for the matching archetype)
```
- Enemy toughness modifiers at spawn (per-path aggregates):
```
attackModifier = (totalMaxHealth > 0 && totalOffense > 0)
                  ? (totalMaxHealth / totalOffense) : 1
  // If the player invests more in health than offense, enemies get higher damage

healthModifier = (totalMaxHealth > 0 && totalOffense > 0)
                  ? (totalOffense / totalMaxHealth) : 1
  // If the player invests more in offense than health, enemies get higher health

// Additional coin-based pressure: more coins => higher enemy damage
coinDamageMult = clamp(1 + coins / 1000, 1, 2)
attackModifier *= coinDamageMult
```

### How spawn locations are determined
- The `RoomGenerator` places spawner GameObjects procedurally.
- Each `SpawnerManager` is associated to a `SubCell` parent and acquires its `PathObj` from the `PathGenerator`.
- Enemies are instantiated at the spawner’s transform position and move along `pathObj.pathCells` via `EnemyMovement`.

### How the system decides which enemy type to spawn
```
While GetTotalEnemyCost(enemiesToSpawn + candidate) < enemyBudget:
  candidate = weighted random pick from enemyPrefabs

Weights:
  base = prefab.ObjectManager.spawnLikelihood
  adjusted by ApplyPlayerStyleWeights() using detected style (×1.5 for favored archetype)

Cost accounting:
  ObjectManager.cost (recomputed/validated via OnValidate):
    cost = round(((attackDamage × attackSpeed × attackRadius) + maxHealth) × (specialityModifier + 1))
```
This loop fills `enemiesToSpawn` until the budget is met or exceeded.

---

## Variables influencing spawning and their formulas

### Inputs and environment
- `enemyPrefabs`: Catalog of enemy types with per-prefab `spawnLikelihood` and computed `cost`.
- `pathObj.pathCells`: The path the enemy will traverse; used for difficulty and path completion rate.
- `pathObj.totalPathDifficulty`: Aggregate difficulty (e.g., overlap factor) over the path.
- `gameManager.playerManager`: Provides `mainTowerHealth`, `coins`.
- `TowerManager` instances on `pathObj.towers` with `attack` and `health` components.

### Wave pacing and counts
- `numberOfWaves`: Set from `GameManager.wavesAmount` when the spawner is set up.
- `enemyQuantityToSpawn`: Random integer in `[minEnemies, maxEnemies]`.
- `spawnInterval` (next wave):
```
spawnInterval = (waveCompletionTime / enemyQuantityToSpawn) × 0.3
```
- `waveInterval`: Delay between waves.

### Budget computation
- `baseBudget`:
```
totalTowerCost = Σ [ tower.RecalculateCost() / pathCountForTower ]
coinBonus = playerCoins / numTowerLocations
baseBudget = round(totalTowerCost) + coinBonus
```
- `difficultyFactor`:
```
difficultyFactor = max(0.75, totalPathDifficulty / pathCellCount)
```
- `playerSkill`:
```
damageFraction x = clamp01((prevWaveStartHealth - currentHealth) / prevWaveStartHealth)
multiplier = {
  x < 0.2 : -exp(8.04 × (x - 0.2)^5) + 1
  x = 0.2 : 1
  x > 0.2 : -exp(1.1 × (x - 0.2)^5) + 1
}
playerSkill = multiplier + wavesUndamaged
```
- Final budget:
```
enemyBudget = max(1, round(baseBudget × difficultyFactor × playerSkill))
```

### Enemy selection
- `ChooseEnemyWeightedByLikelihood()`:
```
// Prefab values are cached and not mutated; adjusted weights are per-wave
totalWeight = Σ max(0, adjustedSpawnLikelihoods[i])
pick r ∈ [0, totalWeight)
return first prefab where cumulativeWeight ≥ r

spawnLikelihood_adjusted = baseSpawnLikelihood[i] × styleWeight
  styleWeight = 1.5 if prefab matches detected style
              = 1.0 otherwise
```

Implementation detail (done): To avoid permanently modifying prefab data, the spawner caches `baseSpawnLikelihoods` at setup and computes `adjustedSpawnLikelihoods` each wave based on play style. Selection reads from `adjustedSpawnLikelihoods` only.

### Modifiers applied per enemy at spawn
- Computed before each spawn, using current per-path aggregates and observed performance:
```
attackModifier = (totalMaxHealth / totalOffense)  // defensive player ⇒ higher enemy damage
healthModifier = (totalOffense / totalMaxHealth)  // offensive player ⇒ higher enemy health

avgPathCompletion = clamp01(((float)totalEnemyPathIndex / max(1, enemiesDiedCount)) / pathLen)
speedModifier_base = lerp(0.8, 1.2, avgPathCompletion) × clamp(playerSkill, 0.5, 2.5)
speedModifier_applied = speedModifier_base × 0.5   // additional tuning factor applied at SetupEnemy
```

### State tracked during a wave
- `prevWaveStartHealth`: Player health snapshot at the start of the wave (for `damageFraction`).
- `wavesUndamaged`: Count of consecutive waves where `damageFraction < 0.2` (increases `playerSkill`).
- `totalEnemyPathIndex`: Sum of `currentPathIndex` from each dead enemy.
- `enemiesDiedCount`: Number of enemies that have died in this wave.
- `avgPathCompletion`: Normalized average of how far enemies got before dying.
- `playerSkill`: Performance multiplier; also influences speed indirectly.

### Spawn location determination
- The `RoomGenerator` places spawners procedurally and the `pathGenerator` assigns a `PathObj` with `pathCells`.
- `SpawnerManager` instantiates enemies at its transform position and sets `EnemyManager.pathFromSpawner = pathObj.pathCells`.

---

## Execution order summary
1) Setup: Spawner gets `PathObj` and `numberOfWaves` from `GameManager`.
2) StartWave(): snapshot `prevWaveStartHealth` → compute `enemyBudget` → fill `enemiesToSpawn`.
3) SpawnRoutine: spawn enemies at `spawnInterval`.
4) At each spawn: ensure components exist (Setup), compute modifiers, `SetupEnemy(attack, health, speed)`.
5) On enemy death: update path completion stats; modifiers update for subsequent spawns.
6) When all enemies in wave are dead: compute `spawnInterval` for next wave and advance waves.


