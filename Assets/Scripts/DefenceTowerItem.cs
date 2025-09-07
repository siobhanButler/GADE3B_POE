using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefenceTowerItem : MonoBehaviour
{
    public UIManager uiManager;

    public Button towerPurchaseButton;
    public TextMeshProUGUI towerNameText;
    public TextMeshProUGUI towerDescription;
    public TextMeshProUGUI towerCost;
    public Image towerImage;

    public GameObject towerPrefab;
    public Sprite towerSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiManager = GetComponentInParent<UIManager>();
        if(uiManager == null)
        {
            Debug.Log("DefenceTowerItem: No ui manager");
        }
        if (towerPurchaseButton != null) towerPurchaseButton.onClick.AddListener(OnTowerPurchaseButtonClick);

        if(towerPrefab == null)
        {
            Debug.Log("DefenceTowerItem: No tower prefab assigned");
            return;
        }
        TowerManager towerManager = towerPrefab.GetComponent<TowerManager>();
        if (towerManager == null)
        {
            Debug.Log("DefenceTowerItem: No tower manager on tower prefab");
            return;
        }

        // Check if health component exists
        if (towerManager.health == null)
        {
            Debug.Log("DefenceTowerItem: No health component on tower manager");
            return;
        }

        // Check if attack component exists
        if (towerManager.attack == null)
        {
            Debug.Log("DefenceTowerItem: No attack component on tower manager");
            return;
        }

        float health = towerManager.health.maxHealth;
        float damage = towerManager.attack.attackDamage;
        float range = towerManager.attack.rangeRadius;
        float fireRate = towerManager.attack.attackSpeed;
        int cost = towerManager.cost;

        towerNameText.text = towerPrefab.name;
        towerDescription.text = "Health: " + health + "\nDamage: " + damage + "\n Fire Rate: " + fireRate + "\nRange: " + range;
        towerCost.text = cost.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     void OnTowerPurchaseButtonClick()
    {
        if(uiManager == null || uiManager.towerLocationManager == null)
        {
            Debug.Log("DefenceTowerItem: ui manager or tower location null");
            return;
        }

        uiManager.towerLocationManager.PurchaseTower(towerPrefab);
        uiManager.EnableMenuPanel(false);
    }
}
