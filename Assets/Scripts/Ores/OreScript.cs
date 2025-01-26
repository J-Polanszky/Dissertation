using System;
using UnityEngine;

public class OreScript : MonoBehaviour
{
    protected int oreScore = 0;
    public bool playerMined,isBeingMined = false;
    public System.Action destroyedCallback;
    
    void OnDestroy()
    {
        try
        {
            // To rework difficulty system idea, and adjust the agent state machine instead of the player's rewards.
            if (playerMined)
                GameData.AddToPlayerInventory(gameObject.name.Substring(0, gameObject.name.Length - 7),
                    oreScore);

            else
                GameData.AddToMachineInventory(gameObject.name.Substring(0, gameObject.name.Length - 7),
                    oreScore);

            destroyedCallback?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }
}
