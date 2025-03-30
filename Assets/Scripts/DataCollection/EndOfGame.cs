using System;
using UnityEngine;

[Serializable]
public class EndOfGame
{
    public string difficulty;
    public string playerScore;
    public string opponentScore;
    
    public EndOfGame(
        string difficulty,
        string playerScore,
        string opponentScore
    )
    {
        this.difficulty = difficulty;
        this.playerScore = playerScore;
        this.opponentScore = opponentScore;
    }
}
