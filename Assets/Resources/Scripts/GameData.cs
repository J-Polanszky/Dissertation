using UnityEngine;

public static class GameData
{
    // Easy - 3
    // Medium - 2
    // Hard - 1
    private static int difficulty = 3;
    private static int playerScore = 0;
    private static int machineScore = 0;

    public static int Difficulty { get => difficulty; set => difficulty = value; }
    
    public static int PlayerScore { get => playerScore; set => playerScore = value; }
    
    public static int MachineScore { get => machineScore; set => machineScore = value; }
    
}
