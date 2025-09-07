using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject proceduralGeneratorPrefab;
    public GameObject playerUIPrefab;

    [Header("Game Properties")]
    public GameState gameState = GameState.Playing;
    public int currentLevel = 1;
    public int levelReward = 100;   //how many coins gained per level

    public GameObject player;   //Player instance
    public PlayerManager playerManager;
    public GameObject ui;       //UI instance
    public UIManager uiManager;

    public GameObject proceduralGenerator; //Procedural Generator instance
    public RoomGenerator roomGenerator;
    public MeshGenerator meshGenerator;
    public PathGenerator pathGenerator;

    public List<GameObject> enemies;
    public List<SpawnerManager> spawnerManagers;

    private bool spawnersDone = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        if(spawnersDone)    //after spawners are done, start checking if all enemies are dead
        {
            WinGame();
        }
    }

    void StartGame()
    {
        Time.timeScale = 1.0f;
        
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.Log("GameManager StartGame(): No player found, instantiating Player");
            player = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        }
        playerManager = player.GetComponent<PlayerManager>();
        //playerManager.Setup();

        ui = GameObject.FindWithTag("PlayerUI");
        if (ui == null)
        {
            Debug.Log("GameManager StartGame(): No ui found, instantiating UI");
            ui = Instantiate(playerUIPrefab, transform.position, Quaternion.identity);
        }
        uiManager = ui.GetComponent<UIManager>();
        if (uiManager != null)
        {
            uiManager.Setup(this);
            
            // Ensure UI renders properly on top
            Canvas canvas = uiManager.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = player.GetComponent<PlayerCameraController>().cam;
                canvas.sortingOrder = 100; // Ensure UI renders on top
            }
        }

        // Always create a new procedural generator to avoid finding old ones (during next level)
        Debug.Log("GameManager StartGame(): Creating new procedural generator");
        proceduralGenerator = Instantiate(proceduralGeneratorPrefab, new Vector3(0,0,0), Quaternion.identity);
        Debug.Log("GameManager StartGame(): Created procedural generator " + proceduralGenerator.name);
        roomGenerator = proceduralGenerator.GetComponent<RoomGenerator>();
        roomGenerator.Setup(currentLevel);  //impliment properly later using difficulty scaler
        meshGenerator = proceduralGenerator.GetComponent<MeshGenerator>();
        pathGenerator = proceduralGenerator.GetComponent<PathGenerator>();

        enemies = new List<GameObject>();
        spawnerManagers = new List<SpawnerManager>();
    }

    public void StartNextLevel()
    {
        // Reset game state for next level
        gameState = GameState.Playing;
        spawnersDone = false;
        currentLevel++;
        
        if (playerManager != null)
        {
            playerManager.coins += levelReward;
        }

        NewLevel();
    }

    public void RestartGame()
    {
        // reload current scene
        Time.timeScale = 1f; // Make sure time scale is reset
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NewLevel()
    {
        // Reset spawners done flag
        spawnersDone = false;
        
        // Clean up enemies list and objects
        if (enemies != null)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] != null)
                {
                    Destroy(enemies[i]);
                }
            }
            enemies.Clear();
        }

        // Reset spawner list
        if (spawnerManagers != null)
        {
            spawnerManagers.Clear();
        }

        // Only destroy proceduralGenerator if it exists (not already destroyed)
        if (proceduralGenerator != null)
        {
            Destroy(proceduralGenerator);
            proceduralGenerator = null;
        }
        
        StartGame();
    }

    public void GameOver()
    {
        Pause(true);    //pause the game
        gameState = GameState.GameOver;
        uiManager.EnableMenuPanel(true);
    }

    public void WinGame()
    {
        Debug.Log("win: checking win");
        spawnersDone = false;

        foreach (SpawnerManager spawner in spawnerManagers)
        {
            if (spawner.wavesCompleted == false)
            {
                spawnersDone = false;    //if even one spawner is not done, set true then exit
                return;   
            }
            else
            {
                spawnersDone = true;
            }
        }
        
        // Remove dead enemies
        enemies.RemoveAll(enemy => enemy == null);

        Debug.Log("win" + enemies.Count + "enemies left");

        if (spawnersDone && enemies.Count == 0)     //if all spawners are done and there are no enemies left
        {
            Pause(true);    //pause the game
            gameState = GameState.Win;
            uiManager.EnableMenuPanel(true);
        } 
    }

    public void Pause(bool paused)
    {
        if (paused)
        {
            gameState = GameState.Paused;
            Time.timeScale = 0f; // Pause game time
            uiManager.EnableMenuPanel(true);
        }
        else
        {
            gameState = GameState.Playing;
            Time.timeScale = 1f; // Resume game time
            uiManager.EnableMenuPanel(false);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

}

public enum GameState
{
    Playing,
    Paused,
    GameOver,
    Win
}
