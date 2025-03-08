using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EvaluationManager : MonoBehaviour
{
    public static EvaluationManager instance;
    TerrainPopulator terrainPopulator;

    GameObject stateMachine, RLAgent;
    
    Vector3 playerStartPos, machineStartPos;
    
    bool gameStarted = false;

    List<float> machineScore = new();
    List<float> playerScore = new();

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
        stateMachine = GameObject.FindGameObjectWithTag("Player");
        RLAgent = GameObject.FindGameObjectWithTag("Agent");
        
        playerStartPos = stateMachine.transform.position;
        machineStartPos = RLAgent.transform.position;
        
        Time.timeScale = 20f;
        // The default State Machine should be normal difficulty
        GameData.Difficulty = 2;
        terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        StartGame();
    }
    
    public void StartGame()
    {
        Debug.Log("Starting Game");
        RunGameStartFunctions();
    }
    
    public void QuitGame()
    {
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
        if (runTimes == 10)
        {
            QuitGame();
            return;
        }
            
        if (gameStarted)
        {
            terrainPopulator.ResetTerrain();
            stateMachine.transform.position = playerStartPos;
            RLAgent.transform.position = machineStartPos;
        }
            
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
        StartGame();
    }
    
    void SpawnOres()
    {
        // Making the agent always play with hard ore spawns
        terrainPopulator.SetOreSpawns(10, 5, 20, 75);
    }
    
}
