using System;
using UnityEngine;

public class OreScript : MonoBehaviour
{
    public OreType oreType;
    protected int oreScore = 0;
    public bool isBeingMined = false;

    public AgentData agentData;
    
    public System.Action destroyedCallback;

    void OnDestroy()
    {
        try
        {
            GameData.AddToInventory(agentData, oreType,
                    oreScore);

            destroyedCallback?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }
}