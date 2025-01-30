using System.Collections.Generic;
using UnityEngine;

public class AgentData
{
    // Updated it to use a class to make it more readable than a simple list, even if it makes it a bit less efficient.
    // Old quantity code parts will be kept since if the old inventory display is used, it will be needed.
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

    public int totalInventory = 0;
    public System.Action<int> onScoreUpdated;
    private int score = 0;
    
    public int Score { get { return score; } set { score = value; onScoreUpdated?.Invoke(value); } }
}

public static class GameData
{
    // Easy - 0
    // Medium - 1
    // Hard - 2
    private static int difficulty = 0;

    private static int timeLeft = 600;

    private static AgentData playerData = new();
    
    private static AgentData machineData = new();

    // TODO: Make inventory limits a total weight and assign weight to each one instead. Figure out how to display it to the user.
    // Save current UI for inventory, and instead use a combined image with the ores, with the number of inv used / total inv.
    // Ask others if this is fine, or if they prefer having the old one and then have the new one next to it to show total.
    private static Dictionary<string, int> invStorageQty = new()
    {
        { "Gold", 5 },
        { "Silver", 3 },
        { "Copper", 1 }
    };

    public static Dictionary<string, int> InvStorageQty
    {
        get => invStorageQty;
    }

    public static int Difficulty { get => difficulty; set => difficulty = value; }
    
    public static int TimeLeft { get => timeLeft; set => timeLeft = value; }
    
    public static AgentData PlayerData { get => playerData; set => playerData = value; }
    
    public static AgentData MachineData { get => machineData; set => machineData = value; }

    private static void AddToInventory(AgentData agentData, string oreName, int oreScore)
    {
        if (agentData.totalInventory + invStorageQty[oreName] >= 20)
            throw new System.Exception("Inventory will be too full");
        
        agentData.inventory[oreName].Quantity++;
        agentData.inventory[oreName].Score += oreScore;
        agentData.totalInventory += invStorageQty[oreName];
    }
    
    public static void AddToPlayerInventory(string oreName, int oreScore)
    {
        AddToInventory(playerData, oreName, oreScore);
    }

    public static void AddToMachineInventory(string oreName, int oreScore)
    {
        AddToInventory(machineData, oreName, oreScore);
    }
    
}
