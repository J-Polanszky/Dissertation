using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;

public class DepositBuilding : MonoBehaviour
{
    private string depositSfx = "event:/SFX_Events/Deposit";
    FMOD.Studio.EventInstance depositInstance;
    
    public float depositCooldown = 5f;
    
    public string agentTag;
    public AgentData agentData;
    bool agentCooldown = false;
    
    void Deposit(AgentData data)
    {
        foreach (var (key, value) in data.inventory)
        {
            if (value.Quantity == 0)
                continue;
            
            // Debug.Log("Depositing: " + key + "with value: " + value.Score);
            data.Score += value.Score;
            value.Quantity = 0;
            value.Score = 0;
        }
        
        data.TotalInventory = 0;
    }

    IEnumerator CountDown()
    {
        float countDown = depositCooldown;
        yield return new WaitForSeconds(countDown);
        agentCooldown = false;
    }

    private void Start()
    {
        depositInstance = FMODUnity.RuntimeManager.CreateInstance(depositSfx);
        depositInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        depositInstance.setVolume(1);
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
                depositInstance.start();
                agentCooldown = true;
                StartCoroutine(CountDown());
            }
        }
        
    }
}
