using System;
using UnityEngine;

[Serializable]
public class DDAChange
{
    public string prev_difficulty;
    public string new_difficulty;
    
    public DDAChange(
        string prev_difficulty,
        string new_difficulty
    )
    {
        this.prev_difficulty = prev_difficulty;
        this.new_difficulty = new_difficulty;
    }
}
