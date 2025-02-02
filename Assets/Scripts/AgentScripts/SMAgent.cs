using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// idle only exists when the agent is not doing anything, which with callbacks should never happen, but exists as a fail-safe.
/// </summary>
public enum AgentState
{
    Idle,
    TravellingToMine,
    Mining,
    TravellingToDeposit,
}

public class SMAgent : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;

    AgentMining agentMining;
    AgentFunctions agentFunctions;

    public bool startAgent = true;
    private AgentState agentState = AgentState.Idle;
    private GameObject oreToMine;
    private Vector3 depoPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        agentMining = GetComponent<AgentMining>();
        agentFunctions = GetComponent<AgentFunctions>();

        navMeshAgent.isStopped = true;
        navMeshAgent.speed += (float)GameData.Difficulty / 2;

        agentMining.onMine += SetAgentToIdle;

        depoPos = GameObject.Find("AgentDeposit").transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        // Add checker for time left and distance so agent deposits before time runs out.
        // Fix collision avoidance issues.

        if (!startAgent || agentState == AgentState.Mining)
            return;

        agentFunctions.UpdateAnimator();

        if (GameData.TimeLeft <= 20)
        {
            if (agentState == AgentState.Idle && GameData.MachineData.totalInventory > 0)
            {
                agentState = AgentState.TravellingToDeposit;
                navMeshAgent.destination = depoPos;
                return;
            }

            if (agentState == AgentState.TravellingToMine)
            {
                float timeNeededToMine =
                    (Vector3.Distance(transform.position, oreToMine.transform.position) / navMeshAgent.speed) +
                    agentMining.OreMiningTime[Enum.Parse<OreType>(oreToMine.tag)];
                float timeNeededToDeposit =
                    Vector3.Distance(oreToMine.transform.position, depoPos) / navMeshAgent.speed;
                if (timeNeededToMine + timeNeededToDeposit < GameData.TimeLeft)
                    return;
                agentState = AgentState.TravellingToDeposit;
                navMeshAgent.destination = depoPos;
                return;
            }
        }

        // Check if the agent is idle.
        if (agentState == AgentState.Idle)
        {
            Debug.Log("@M total inv" + GameData.MachineData.totalInventory);
            oreToMine = agentFunctions.FindBestOre();

            Debug.Log("Agent inv after mining: " +
                      (GameData.MachineData.totalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(oreToMine.tag)]));
            // Check if inventory would be full after mining, or is over half full, and if the deposit is closer than the ore, go deposit.
            if ((GameData.MachineData.totalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(oreToMine.tag)]) > 20 ||
                (GameData.MachineData.totalInventory > 10 && Vector3.Distance(transform.position, depoPos) <
                    Vector3.Distance(transform.position, oreToMine.transform.position)))
            {
                agentState = AgentState.TravellingToDeposit;
                navMeshAgent.destination = depoPos;
                navMeshAgent.isStopped = false;
                return;
            }

            agentState = AgentState.TravellingToMine;
            navMeshAgent.destination = oreToMine.transform.position;
            navMeshAgent.isStopped = false;
            return;
        }

        if (agentState == AgentState.TravellingToMine)
        {
            if (navMeshAgent.remainingDistance < 1f)
            {
                // Check if ore is being mined, or has been mined and destroyed:
                if (oreToMine == null || oreToMine.GetComponent<OreScript>().isBeingMined)
                {
                    oreToMine = agentFunctions.FindBestOre();
                    navMeshAgent.destination = oreToMine.transform.position;
                    return;
                }

                agentState = AgentState.Mining;
                agentMining.Mine(oreToMine);
                oreToMine = null;
            }

            return;
        }

        if (agentState == AgentState.TravellingToDeposit)
        {
            if (navMeshAgent.remainingDistance < 8f)
            {
                agentState = AgentState.Idle;
                navMeshAgent.isStopped = true;
            }
        }
    }

    // TODO: Should something like this be implemented, as it can increase complexity and decrease performance.
    // private void AvoidObstacles()
    // {
    //     RaycastHit hit;
    //     if (Physics.Raycast(transform.position, transform.forward, out hit, obstacleDetectionDistance, obstacleLayer))
    //     {
    //         Vector3 avoidDirection = Vector3.Reflect(transform.forward, hit.normal);
    //         Vector3 newDestination = transform.position + avoidDirection * obstacleDetectionDistance;
    //         navMeshAgent.SetDestination(newDestination);
    //     }
    // }

    private void SetAgentToIdle()
    {
        agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
    }

}