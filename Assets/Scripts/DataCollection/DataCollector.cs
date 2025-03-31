using System;
using UnityEngine;

public class DataCollector : MonoBehaviour
{
    public DataCollector Instance { get; private set; }

    private string playerID, playtestName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string PlayerID
    {
        get => playerID;
        set => playerID = value;
    }

    public string PlaytestName
    {
        get => playtestName;
        set => playtestName = value;
    }

    public void RecordEndOfGameEvent()
    {
        // To figure out where to set this properly
        PlayerID = "12345";

        string difficulty = GameData.Difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
        string playerScore = GameData.PlayerData.Score.ToString();
        string opponentScore = GameData.MachineData.Score.ToString();

        EndOfGame endOfGame = new EndOfGame(
            difficulty,
            playerScore,
            opponentScore
        );
        
        FirebaseHandler.Instance.SendEndOfGameEvent(PlayerID, PlaytestName, endOfGame);
    }

    public void RecordTimestampEvent()
    {
        string timestamp = (GameData.InitialTime - GameData.TimeLeft).ToString();
        string difficulty = GameData.Difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
        string timePassed = (GameData.InitialTime - GameData.TimeLeft).ToString();
        
        // Player data
        
        string playerTimeSpentMining = GameData.PlayerData.TimeSpentMining.ToString();
        string playerTimeSpentWTravelling =
            Math.Round((float) (GameData.InitialTime - GameData.TimeLeft) - GameData.PlayerData.TimeSpentMining, 2).ToString();
        string playerScore = GameData.PlayerData.Score.ToString();
        string playerInventoryUsed = GameData.PlayerData.TotalInventory.ToString();
        
        int totalScoreOfInventory = 0;
        
        foreach (var (key, value) in GameData.PlayerData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }
        
        string playerScoreOfInventory = totalScoreOfInventory.ToString();
        
        AgentCollectedData playerCollectedData = new AgentCollectedData(
            playerTimeSpentMining,
            playerTimeSpentWTravelling.ToString(),
            playerScore,
            playerInventoryUsed,
            playerScoreOfInventory
        );
        
        // Opponent data
        
        string opponentTimeSpentMining = GameData.MachineData.TimeSpentMining.ToString();
        string opponentTimeSpentWTravelling =
            Math.Round((float) (GameData.InitialTime - GameData.TimeLeft) - GameData.MachineData.TimeSpentMining, 2).ToString();
        string opponentScore = GameData.MachineData.Score.ToString();
        string opponentInventoryUsed = GameData.MachineData.TotalInventory.ToString();
        
        totalScoreOfInventory = 0;
        
        foreach (var (key, value) in GameData.MachineData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }
        
        string opponentScoreOfInventory = totalScoreOfInventory.ToString();
        
        AgentCollectedData opponentCollectedData = new AgentCollectedData(
            opponentTimeSpentMining,
            opponentTimeSpentWTravelling.ToString(),
            opponentScore,
            opponentInventoryUsed,
            opponentScoreOfInventory
        );
        
        TimeStampEvent timestampEvent = new TimeStampEvent(
            difficulty,
            timePassed,
            playerCollectedData,
            opponentCollectedData
        );

        FirebaseHandler.Instance.SendTimestampEvent(PlayerID, PlaytestName, timestamp, timestampEvent);
    }
}