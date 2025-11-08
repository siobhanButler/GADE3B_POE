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
        SetLootToGrant(enemy.lootItems);    //using GetCurrentCost(0 so that the enemy modifiers are taken into consideration

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
        if (coinSplit + lootSplit != 1f) Debug.LogWarning($"LootManager SelectLootCoinSplit(): Invalid results, lootSplit is {lootSplit} and coinSplit is {coinSplit}");

        coinBudget = Mathf.RoundToInt(enemyCost * coinSplit);
        lootBudget = Mathf.RoundToInt(enemyCost * lootSplit);
    }

    void SetLootToGrant(LootItem[] lootItems)     //populates lootToGrant with randomly selected loot until 
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

                //select a random property for the new loot
                switch (Random.Range(0, 4)) //0 to 3 (4 cause range is exclusive) for the LootProperty options
                {
                    case 0:
                        newLoot = new Loot(newLootItem, LootProperty.Health);
                        break;
                    case 1:
                        newLoot = new Loot(newLootItem, LootProperty.AttackDamage);
                        break;
                    case 2:
                        newLoot = new Loot(newLootItem, LootProperty.AttackSpeed);
                        break;
                    case 3:
                        newLoot = new Loot(newLootItem, LootProperty.AttackRange);
                        break;
                }

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
