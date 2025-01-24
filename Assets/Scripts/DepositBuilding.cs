using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;

public class DepositBuilding : MonoBehaviour
{
    public float depositCooldown = 5f;
    
    // Changed idea and made them universal for both player and agent.
    public string agentTag;
    public AgentData agentData;
    bool agentCooldown = false;

    public string playerTag;
    public AgentData playerData;
    bool playerCooldown = false;
    
    void Deposit(AgentData data)
    {
        foreach (var (key, value) in data.inventory)
        {
            if (value[0] == 0)
                continue;
            
            Debug.Log("Depositing: " + key + "with value: " + value[1]);
            data.Score += value[1];
            value[0] = 0;
            value[1] = 0;
        }
    }

    IEnumerator CountDown(bool isPlayer)
    {
        float countDown = depositCooldown;
        yield return new WaitForSeconds(countDown);
        if (isPlayer)
            playerCooldown = false;
    }
    
    private void Update()
    {
        // Create a physics sphere where if a player walks into it, it checks if it is the player assigned to it
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 7.5f);
        foreach (Collider col in hitColliders)
        {
            // Check if the collided object is a player or State machine
            if (col.gameObject.CompareTag(agentTag) && agentCooldown == false)
            {
                Deposit(agentData);
                agentCooldown = true;
                StartCoroutine(CountDown(false));
            }

            if (col.gameObject.CompareTag(playerTag) && playerCooldown == false)
            {
                Deposit(playerData);
                playerCooldown = true;
                StartCoroutine(CountDown(true));
            }
        }
        
    }
}
