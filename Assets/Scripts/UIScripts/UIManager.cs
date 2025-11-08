using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    public TowerLocationManager towerLocationManager;
    public InventoryUIManager inventoryUIManager;

    [Header("Buttons")]
    public Button pauseButton;
    public Button InventoryButton;

    [Header("Gameplay Panel")]
    public RectTransform GameplayPanel;
    public TextMeshProUGUI coinAmountText;
    public TextMeshProUGUI levelText;
    public Slider playerHealthBar;

    [Header("Menu Panel")]
    public RectTransform menuPanel;
    public TextMeshProUGUI menuText;
    public Button restartButton;
    public Button resumeButton;
    public Button exitButton;

    [Header("Tower Location Panel")]
    public RectTransform towerLocationPanel;
    public TextMeshProUGUI titleText;
    //public ScrollRect towerPurchaseScrollView;
    //public RectTransform viewport;
    //public RectTransform content;
    public Button exitTowerLocationButton;

    [Header("Tower Location Panel")]
    public RectTransform towerUpgradePanel;
    public TowerUpgradeUIManager towerUpgradeUIManager;
    public towerUpgradeManager towerUpgradeManager;

    // Start method removed - initialization handled in Setup()

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying && gameManager != null && gameManager.playerManager != null)
        {
            if (coinAmountText != null)
            {
                coinAmountText.text = gameManager.playerManager.coins.ToString();
            }
            if (playerHealthBar != null && gameManager.playerManager.mainTowerHealth != null)
            {
                float maxH = Mathf.Max(0.0001f, gameManager.playerManager.mainTowerHealth.maxHealth);
                float frac = Mathf.Clamp01(gameManager.playerManager.mainTowerHealth.currentHealth / maxH);
                playerHealthBar.value = frac;
            }
        }
    }

    public void Setup(GameManager pGameManager)
    {
        gameManager = pGameManager;
        // Try to auto-bind any missing references under this UI hierarchy
        if (pauseButton == null) pauseButton = GetComponentInChildren<Button>(true);
        if (InventoryButton == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true)) { if (b.name == "InventoryButton") { InventoryButton = b; break; } }
        }
		if (inventoryUIManager == null)
		{
			inventoryUIManager = GetComponentInChildren<InventoryUIManager>(true);
		}
        if (menuPanel == null || resumeButton == null || exitButton == null || restartButton == null)
        {
            // Attempt to find elements safely
            if (menuPanel == null)
            {
                var panels = GetComponentsInChildren<RectTransform>(true);
                foreach (var rt in panels) { if (rt.name == "MenuPanel") { menuPanel = rt; break; } }
            }
            if (resumeButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true)) { if (b.name == "ResumeButton") { resumeButton = b; break; } }
            }
            if (exitButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true)) { if (b.name == "ExitButton") { exitButton = b; break; } }
            }
            if (restartButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true)) { if (b.name == "RestartButton") { restartButton = b; break; } }
            }
        }

        // Pause button
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseButtonClick);
        if (InventoryButton != null) InventoryButton.onClick.AddListener(OnInventoryButtonClick);
        // Menu panel
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeButtonClick);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClick);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartButtonClick);
        // Tower location panel
        if (exitTowerLocationButton != null) exitTowerLocationButton.onClick.AddListener(OnExitTowerLocationButtonClick);

		// Bind the inventory UI to the player's inventory so its events reflect the correct instance
		if (inventoryUIManager != null && gameManager != null && gameManager.playerManager != null)
		{
			inventoryUIManager.BindInventory(gameManager.playerManager.inventory);
		}

        EnableMenuPanel(false);
        EnableTowerLocationPanel(false, null);
        EnableTowerUpgradePanel(false, null);
        inventoryUIManager.CloseInventory();

        levelText.text = gameManager.currentLevel.ToString();
    }

    public void EnableMenuPanel(bool enable)
    {
        switch (gameManager.gameState)
        {
            case GameState.Paused:
                menuText.text = "Paused";
                restartButton.GetComponentInChildren<TextMeshProUGUI>().text = "Restart";
                restartButton.gameObject.SetActive(true);
                resumeButton.gameObject.SetActive(true);
                exitButton.gameObject.SetActive(true);
                break;
            case GameState.GameOver:
                menuText.text = "Game Over";
                restartButton.GetComponentInChildren<TextMeshProUGUI>().text = "Restart";
                restartButton.gameObject.SetActive(true);
                resumeButton.gameObject.SetActive(false);
                exitButton.gameObject.SetActive(true);
                break;
            case GameState.Win:
                menuText.text = "You Win!";
                restartButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Level";
                restartButton.gameObject.SetActive(true);
                resumeButton.gameObject.SetActive(false);
                exitButton.gameObject.SetActive(true);
                break;
            case GameState.Playing:
                // Should not enable menu in playing state
                enable = false;
                break;
            default:
                // Unknown state, disable menu
                enable = false;
                break;
        }

        menuPanel.gameObject.SetActive(enable);
        if(towerLocationPanel.gameObject.activeInHierarchy) //if its enabled
        {
            EnableTowerLocationPanel(false, null);
        }
    }

    public void EnableTowerLocationPanel(bool enable, TowerLocationManager caller)
    {
        towerLocationPanel.gameObject.SetActive(enable);
        towerLocationManager = caller;
        
        // Ensure tower location panel renders on top
        if (enable)
        {
            Canvas panelCanvas = towerLocationPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = towerLocationPanel.GetComponentInParent<Canvas>();
            }
            
            if (panelCanvas != null)
            {
                panelCanvas.sortingOrder = 200; // Higher than main UI
            }
        }
    }

    public void EnableTowerUpgradePanel(bool enable, towerUpgradeManager caller)
    {
        if(towerUpgradePanel == null)
        {
            Debug.Log("UIManager EnableTowerUpgradePanel(): towerUpgradePanel is null!");
            return;
        }
        towerUpgradeManager = caller;
		towerUpgradeUIManager.EnableUI(enable);
		towerUpgradePanel.gameObject.SetActive(enable);

        // Ensure tower location panel renders on top
        if (enable)
        {
            Canvas panelCanvas = towerUpgradePanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = towerUpgradePanel.GetComponentInParent<Canvas>();
            }

            if (panelCanvas != null)
            {
                panelCanvas.sortingOrder = 200; // Higher than main UI
            }
        }
    }

    void OnPauseButtonClick()
    {
        gameManager.Pause(true);
    }

    void OnResumeButtonClick()
    {
        gameManager.Pause(false);
        EnableMenuPanel(false);
    }

    void OnExitButtonClick()
    {
        gameManager.ExitGame();
    }

    void OnRestartButtonClick()
    {
        switch (gameManager.gameState)
        {
            case GameState.Paused:
                gameManager.RestartGame();
                break;
            case GameState.GameOver:
                gameManager.RestartGame();
                break;
            case GameState.Win:
                gameManager.StartNextLevel();
                break;
        }
        EnableMenuPanel(false);
    }

    void OnExitTowerLocationButtonClick()
    {
        EnableTowerLocationPanel(false, null);
    }

    void OnInventoryButtonClick()
    {
        if (inventoryUIManager == null || inventoryUIManager.panel_Inventory == null) return;
        bool next = !inventoryUIManager.panel_Inventory.activeSelf;
        inventoryUIManager.panel_Inventory.SetActive(next);
    }
}
