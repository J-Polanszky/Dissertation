using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager instance;
    TerrainPopulator terrainPopulator;

    
    bool gameStarted = false;
    
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
        Time.timeScale = 20f;
        // The default State Machine should be normal difficulty
        GameData.Difficulty = 1;
        terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        // StartGame();
    }
    
    public void StartGame()
    {
        Debug.Log("Starting Game");
        RunGameStartFunctions();
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
        if (gameStarted)
        {
            terrainPopulator.ResetTerrain();
            //TODO: Reset SM and RL agent positions
        }
            
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

        //TODO: Restart Training Scene.
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
