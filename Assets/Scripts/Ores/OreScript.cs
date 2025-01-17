using System;
using UnityEngine;

public class OreScript : MonoBehaviour
{
    protected int oreScore = 0;
    public bool playerMined = false;
    public System.Action destroyedCallback;
    
    void OnDestroy()
    {
        try
        {
            if (playerMined)
                GameData.AddToPlayerInventory(gameObject.name.Substring(0, gameObject.name.Length - 7),
                    oreScore * GameData.Difficulty);

            else
                GameData.AddToMachineInventory(gameObject.name.Substring(0, gameObject.name.Length - 7),
                    oreScore * GameData.Difficulty);

            destroyedCallback?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }
}
