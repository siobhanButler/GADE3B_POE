using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class LootManager : MonoBehaviour
{
    [SerializeField] private bool giveLoot = true;
    [SerializeField] private Dictionary<LootItem, float> lootTable;     //weighted loot table with the loot type the enemy can drop and the chance of it dropping that loot
    
    PlayerManager playerManager;
    TowerLocationManager[] towerLocations;

    private int coinBudget = 0;
    private int lootBudget = 0;
    private List<Loot> lootToGrant = new List<Loot>();

    private int lootBudgetLeftover = 0;

    // Start as coroutine so we can wait until PlayerManager and tower locations exist
    System.Collections.IEnumerator Start()
    {
        // Ensure we have the PlayerManager on the same GameObject
        playerManager = GetComponent<PlayerManager>();
        while (playerManager == null)
        {
            playerManager = GetComponent<PlayerManager>();
            yield return null;
        }

        // Wait until at least one TowerLocationManager is present (they may be spawned later)
        while (true)
        {
            towerLocations = FindObjectsByType<TowerLocationManager>(FindObjectsSortMode.None);
            if (towerLocations != null && towerLocations.Length > 0) break;
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GrantRewards(EnemyManager enemy)
    {
        if (enemy == null)
        {
            Debug.Log("LootManger AllocateLoot(): Enemy parameter is null. Can't grant loot.");
            return;
        }
        UpdateLootCoinBudget(enemy.GetCurrentCost());
        SetLootToGrant(enemy.lootItems, enemy.preferredLootProperty);    // using GetCurrentCost so that enemy modifiers are considered

        GrantCoins(enemy.GetCurrentCost());
        GrantLoot();

        //Reset
        coinBudget = 0;
        lootBudget = 0;
        lootToGrant.Clear();
        lootBudgetLeftover = 0;
    }

    void UpdateLootCoinBudget(int enemyCost)  //sets coinSplit and lootSplit based on the split between number of occupied vs unoccupied tower locations
    {
        if (giveLoot)
        {
            coinBudget = Mathf.RoundToInt(enemyCost * 0.5f);
            lootBudget = Mathf.RoundToInt(enemyCost * 0.5f);
            return;
        }
        int occupiedLocations = 0;
        int unoccupiedLocations = 0;
        foreach (TowerLocationManager towerLocation in towerLocations)
        {
            if(towerLocation.isOccupied) occupiedLocations++;
            else unoccupiedLocations++;
        }
		float coinSplit = (float)unoccupiedLocations / (float)towerLocations.Length;
		float lootSplit = (float)occupiedLocations / (float)towerLocations.Length;

		// Apply small jitter (±10%) to coin/loot ratio, then renormalize
		float jitter = Random.Range(-0.1f, 0.1f);
		coinSplit = Mathf.Clamp01(coinSplit * (1f + jitter));
		lootSplit = 1f - coinSplit;
        if (coinSplit + lootSplit != 1f) Debug.LogWarning($"LootManager SelectLootCoinSplit(): Invalid results, lootSplit is {lootSplit} and coinSplit is {coinSplit}");

        coinBudget = Mathf.RoundToInt(enemyCost * coinSplit);
        lootBudget = Mathf.RoundToInt(enemyCost * lootSplit);
    }

    void SetLootToGrant(LootItem[] lootItems, LootProperty preferredProperty)     //populates lootToGrant with randomly selected loot until 
    {
        //Select random loot items up to the cost of the lootSplit
        lootToGrant.Clear();
        int lootCost = 0;
        LootItem newLootItem = null;
        Loot newLoot = null;
        bool isBudgetFilled = false;

        while (!isBudgetFilled)     //continue adding loot until a randomly generated loot item's cost exceeds the budget
        {
            newLootItem = SelectRandomWeightedLoot(lootItems);
            if (newLootItem == null)
            {
                isBudgetFilled = true;
                lootBudgetLeftover = lootBudget - lootCost;
                break;
            }
            if (lootCost + newLootItem.lootCost > lootBudget)
            {
                isBudgetFilled = true;
                lootBudgetLeftover = lootBudget - lootCost;
                // TODO: prioritize rarer loot even if it was drawn last, by removing common loot (order the loot by rarity thenn remove the common loot until the budget is not exceeded)
            }
            else
            {
                lootCost += newLootItem.lootCost;

                // select a property with +20% weight bias toward the enemy's preferred property
                LootProperty chosen = SelectWeightedProperty(preferredProperty, 1.2f);
                newLoot = new Loot(newLootItem, chosen);

                lootToGrant.Add(newLoot);
            }               
        }

        //DEBUG LOG
        string lootNames = "";
        foreach (Loot loot in lootToGrant) 
        {
            lootNames += loot.lootName + "(" + loot.lootProperty + "), ";
        }
        Debug.Log($"LootManager SetLootToGrant(): Loot to grant is: {lootNames}");
    }

    LootProperty SelectWeightedProperty(LootProperty preferred, float preferredMultiplier)
    {
        // Base equal weights
        float wHealth = 1f;
        float wADmg = 1f;
        float wASpd = 1f;
        float wARng = 1f;
        // Boost preferred
        switch (preferred)
        {
            case LootProperty.Health: wHealth *= preferredMultiplier; break;
            case LootProperty.AttackDamage: wADmg *= preferredMultiplier; break;
            case LootProperty.AttackSpeed: wASpd *= preferredMultiplier; break;
            case LootProperty.AttackRange: wARng *= preferredMultiplier; break;
        }
        float total = wHealth + wADmg + wASpd + wARng;
        float roll = Random.Range(0f, total);
        if (roll < wHealth) return LootProperty.Health;
        roll -= wHealth;
        if (roll < wADmg) return LootProperty.AttackDamage;
        roll -= wADmg;
        if (roll < wASpd) return LootProperty.AttackSpeed;
        return LootProperty.AttackRange;
    }

    LootItem SelectRandomWeightedLoot(LootItem[] lootItems)
    {
        if (lootItems == null || lootItems.Length == 0) return null;

        // Calculate the total of all base likelihoods.
        float totalWeight = 0f;
        foreach (LootItem item in lootItems)
        {
            if (item == null) continue;
            if (item.lootBaseLikelihood > 0f) totalWeight += item.lootBaseLikelihood;
        }

        if (totalWeight <= 0f) return null;

        // Roll a number in [0, totalWeight). This is our target within the cumulative range.
        float roll = Random.Range(0f, totalWeight);

        // Walk through the items, accumulating weights until we cross the roll.
        float cumulative = 0f;
        foreach (LootItem item in lootItems)
        {
            if (item == null) continue;
            float w = item.lootBaseLikelihood;
            if (w <= 0f) continue;
            cumulative += w;
            if (roll < cumulative)
            {
                return item;
            }
        }

        // Fallback (should not be hit due to checks above)
        return null;
    }

    void GrantCoins(int enemyCost)      //Calculate coins using coinSplit and add to player coins
    {
        if (playerManager == null) return;
        playerManager.coins += (coinBudget + lootBudgetLeftover);
    }

    void GrantLoot()    //adds loot to the player's inventory
    {
        foreach(Loot loot in lootToGrant)
        {
            playerManager.inventory.AddItem(loot);
        }
    }
}
