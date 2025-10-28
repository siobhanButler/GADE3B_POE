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
                return;
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
        if (inventory == null)
        {
            inventory = GetComponentInParent<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("InventoryUIManager Start(): No player inventory found");
                return;
            }
        }
        // Subscribe to inventory add events so UI reflects changes
        if (inventory != null)
        {
            inventory.OnItemAdded += AddItem;
            inventory.OnItemRemoved += RemoveItem;
            inventory.OnItemQuantityChanged += ChangeItemQuantity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*
    void OnEnable()
    {
        // Subscribe to inventory add events so UI reflects changes
        if (inventory != null)
        {
            inventory.OnItemAdded += AddItem;
            inventory.OnItemRemoved += RemoveItem;
            inventory.OnItemQuantityChanged += ChangeItemQuantity;
        }
    }

    void OnDisable()
    {
        // Unsubscribe to avoid memory leaks and duplicate bindings
        if (inventory != null)
        {
            //inventory.OnItemAdded -= AddItem;
            //inventory.OnItemRemoved -= RemoveItem;
            //inventory.OnItemQuantityChanged -= ChangeItemQuantity;
        }
    }
    */

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
}
