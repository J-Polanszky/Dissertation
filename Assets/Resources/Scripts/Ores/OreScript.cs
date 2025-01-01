using UnityEngine;

public class OreScript : MonoBehaviour
{
    protected int oreScore = 0;
    public bool playerMined = false;
    
    void OnDestroy()
    {
        if (playerMined)
            GameData.PlayerScore += oreScore * GameData.Difficulty;

        else
            GameData.MachineScore += oreScore * GameData.Difficulty;
        
        print("Machine score: " + GameData.MachineScore);
        print("Player Score: " + GameData.PlayerScore); 
    }
}
