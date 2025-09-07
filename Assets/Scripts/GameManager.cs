using System;
using System.Collections.Generic;
using UnityEngine;

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

    public List<GameObject> Enemies;
    public List<SpawnerManager> spawnerManagers;

    void Awake()
    {
        Enemies = Enemies ?? new List<GameObject>();
        spawnerManagers = spawnerManagers ?? new List<SpawnerManager>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartGame()
    {
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
            uiManager.GetComponent<Canvas>().worldCamera = player.GetComponent<PlayerCameraController>().cam;
        }

        proceduralGenerator = GameObject.FindWithTag("ProceduralGenerator");
        if (proceduralGenerator == null)
        {
            Debug.Log("GameManager StartGame(): No proceduralGenerator found, instantiating procedural generator");
            proceduralGenerator = Instantiate(proceduralGeneratorPrefab, new Vector3(0,0,0), Quaternion.identity);
        }
        roomGenerator = proceduralGenerator.GetComponent<RoomGenerator>();
        roomGenerator.Setup(currentLevel);  //impliment properly later using difficulty scaler
        meshGenerator = proceduralGenerator.GetComponent<MeshGenerator>();
        pathGenerator = proceduralGenerator.GetComponent<PathGenerator>();
    }

    public void StartNextLevel()
    {
        currentLevel++;
        if (playerManager != null)
        {
            playerManager.coins += levelReward;
        }

        NewLevel();
    }

    public void RestartGame()
    {
        Destroy(player);
        NewLevel();
    }

    public void NewLevel()
    {
        // Clean up enemies list and objects
        if (Enemies != null)
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i] != null)
                {
                    Destroy(Enemies[i]);
                }
            }
            Enemies.Clear();
        }

        // Reset spawner list
        if (spawnerManagers != null)
        {
            spawnerManagers.Clear();
        }

        Destroy(proceduralGenerator);
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
        bool spawnersDone = false;

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

        if (spawnersDone && Enemies.Count == 0)     //if all spawners are done and there are no enemies left
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
