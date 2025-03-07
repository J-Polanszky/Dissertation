using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager instance;
    TerrainPopulator terrainPopulator;

    GameObject stateMachine, RLAgent;
    
    Vector3 playerStartPos, machineStartPos;
    
    bool gameStarted = false;

    private Coroutine timer;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else
            Destroy(gameObject);
        
    }

    private void Start()
    {
        // The Training all seems to work without ever breaking, thus the dev console will be disabled to save on resources.
        Debug.developerConsoleEnabled = false;
        Debug.unityLogger.logEnabled = false;
        
        stateMachine = GameObject.FindGameObjectWithTag("Player");
        RLAgent = GameObject.FindGameObjectWithTag("Agent");
        
        playerStartPos = stateMachine.transform.position;
        machineStartPos = RLAgent.transform.position;
        
        Time.timeScale = 20;
        // The default State Machine should be normal difficulty
        GameData.Difficulty = 1;
        terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        // StartGame();
    }
    
    public void StartGame()
    {
        Debug.Log("Starting Game");
        if (!gameStarted)
            RunGameStartFunctions();
        else
            RunGameRestartFunctions();
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    void RunGameStartFunctions()
    {
        gameStarted = true;
        GameObject[] deposits = GameObject.FindGameObjectsWithTag("Deposit");
        foreach (GameObject deposit in deposits)
        {
            if (deposit.name == "PlayerDeposit")
                deposit.GetComponent<DepositBuilding>().agentData = GameData.PlayerData;
            else
                deposit.GetComponent<DepositBuilding>().agentData = GameData.MachineData;
        }
        
        SpawnOres();
        GameData.PlayerData.Reset();
        GameData.MachineData.Reset();
        timer = StartCoroutine(CountDown());
    }
    
    void RunGameRestartFunctions()
    {
        // Clear console if it is open
        Debug.ClearDeveloperConsole();
        terrainPopulator.ResetTerrain();
        if (timer != null)
            StopCoroutine(timer);
        
        stateMachine.transform.position = playerStartPos;
        RLAgent.transform.position = machineStartPos;
        
        SpawnOres();
        GameData.PlayerData.Reset();
        GameData.MachineData.Reset();
        stateMachine.GetComponent<SMAgent>().Reset();
        RLAgent.GetComponent<RLAgent>().Reset();
        timer = StartCoroutine(CountDown());
    }
    
    IEnumerator CountDown()
    {
        GameData.TimeLeft = GameData.InitialTime;

        while (GameData.TimeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            GameData.TimeLeft--;
        }

        RLAgent.GetComponent<RLAgent>().CustomEndEpisode();
    }
    
    void SpawnOres()
    {
        // Easy
        if (GameData.Difficulty == 3)
            terrainPopulator.SetOreSpawns(40, 10,30,60);
        
        // Normal
        else if (GameData.Difficulty == 2)
            terrainPopulator.SetOreSpawns(30, 5, 25, 70);
        
        // Hard
        else
            terrainPopulator.SetOreSpawns(20, 5, 20, 75);
    }
    
}
