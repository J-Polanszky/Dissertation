using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    // Easy - 3
    // Medium - 2
    // Hard - 1
    private static int difficulty = 3;
    private static int playerScore = 0;
    private static int machineScore = 0;

    private static Dictionary<string, List<float>> playerInventory = new()
    {
        { "Gold", new List<float>() {0,0} },
        { "Silver", new List<float>() {0,0} },
        { "Iron", new List<float>() {0,0} }
    };
    
    private static Dictionary<string, List<float>> machineInventory = new()
    {
        { "Gold", new List<float>() {0,0} },
        { "Silver", new List<float>() {0,0} },
        { "Iron", new List<float>() {0,0} }
    };

    public static int Difficulty { get => difficulty; set => difficulty = value; }
    
    public static int PlayerScore { get => playerScore; set => playerScore = value; }
    
    public static int MachineScore { get => machineScore; set => machineScore = value; }
    
    public static Dictionary<string, List<float>> PlayerInventory { get => playerInventory; set => playerInventory = value; }
    
    public static Dictionary<string, List<float>> MachineInventory { get => machineInventory; set => machineInventory = value; }

    public static void AddToPlayerInventory(string oreName, float oreScore)
    {
        PlayerInventory[oreName][0]++;
        PlayerInventory[oreName][1] += oreScore;
    }

    public static void AddToMachineInventory(string oreName, float oreScore)
    {
        MachineInventory[oreName][0]++;
        MachineInventory[oreName][1] += oreScore;  
    }
    
}
