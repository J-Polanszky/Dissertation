using System.Collections.Generic;
using UnityEngine;

public class AgentData
{
    public Dictionary<string, List<int>> inventory = new()
    {
        { "Gold", new List<int>() {0,0} },
        { "Silver", new List<int>() {0,0} },
        { "Iron", new List<int>() {0,0} }
    };

    public System.Action<int> onScoreUpdated;
    private int score = 0;
    
    public int Score { get { return score; } set { score = value; onScoreUpdated?.Invoke(value); } }
}

public static class GameData
{
    // Easy - 3
    // Medium - 2
    // Hard - 1
    private static int difficulty = 3;

    private static AgentData playerData = new();
    
    private static AgentData machineData = new();

    private static Dictionary<string, int> invLimits = new()
    {
        { "Gold", 3 },
        { "Silver", 5 },
        { "Iron", 7 }
    };

    public static int Difficulty { get => difficulty; set => difficulty = value; }
    
    public static AgentData PlayerData { get => playerData; set => playerData = value; }
    
    public static AgentData MachineData { get => machineData; set => machineData = value; }

    private static void AddToInventory(AgentData agentData, string oreName, int oreScore)
    {
        if (agentData.inventory[oreName][0] >= invLimits[oreName])
            throw new System.Exception("Inventory is full");
        
        agentData.inventory[oreName][0]++;
        agentData.inventory[oreName][1] += oreScore;
        
        Debug.Log(agentData.inventory[oreName][0] + " " + agentData.inventory[oreName][1]);
    }
    
    public static void AddToPlayerInventory(string oreName, int oreScore)
    {
        AddToInventory(playerData, oreName, oreScore);
    }

    public static void AddToMachineInventory(string oreName, int oreScore)
    {
        AddToInventory(playerData, oreName, oreScore);
    }
    
}
