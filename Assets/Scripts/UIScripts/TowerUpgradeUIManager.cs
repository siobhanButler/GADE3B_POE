using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerUpgradeUIManager : MonoBehaviour
{
    towerUpgradeManager towerUpgradeManager;

    // UI references: parent panel for required items and prefab for each item UI
    public RectTransform towerUpgradePanel;
    public TMP_Text title;
    public TMP_Text upgradeText;
    public RectTransform requiredItemsPanel;
    public GameObject itemUIPrefab;
    public Button upgradeButton;
    public Button closeButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (towerUpgradeManager == null) towerUpgradeManager = FindFirstObjectByType<towerUpgradeManager>();
        if (towerUpgradeManager == null) Debug.LogError("TowerUpgradeManager Start(): TowerUpgradeManager not found");

        upgradeButton.onClick.AddListener(UpgradeButtonOnClick);
        closeButton.onClick.AddListener(CloseButtonOnClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableUI(bool enable)
    {
        if (towerUpgradeManager == null)
        {
            if (towerUpgradeManager == null) towerUpgradeManager = FindFirstObjectByType<towerUpgradeManager>();
            if (towerUpgradeManager == null) Debug.LogError("TowerUpgradeManager EnableUI(): TowerUpgradeManager not found");
        }
        if (towerUpgradePanel != null) towerUpgradePanel.gameObject.SetActive(enable);

        if(enable) BasicUpdate();
        
    }

    void BasicUpdate()
    {
        //Upgrade Button and If can be upgraded
        upgradeButton.enabled = towerUpgradeManager.CanBeUpgraded();
        if (towerUpgradeManager.IsMaxLevel())
        {
            title.text = "Tower is Maximum Level.";
            upgradeText.text = "Tower can not be upgradeded further";
            //Required Items
            if (requiredItemsPanel != null)
            {
                // clear existing children
                for (int i = requiredItemsPanel.childCount - 1; i >= 0; i--)
                {
                    Destroy(requiredItemsPanel.GetChild(i).gameObject);
                }
            }
                return;
        }
        else
        {
            if (!towerUpgradeManager.SelectRequiredItems())
            {
                upgradeText.text = "You do not posess the required items";
            }
            else
            {
                towerUpgradeManager.ComputeUpgradePreview();
                SetUpgradeText();
            }
        }

        //Title Text
        title.text = "Upgrade " + towerUpgradeManager.tower.name + " to Level " + (towerUpgradeManager.tower.level+1) + "?";
        
        //Required Items
		if (requiredItemsPanel != null)
		{
			// clear existing children
			for (int i = requiredItemsPanel.childCount - 1; i >= 0; i--)
			{
				Destroy(requiredItemsPanel.GetChild(i).gameObject);
			}

			// get requirements for next level (indexed by current level)
			int reqIndex = towerUpgradeManager.tower.level;
			var reqArray = towerUpgradeManager.upgradeRequirements;
			if (reqArray != null && reqIndex >= 0 && reqIndex < reqArray.Length)
			{
				var req = reqArray[reqIndex];
				if (req != null && req.items != null)
				{
					foreach (var entry in req.items)
					{
						if (entry == null || entry.lootItem == null || itemUIPrefab == null) continue;
						var go = Instantiate(itemUIPrefab, requiredItemsPanel);
						var ui = go.GetComponent<ItemUIManager>();
						if (ui != null) ui.UpdateItemUI(entry.lootItem, Mathf.Max(0, entry.quantity));
					}
				}
			}
		}
    }

    void UpgradeButtonOnClick() 
    {
        towerUpgradeManager.ApplyUpgrade();
    }

    void CloseButtonOnClick()
    {
        EnableUI(false);
    }

    void SetUpgradeText()
    {
        if (towerUpgradeManager == null) return;

        upgradeText.text =
        $"Health: {towerUpgradeManager.tower.maxHealth} (+{towerUpgradeManager.deltaH}) \t\t" +
        $"Attack Damage: {towerUpgradeManager.tower.attackDamage} (+{towerUpgradeManager.deltaD}) \t\t" +
        $"Attack Rate: {towerUpgradeManager.tower.attackSpeed} (+{towerUpgradeManager.deltaS}) \t\t" +
        $"Attack Range: {towerUpgradeManager.tower.attackRadius} (+{towerUpgradeManager.deltaR})";
    }
}
