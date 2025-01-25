using System.Collections.Generic;
using UnityEngine;

public class AgentData
{
    // Updated it to use a class to make it more readable than a simple list, even if it makes it a bit less efficient.
    public class InventoryClass
    {
        private int quantity, score = 0;
        
        public System.Action<int> onQuantityUpdated = null;
        
        public int Quantity { get { return quantity; } set { quantity = value;  if(onQuantityUpdated != null) onQuantityUpdated?.Invoke(value); } }
        public int Score { get { return score; } set { score = value; } }
    }
    
    public Dictionary<string, InventoryClass> inventory = new()
    {
        { "Gold", new InventoryClass() },
        { "Silver", new InventoryClass() },
        { "Copper", new InventoryClass() }
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
        { "Copper", 7 }
    };

    public static int Difficulty { get => difficulty; set => difficulty = value; }
    
    public static AgentData PlayerData { get => playerData; set => playerData = value; }
    
    public static AgentData MachineData { get => machineData; set => machineData = value; }

    private static void AddToInventory(AgentData agentData, string oreName, int oreScore)
    {
        if (agentData.inventory[oreName].Quantity >= invLimits[oreName])
            throw new System.Exception("Inventory is full");
        
        agentData.inventory[oreName].Quantity++;
        agentData.inventory[oreName].Score += oreScore;
        
        Debug.Log(agentData.inventory[oreName].Quantity + " " + agentData.inventory[oreName].Score);
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
