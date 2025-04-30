using System;
using UnityEngine;

[Serializable]
public class EndOfGame
{
    public string difficulty;
    public string playerScore;
    public string opponentScore;
    public AgentCollectedData playerCollectedData;
    public AgentCollectedData opponentCollectedData;
    public PerformanceLog performanceLog;
    
    public EndOfGame(
        string difficulty,
        string playerScore,
        string opponentScore, 
        AgentCollectedData playerCollectedData,
        AgentCollectedData opponentCollectedData,
        PerformanceLog performanceLog
    )
    {
        this.difficulty = difficulty;
        this.playerScore = playerScore;
        this.opponentScore = opponentScore;
        this.playerCollectedData = playerCollectedData;
        this.opponentCollectedData = opponentCollectedData;
        this.performanceLog = performanceLog;
    }
}
