using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game controller managing:
/// - Game state and level progression
/// - Player resources and health
/// - Procedural room generation
/// - Enemy and spawner management
/// - Win/lose conditions
/// Uses singleton pattern for global access.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }    //Singleton Pattern
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject proceduralGeneratorPrefab;
    public GameObject playerUIPrefab;

    [Header("Game Properties")]
    public GameState gameState = GameState.Playing;
    public int currentLevel = 1;
    public float wavesAmount = 3f; // base float wave count that increases over levels and performance
    public int levelReward = 100;   //how many coins gained per level
    public List<GameObject> enemies;
    public List<SpawnerManager> spawnerManagers;
    public bool spawnersDone = false;

    public GameObject player;   //Player instance
    public PlayerManager playerManager;
    public int startingCoins = 100;     //how many coins at the beginning of the game, later how many from previous level
    public GameObject ui;       //UI instance
    public UIManager uiManager;

    public GameObject proceduralGenerator; //Procedural Generator instance
    public RoomGenerator roomGenerator;
    public MeshGenerator meshGenerator;
    public PathGenerator pathGenerator;


    private void Awake()
    {
        //SingletonPattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Persist GameManager across scenes
        SceneManager.sceneLoaded += OnSceneLoaded;  //Because Start only ever plays once
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)     //replaces the Start() function for scene loads
    {
        Debug.Log("Scene loaded: " + scene.name);
        StartGame();
    }

    void Start()
    {
        gameState = GameState.Playing;
        StartGame();
    }
 
    void Update()      // Update is called once per frame
    {
        if (spawnersDone && gameState != GameState.Win)    //after spawners are done, start checking if all enemies are dead, gameState.Win to prevent multiple calls
        {
            WinGame();
        }
    }

    void StartGame()
    {
        Debug.Log("GameManager StartGame() Running");
        gameState = GameState.Playing;
        Time.timeScale = 1.0f;

        if(player != null || proceduralGenerator != null || ui != null)
        {
            Debug.LogWarning("GameManager StartGame(): One or more classes are not null during initialization");
            return;
        }

        //CREATING PLAYER
        player = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        playerManager = player.GetComponent<PlayerManager>();
        playerManager.Setup(startingCoins);
        Debug.Log("GameManager StartGame() Created Player: " + player.name);

        //CREATING UI
        ui = Instantiate(playerUIPrefab, transform.position, Quaternion.identity);
        uiManager = ui.GetComponent<UIManager>();
        uiManager.Setup(this);
        Debug.Log("GameManager StartGame() Created ui: " + ui.name);

        //CREATING PROCEDURAL GENERATOR
        proceduralGenerator = Instantiate(proceduralGeneratorPrefab, new Vector3(0,0,0), Quaternion.identity);
        roomGenerator = proceduralGenerator.GetComponent<RoomGenerator>();
        roomGenerator.Setup(currentLevel);  //POE next: implement properly later using difficulty scaler
        meshGenerator = proceduralGenerator.GetComponent<MeshGenerator>();
        pathGenerator = proceduralGenerator.GetComponent<PathGenerator>();
        Debug.Log("GameManager StartGame() Created proceduralGenerator " + proceduralGenerator.name);

        //CREATING enemies and spawnerManagers if they don't already exist
        if (enemies == null && spawnerManagers == null)
        {
            enemies = new List<GameObject>();
            spawnerManagers = new List<SpawnerManager>();
            Debug.Log("GameManager StartGame() Creating enemies and spawnerManagers list");
        }  
    }

    public void StartNextLevel()
    {
        //Resetting to default level
        gameState = GameState.Playing;         //Reset to play
        currentLevel++;                        //incriment by 1
        wavesAmount += 1f;                     // baseline: increase total waves each level
        Debug.Log($"GameManager: Level up to {currentLevel}. wavesAmount now {wavesAmount:F2}");
        levelReward = Mathf.RoundToInt(levelReward * 1.2f);    //each level gives 20% more coins upon completion
        enemies.Clear();                       //clear the enemies, as they should all be destroyed upon game win/next level
        spawnerManagers.Clear();               //clear the spawnerManagers
        spawnersDone = false;                  //reset spawners done
        startingCoins += playerManager.coins;  //how many coins at the beginning of the game = player's coins from last level

        //Destroying and setting all classes to null
            Destroy(player);
            player = null;
            playerManager = null;
            Destroy(ui);
            ui = null;
            uiManager = null;

            Destroy(proceduralGenerator);
            proceduralGenerator = null;
            roomGenerator = null;
            meshGenerator = null;
            pathGenerator = null;

        //Reloading Scene (with this game object being set to DontDestroyOnLoad() in awake)
        Time.timeScale = 1f;       // Make sure time scale is reset
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartGame()
    {
        // reload current scene
        Time.timeScale = 1f; // Make sure time scale is reset
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Destroy(this.gameObject); // Destroy current GameManager (because it is set to DontDestroyOnLoad), new one will be created with scene load
    }

    public void GameOver()
    {
        Pause(true);    //pause the game
        gameState = GameState.GameOver;
        uiManager.EnableMenuPanel(true);
    }

    public void WinGame()
    {
        Debug.Log("GameManager WinGame(): checking win");
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

        // Checking remaining enemies
        if (spawnersDone && enemies.Count == 0)     //if all spawners are done and there are no enemies left
        {
            //GAME HAS BEEN WON LOGIC
            Pause(true);    //pause the game
            gameState = GameState.Win;
            uiManager.EnableMenuPanel(true);

            if (playerManager != null)
            {
                playerManager.coins += levelReward;
                startingCoins = playerManager.coins; // Carry over coins to next level
            }
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

        // For editor testing, stop play mode if running in editor
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
    }

}

public enum GameState
{
    Playing,
    Paused,
    GameOver,
    Win
}
