using UnityEngine;

public class OreScript : MonoBehaviour
{
    protected int oreScore = 0;
    public bool playerMined = false;
    public System.Action destroyedCallback;
    
    void OnDestroy()
    {
        if (playerMined)
            GameData.AddToPlayerInventory(gameObject.name, oreScore * GameData.Difficulty);

        else
            GameData.AddToMachineInventory(gameObject.name, oreScore * GameData.Difficulty);
        
        print("Machine inventory: " + GameData.MachineInventory);
        print("Player inventory: " + GameData.PlayerInventory); 
        
        destroyedCallback?.Invoke();
    }
}
