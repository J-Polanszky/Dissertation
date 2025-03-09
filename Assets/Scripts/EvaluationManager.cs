using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EvaluationManager : MonoBehaviour
{
    public static EvaluationManager instance;
    TerrainPopulator terrainPopulator;

    GameObject stateMachine, RLAgent;
    
    Vector3 playerStartPos, machineStartPos;
    
    bool gameStarted = false;
    
    Coroutine timer;

    List<float> machineScore = new();
    List<float> playerScore = new();

    public int difficulty = 2;

    private int runTimes = 0;
    
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
        Debug.developerConsoleEnabled = false;
        Debug.unityLogger.logEnabled = false;
        
        stateMachine = GameObject.FindGameObjectWithTag("Player");
        RLAgent = GameObject.FindGameObjectWithTag("Agent");
        
        playerStartPos = stateMachine.transform.position;
        machineStartPos = RLAgent.transform.position;
        
        Time.timeScale = 20f;
        GameData.Difficulty = difficulty;

        terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        
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
        Debug.developerConsoleEnabled = true;
        Debug.unityLogger.logEnabled = true;
        Debug.Log("Quitting Game");
        Debug.Log("Agent Score: " + machineScore);
        Debug.Log("Agent Score avg: " + machineScore.Average());
        Debug.Log("Player Score: " + playerScore);
        Debug.Log("Player Score avg: " + playerScore.Average());
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
        
        runTimes++;
        SpawnOres();
        GameData.PlayerData.Reset();
        GameData.MachineData.Reset();
        timer = StartCoroutine(CountDown());
    }
    
    void RunGameRestartFunctions()
    {
        if (runTimes == 10)
        {
            QuitGame();
            return;
        }
            
        terrainPopulator.ResetTerrain();
        if (timer != null)
            StopCoroutine(timer);
        
        stateMachine.transform.position = playerStartPos;
        RLAgent.transform.position = machineStartPos;
        
        runTimes++; 
        SpawnOres();
        GameData.PlayerData.Reset();
        GameData.MachineData.Reset();
        stateMachine.GetComponent<SMAgent>().Reset();
        RLAgent.GetComponent<RLAgent>().Reset();
        StartCoroutine(CountDown());
    }
    
    IEnumerator CountDown()
    {
        GameData.TimeLeft = GameData.InitialTime;

        while (GameData.TimeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            GameData.TimeLeft--;
        }

        playerScore.Add(GameData.PlayerData.Score);
        machineScore.Add(GameData.MachineData.Score);

        RLAgent.GetComponent<RLAgent>().CustomEndEpisode();
        
        // Issue with state machine not resetting properly, even though scene is identical to training scene.   
        // QuitGame();
    }
    
    void SpawnOres()
    {
        // Easy
        if (GameData.Difficulty == 0)
            terrainPopulator.SetOreSpawns(20, 10,30,60);
        
        // Normal
        else if (GameData.Difficulty == 1)
            terrainPopulator.SetOreSpawns(15, 5, 25, 70);
        
        // Hard
        else
            terrainPopulator.SetOreSpawns(10, 5, 20, 75);
    }
    
}
