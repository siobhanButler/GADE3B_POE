using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    // References to UI elements, assign in inspector
    public GameObject panel_Inventory;          // Root panel
    public RectTransform content;               // Content RectTransform (items parent)
    public Button btn_CloseInventory;           // Close button for inventory panel
    public UIManager uiManager;               // Reference to UIManager for enabling/disabling panels

    public GameObject itemUIPrefab;            // Prefab for individual item UI
    [SerializeField] private Inventory inventory;    // Source inventory to listen to
    // Lookup: map each Loot to its instantiated ItemUIManager for fast update/remove without searching children
    private readonly Dictionary<Loot, ItemUIManager> ItemUiMap = new Dictionary<Loot, ItemUIManager>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("InventoryUIManager Start(): No inventory found");
					// don't return yet; UIManager may bind later via BindInventory
            }
        }
        // Wire up the close button to hide the inventory panel
        if (btn_CloseInventory != null)
        {
            btn_CloseInventory.onClick.AddListener(CloseInventory);
        }

        if (uiManager == null)
        {
            uiManager = GetComponentInParent<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("CraftingUIManager Start(): No UIManager found in parent hierarchy");
            }
        }
        //Get Inventory from the attached player if not assigned
			if (inventory == null) { inventory = GetComponentInParent<Inventory>(); }
			// Subscribe to inventory events and build initial UI if we have one now
			if (inventory != null) { BindInventory(inventory); }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void OnEnable()
	{
		// When the panel is shown, ensure we are bound to the correct inventory and refresh UI
		if (inventory == null)
		{
			Inventory candidate = null;
			if (uiManager != null && uiManager.gameManager != null && uiManager.gameManager.playerManager != null)
			{
				candidate = uiManager.gameManager.playerManager.inventory;
			}
			if (candidate == null)
			{
				candidate = FindFirstObjectByType<Inventory>();
			}
			if (candidate != null)
			{
				BindInventory(candidate);
			}
		}

		// Rebuild from the current inventory snapshot so removed items are not displayed
		RebuildAllFromInventory();
	}

    // Add a UI entry when an item is added to the Inventory (listener for Inventory.AddItem)
    public void AddItem(Loot item, int quantity)
    {
        if (itemUIPrefab == null || content == null || item == null)
        {
            Debug.LogWarning("InventoryUIManager.AddItem(): Missing references or item is null.");
            return;
        }

        // Instantiate the UI prefab as a child of the content container
        GameObject itemUiObject = Instantiate(itemUIPrefab, content);

        // If the prefab has an ItemUIManager, populate its fields
        ItemUIManager itemUiManager = itemUiObject.GetComponent<ItemUIManager>();
        if (itemUiManager != null)
        {
            itemUiManager.UpdateItemUI(item, quantity);
            ItemUiMap[item] = itemUiManager;
        }
    }

    // Remove the item's UI entry when it is removed from the inventory
    public void RemoveItem(Loot item, int oldQuantity)
    {
        if (item == null) return;

        if (ItemUiMap.TryGetValue(item, out ItemUIManager itemUiManager) && itemUiManager != null)
        {
            Destroy(itemUiManager.gameObject);
            ItemUiMap.Remove(item);
        }
    }

    // Update the quantity display when an item's quantity changes
    public void ChangeItemQuantity(Loot item, int oldQuantity, int newQuantity)
    {
        if (item == null) return;

        if (newQuantity <= 0) // if Quantity dropped to zero or below; remove the UI entry
        {
            RemoveItem(item, oldQuantity);
            return;
        }

        if (ItemUiMap.TryGetValue(item, out ItemUIManager itemUiManager) && itemUiManager != null)
        {
            itemUiManager.UpdateItemUI(item, newQuantity);
        }
    }

    // Close the inventory panel when close button is clicked
    public void CloseInventory()
    {
        panel_Inventory.SetActive(false);
    }

    // Open the inventory panel (useful for external calls)
    public void OpenInventory()
    {
        panel_Inventory.SetActive(true);
    }

		// Rebind to a specific Inventory instance and rebuild UI to match it
		public void BindInventory(Inventory newInventory)
		{
			if (newInventory == inventory && newInventory != null) return;

			// Unsubscribe from previous inventory events
			if (inventory != null)
			{
				inventory.OnItemAdded -= AddItem;
				inventory.OnItemRemoved -= RemoveItem;
				inventory.OnItemQuantityChanged -= ChangeItemQuantity;
			}

			inventory = newInventory;

			// Clear existing UI and map
			RebuildAllFromInventory();

			// Subscribe to new inventory events
			if (inventory != null)
			{
				inventory.OnItemAdded += AddItem;
				inventory.OnItemRemoved += RemoveItem;
				inventory.OnItemQuantityChanged += ChangeItemQuantity;
			}
		}

		void RebuildAllFromInventory()
		{
			// Clear content children
			if (content != null)
			{
				for (int i = content.childCount - 1; i >= 0; i--)
				{
					Destroy(content.GetChild(i).gameObject);
				}
			}
			ItemUiMap.Clear();

			// Populate from current inventory snapshot
			if (inventory == null || content == null || itemUIPrefab == null) return;

			var items = inventory.GetAllItems();
			foreach (var kvp in items)
			{
				var loot = kvp.Key;
				var qty = kvp.Value;
				if (loot == null || qty <= 0) continue;
				AddItem(loot, qty);
			}
		}
}
