using System;
using UnityEngine;

public class OreScript : MonoBehaviour
{
    public OreType oreType;
    protected int oreScore = 0;
    public bool playerMined, isBeingMined = false;
    public System.Action destroyedCallback;

    void OnDestroy()
    {
        try
        {
            // To rework difficulty system idea, and adjust the agent state machine instead of the player's rewards.
            if (playerMined)
                GameData.AddToPlayerInventory(oreType,
                    oreScore);

            else
                GameData.AddToMachineInventory(oreType,
                    oreScore);

            destroyedCallback?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }
}