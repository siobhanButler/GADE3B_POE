using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    // limitless inventory: no size constraints
    
    [Header("Inventory Contents")]
    [SerializeField] private Dictionary<Loot, int> items = new Dictionary<Loot, int>();
    
    // Events for inventory changes (Delegates)
    // @Ang 2 UI - These can be used to update the UI
    public System.Action<Loot, int> OnItemAdded;
    public System.Action<Loot, int> OnItemRemoved;
    public System.Action<Loot, int, int> OnItemQuantityChanged; // loot, oldQuantity, newQuantity
    
    // Properties
    public int CurrentItemCount => items.Count;
    // limitless inventory: no size/limit properties
    
#if UNITY_EDITOR
    // Editor-only read-only view of items for custom inspector display
    public System.Collections.Generic.IReadOnlyDictionary<Loot, int> DebugItems => items;
#endif
    
    // Add loot to inventory
    public bool AddItem(Loot loot, int quantity = 1)
    {
        if (loot == null || quantity <= 0) return false;
        
        // limitless inventory: no capacity check

        // Stack by equivalence (same loot name + property) even if instance differs
        Loot key = FindEquivalentKey(loot);
        if (key != null)
        {
            int oldQuantity = items[key];
            items[key] = oldQuantity + quantity;
            OnItemQuantityChanged?.Invoke(key, oldQuantity, items[key]);
        }
        else
        {
            items[loot] = quantity;
            OnItemAdded?.Invoke(loot, quantity);
        }
        
        return true;
    }
    
    // Remove loot from inventory
    public bool RemoveItem(Loot loot, int quantity = 1)
    {
        if (loot == null || quantity <= 0) return false;

        // Support removing by equivalent loot instance
        Loot key = items.ContainsKey(loot) ? loot : FindEquivalentKey(loot);
        if (key == null) return false;

        if (items[key] >= quantity)
        {
            int oldQuantity = items[key];
            items[key] -= quantity;
            
            if (items[key] <= 0)
            {
                items.Remove(key);
                OnItemRemoved?.Invoke(key, oldQuantity);
            }
            else
            {
                OnItemQuantityChanged?.Invoke(key, oldQuantity, items[key]);
            }
            
            return true;
        }
        
        return false;
    }
    
    // Get quantity of specific loot
    public int GetItemQuantity(Loot loot)
    {
        if (loot == null) return 0;
        if (items.ContainsKey(loot)) return items[loot];
        Loot key = FindEquivalentKey(loot);
        return key != null ? items[key] : 0;
    }
    
    // Check if inventory contains loot
    public bool HasItem(Loot loot, int quantity = 1)
    {
        return GetItemQuantity(loot) >= quantity;
    }
    
    // Get all items as a list (for UI display)
    public List<KeyValuePair<Loot, int>> GetAllItems()
    {
        return items.ToList();
    }

    // Clear entire inventory
    public void ClearInventory()
    {
        items.Clear();
        OnItemRemoved?.Invoke(null, 0); // Signal complete clear
    }
    
    // Get loot by rarity
    public List<KeyValuePair<Loot, int>> GetItemsByRarity(LootRarity rarity)
    {
        return items.Where(kvp => kvp.Key.lootRarity == rarity).ToList();
    }

    // Get loot by property
    public List<KeyValuePair<Loot, int>> GetItemsByProperty(LootProperty property)
    {
        return items.Where(kvp => kvp.Key.lootProperty == property).ToList();
    }
    
    // Debug method to print inventory contents
    [ContextMenu("Print Inventory")]
    public void PrintInventory()
    {
        int totalQty = 0; foreach (var v in items.Values) totalQty += v;
        Debug.Log($"Inventory Contents ({CurrentItemCount} unique items, {totalQty} total):");
        foreach (var kvp in items)
        {
            Debug.Log($"- {kvp.Key.lootName}: {kvp.Value}");
        }
    }

    // Find an existing key in the dictionary that is equivalent to the provided loot
    Loot FindEquivalentKey(Loot loot)
    {
        foreach (Loot existing in items.Keys)
        {
            if (AreEquivalent(existing, loot)) return existing;
        }
        return null;
    }

    // Define equivalence for stacking: same logical item and property
    bool AreEquivalent(Loot a, Loot b)
    {
        if (a == null || b == null) return false;
        // Use name + property as identity; extend if needed (e.g., rarity)
        return a.lootProperty == b.lootProperty && a.lootName == b.lootName;
    }
}
