using System;
using UnityEngine;

[Serializable]
public class AgentCollectedData
{
    public string timeSpentMining;
    public string timeSpentWTravelling;
    public string score;
    public string inventoryUsed;
    public string scoreOfInventory;

    public AgentCollectedData(
        string timeSpentMining,
        string timeSpentWTravelling,
        string score,
        string inventoryUsed,
        string scoreOfInventory
    )
    {
        this.timeSpentMining = timeSpentMining;
        this.timeSpentWTravelling = timeSpentWTravelling;
        this.score = score;
        this.inventoryUsed = inventoryUsed;
        this.scoreOfInventory = scoreOfInventory;
    }
}

[Serializable]
public class TimeStampEvent
{
    // public string timestamp;
    // public string sceneName;
    // public string playerID;
    public string difficulty;
    public string timePassed;
    public AgentCollectedData playerCollectedData;
    public AgentCollectedData opponentCollectedData;

    // Maybe make the time stamp the id of the event, and like that it will automatically be under the same playerID, and right scene name.
    public TimeStampEvent(
        // string sceneName, 
        // string playerID, 
        string difficulty,
        string timePassed,
        AgentCollectedData playerCollectedData,
        AgentCollectedData opponentCollectedData
        // string timestamp = null
    )
    {
        // this.timestamp = timestamp ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // this.sceneName = sceneName;
        // this.playerID = playerID;
        this.difficulty = difficulty;
        this.timePassed = timePassed;
        this.playerCollectedData = playerCollectedData;
        this.opponentCollectedData = opponentCollectedData;
    }
}