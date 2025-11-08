using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class RequirementEntry
{
	public LootItem lootItem;
	public int quantity = 1;
}

[System.Serializable]
public class UpgradeRequirement
{
	public List<RequirementEntry> items = new List<RequirementEntry>();
}

public class towerUpgradeManager : MonoBehaviour
{
	GameManager gameManager;
    public Inventory inventory;		//set in the beginning

    public TowerManager tower;		//passed in by TowerManager
    public UpgradeRequirement[] upgradeRequirements;	//fetched from tower
    
    // Working sets for selection/consumption
    private List<Loot> playerLoot = new List<Loot>();		//all applicable loot
    private List<Loot> selectedItems = new List<Loot>();	//selected loot to be consumed

    //Percentage split of total loot's properties.
    private float healthPropertyValue = 0;
    private float attackDamagedValue = 0;
    private float attackSpeedValue = 0;
    private float attackRangeValue = 0;

	//Deltas of the properties (Deltas are the the totalCost increase per +1 of the property)
	public float deltaH = 0f;
	public float deltaD = 0f;
	public float deltaS = 0f;
	public float deltaR = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null) Debug.LogError("TowerManager Start(): GameManager not found in scene");

        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null && gameManager != null) inventory = gameManager.playerManager.inventory;
        if (inventory == null) Debug.LogWarning("towerUpgradeManager Start(): Inventory not found");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpgradeTower(TowerManager pTower)
    {
		if (pTower == null) 
        {
            Debug.Log("towerUpgradeManager UpgradeTower(): TowerManager is null!");
            return;
        }
		tower = pTower;
		upgradeRequirements = tower.upgradeRequirements;

		//if (!CanBeUpgraded()) return;
		// Select required items first (if the items cant be selected, then return)
		//if (!SelectRequiredItems()) return;
		// Compute property percentages from selected items
		CalculatePropertyValues();
		CalculateDeltas();

		//Update/Enable the UI
		EnableUI(true);
    }

    public void ApplyUpgrade()
    {
        //Before Upgrade
        int prevCost = tower.GetCurrentCost();

        //Consume the selected items
        ConsumeSelectedItems();
        // Apply the upgrades to the Tower Manager
        tower.maxHealth = Mathf.Max(0, Mathf.RoundToInt(tower.health.maxHealth + deltaH));
        tower.attackDamage = Mathf.Max(0f, tower.attack.attackDamage + deltaD);
        tower.attackSpeed = Mathf.Max(0f, tower.attack.attackSpeed + deltaS);
        tower.attackRadius = Mathf.Max(0f, tower.attack.rangeRadius + deltaR);
        tower.level++;
        //Apply the upgrade to the Tower's classes (health and attack)
        tower.UpdateStats();

        //After Upgrade
        int newCost = tower.GetCurrentCost();
        Debug.Log($"towerUpgradeManager UpgradeTower(): prevCost {prevCost} + upgradeCost {CalculateUpgradeCost()} = {newCost}");

        EnableUI(false);
        ResetValues();
    }

    public bool CanBeUpgraded()
    {
		// Can upgrade only if not at max level and requirements are met
		return !IsMaxLevel() && HasAllItems();
    }

	// Lazily (re)acquire the player's Inventory if it is missing (e.g., after scene/level load)
	bool EnsureInventory()
	{
		if (inventory != null) return true;

		inventory = FindAnyObjectByType<Inventory>();
		if (inventory == null && gameManager != null && gameManager.playerManager != null)
		{
			inventory = gameManager.playerManager.inventory;
		}
		return inventory != null;
	}

    public bool HasAllItems()
    {
		if (!EnsureInventory()) return false;
        var req = GetCurrentRequirement();
        if (req == null || req.items == null || req.items.Count == 0) return false;
        foreach (var entry in req.items)
        {
            if (entry == null || entry.lootItem == null) return false;
            if (!inventory.HasLoot(entry.lootItem, Mathf.Max(0, entry.quantity))) return false;
        }
        return true;
    }

    public bool IsMaxLevel()
    {
        return (tower.level >= upgradeRequirements.Length);
    }

    // Recompute preview deltas for the UI (requires SelectRequiredItems has populated selectedItems)
    public void ComputeUpgradePreview()
    {
        CalculatePropertyValues();
        CalculateDeltas();
    }

    // Selects the first applicable loot items per requirement and stores them in selectedItems
    public bool SelectRequiredItems()
    {
		if (!EnsureInventory()) return false;
        var req = GetCurrentRequirement();
        if (req == null || req.items == null || req.items.Count == 0) return false;

        playerLoot.Clear();
        selectedItems.Clear();
		// Track how many times we have already selected each specific Loot key to avoid double-using the same stack
		Dictionary<Loot, int> selectedCountByKey = new Dictionary<Loot, int>();

        foreach (var entry in req.items)
        {
			if (entry == null || entry.lootItem == null) return false;

			// Build an availability list for this specific requirement, subtracting already selected counts per key
			List<Loot> availableForThisEntry = new List<Loot>();
            foreach (var itemEntry in inventory.GetAllItems())
            {
                var lootKey = itemEntry.Key;
                var qty = itemEntry.Value;
                if (lootKey != null && lootKey.lootID == entry.lootItem.lootID)
                {
					int alreadySelected = 0;
					selectedCountByKey.TryGetValue(lootKey, out alreadySelected);
					int remaining = Mathf.Max(0, qty - alreadySelected);
					for (int i = 0; i < remaining; i++) availableForThisEntry.Add(lootKey);
                }
            }

            // Select the first N applicable items for this requirement
            int needed = Mathf.Max(0, entry.quantity);
			if (availableForThisEntry.Count < needed) return false; // not enough distinct instances left

			for (int i = 0; i < needed; i++)
			{
				var pick = availableForThisEntry[i];
				selectedItems.Add(pick);
				if (!selectedCountByKey.ContainsKey(pick)) selectedCountByKey[pick] = 0;
				selectedCountByKey[pick] = selectedCountByKey[pick] + 1;
			}
        }

        return true;
    }

    void CalculatePropertyValues()
    {
        // Calculate percentages of each property within selectedItems
        if (selectedItems == null || selectedItems.Count == 0)
        {
            healthPropertyValue = 0f;
            attackDamagedValue = 0f;
            attackSpeedValue = 0f;
            attackRangeValue = 0f;
            return;
        }

        int total = selectedItems.Count;
        int healthCount = 0;
        int damageCount = 0;
        int speedCount = 0;
        int rangeCount = 0;

        foreach (var loot in selectedItems)
        {
            switch (loot.lootProperty)
            {
                case LootProperty.Health:
                    healthCount++;
                    break;
                case LootProperty.AttackDamage:
                    damageCount++;
                    break;
                case LootProperty.AttackSpeed:
                    speedCount++;
                    break;
                case LootProperty.AttackRange:
                    rangeCount++;
                    break;
            }
        }

        healthPropertyValue = (float)healthCount / (float)total;
        attackDamagedValue = (float)damageCount / (float)total;
        attackSpeedValue = (float)speedCount / (float)total;
        attackRangeValue = (float)rangeCount / (float)total;
    }

    void CalculateDeltas()
    {
        // We want: costAfter = costBefore + upgradeCost, based on ObjectManager's cost formula
        // C = ((D * S * (R/1.5)) + H) * M
        // Allocate upgradeCost across properties via previously computed percentages
        int upgradeCost = CalculateUpgradeCost();
        if (upgradeCost <= 0) return;

        float M = tower.specialityModifier;
        if (Mathf.Approximately(M, 0f)) M = 1f; // avoid div-by-zero; treat as neutral if unset

        // Calculate the target upgrade cost for each property
        float U = upgradeCost * 0.5f;
        float U_health = U * healthPropertyValue + U;   //health is by default half the cost, plus the extra health
        float U_damage = U * attackDamagedValue;        //damage uses half the cost (what remains after the base health increase)
        float U_speed = U * attackSpeedValue;           //speed uses half the cost (what remains after the base health and damage increase)
        float U_range = U * attackRangeValue;           //range uses half the cost (what remains after the base health, damage and speed increase)

        // Current stats
        float D = tower.attack.attackDamage;
        float S = tower.attack.attackSpeed;
        float R = tower.attack.rangeRadius;
        float H = tower.health.maxHealth;

        // Compute minimal deltas to achieve the allocated increases in total cost
        // Health increases additively inside parentheses → ΔH = U_health / M
        deltaH = Mathf.RoundToInt(U_health / M);

        // For multiplicative term T = D * S * (R/1.5), change each factor individually
        // If denominator is zero (e.g., S==0 or R==0), reallocate that share to health to avoid NaN
        float denomD = S * (R / 1.5f) * M; // cost increase per +1 damage
        if (denomD > 0f && U_damage > 0f)
        {
            deltaD = U_damage / denomD;
        }
        else
        {
            // roll damage allocation into health if not computable
            deltaH += Mathf.RoundToInt(U_damage / M);
        }

        float denomS = D * (R / 1.5f) * M; // cost increase per +1 speed
        if (denomS > 0f && U_speed > 0f)
        {
            deltaS = U_speed / denomS;
        }
        else
        {
            deltaH += Mathf.RoundToInt(U_speed / M);
        }

        float denomR = D * S * (1f / 1.5f) * M; // cost increase per +1 range
        if (denomR > 0f && U_range > 0f)
        {
            deltaR = U_range / denomR;
        }
        else
        {
            deltaH += Mathf.RoundToInt(U_range / M);
        }
        Debug.Log($"towerUpgradeManager CalculateDeltas(): deltaH {deltaH}, deltaD {deltaD}, deltaS {deltaS}, deltaR {deltaR}");
    }

    int CalculateUpgradeCost()
    {
        int cost = 0;
        var req = GetCurrentRequirement();
        if (req != null && req.items != null)
        {
            foreach (var entry in req.items)
            {
                if (entry != null && entry.lootItem != null)
                {
                    cost += entry.lootItem.lootCost * Mathf.Max(0, entry.quantity);
                }
            }
        }
        return cost;
    }

    // Consumes the already selected items from the inventory
    void ConsumeSelectedItems()
    {
		if (!EnsureInventory()) return;
        if (selectedItems == null || selectedItems.Count == 0) return;
        Debug.Log("towerUpgradeManager ConsumeSelectedItems(): Checks passed, removing items.");
        foreach (var group in selectedItems.GroupBy(l => l))
        {
			int toRemove = group.Count();
			bool ok = inventory.RemoveItem(group.Key, toRemove);
			if (!ok)
			{
				Debug.LogWarning($"towerUpgradeManager ConsumeSelectedItems(): Failed to remove {toRemove}x '{group.Key?.lootName}'");
			}
			else
			{
				int remaining = inventory.GetItemQuantity(group.Key);
				Debug.Log($"towerUpgradeManager ConsumeSelectedItems(): Removed {toRemove}x '{group.Key?.lootName}'. Remaining: {remaining}");
			}
        }
    }

    UpgradeRequirement GetCurrentRequirement()
	{
		if (upgradeRequirements == null || upgradeRequirements.Length == 0) return null;
		if (tower == null) return null;
		if (tower.level < 0 || tower.level >= upgradeRequirements.Length) return null;
		return upgradeRequirements[tower.level];
	}

    void EnableUI(bool enable)
    {
        if(gameManager == null || gameManager.uiManager == null)
        {
            Debug.Log("towerUpgradeManager EnableUI(): game manager or ui manager are null!"); 
            return;
        }
        //Enable UI
        gameManager.uiManager.EnableTowerUpgradePanel(enable, this);
    }

    void ResetValues()
	{
        tower = null;		//passed in by TowerManager
		upgradeRequirements = null;    //fetched from tower

		// Working sets for selection/consumption
		playerLoot.Clear();
		selectedItems.Clear();

		//Percentage split of total loot's properties.
		healthPropertyValue = 0;
		attackDamagedValue = 0;
		attackSpeedValue = 0;
		attackRangeValue = 0;

		//Deltas of the properties (Deltas are the the totalCost increase per +1 of the property)
		deltaH = 0f;
		deltaD = 0f;
		deltaS = 0f;
		deltaR = 0f;
	}
}
