using System;
using UnityEngine;
using Object = System.Object;

public class DepositBuilding : MonoBehaviour
{
    // TODO: Have the gamemanager assign these, and to then test the physics overlap and functionality.
    public string agentTag;
    public AgentData agentData;
    
    private void Update()
    {
        // Create a physics sphere where if a player walks into it, it checks if it is the player assigned to it
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1f);
        foreach (Collider col in hitColliders)
        {
            // Check if the collided object is a player or State machine
            if (col.gameObject.CompareTag(agentTag))
            {
                foreach (var (key, value) in agentData.inventory)
                {
                    agentData.score += value[1];
                    value[0] = 0;
                    value[1] = 0;
                }
            }
        }
        
    }
}
