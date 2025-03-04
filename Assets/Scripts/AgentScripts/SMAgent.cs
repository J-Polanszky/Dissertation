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

    protected AgentData agentData;

    public bool startAgent = false;
    private AgentState agentState = AgentState.Idle;
    private GameObject oreToMine;

    protected string depoName;
    // private Vector3 depoPos;

    protected void Awake()
    {
        agentData = GameData.MachineData;
        depoName = "AgentDeposit";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        agentMining = GetComponent<AgentMining>();
        agentFunctions = GetComponent<AgentFunctions>();

        navMeshAgent.isStopped = true;


        agentMining.onMine += SetAgentToIdle;
        agentMining.agentData = agentData;
        agentFunctions.agentData = agentData;
        agentFunctions.depositBuilding = GameObject.Find(depoName).transform;

        StartCoroutine(DelayedStart());
    }

    public void Reset()
    {
        agentMining.Stop();
        agentFunctions.StopAllCoroutines();
        agentFunctions.NormalisationData.Reset();
        navMeshAgent.isStopped = true;
        agentState = AgentState.Idle;
        oreToMine = null;
        navMeshAgent.ResetPath();
        startAgent = false;
        StartCoroutine(DelayedStart());
    }


    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        startAgent = true;
    }

    // Updated it to use Fixed Update, as it is more efficient for physics calculations.
    void FixedUpdate()
    {
        // Add checker for time left and distance so agent deposits before time runs out.
        // Fix collision avoidance issues.

        if (!startAgent || agentState == AgentState.Mining)
            return;

        agentFunctions.UpdateAnimator();

        if (GameData.TimeLeft <= 20)
        {
            if (agentState == AgentState.TravellingToDeposit)
            {
                //This normally means there is not enough time to mine and depo in time, so to save resources, the agent will be disabled.
                startAgent = false;
                return;
            }
            if (agentState == AgentState.TravellingToMine)
            {
                float timeNeededToMine =
                    (agentFunctions.CalculatePathRemainingDistance(oreToMine.transform.position) / navMeshAgent.speed) +
                    agentMining.OreMiningTime[Enum.Parse<OreType>(oreToMine.tag)];
                float timeNeededToDeposit =
                    agentFunctions.CalculatePathRemainingDistance(
                        agentFunctions.FindClosestDepositWaypoint(oreToMine.transform.position),
                        oreToMine.transform.position) / navMeshAgent.speed;
                if (timeNeededToMine + timeNeededToDeposit < GameData.TimeLeft)
                    return;
                agentState = AgentState.TravellingToDeposit;
                navMeshAgent.destination = agentFunctions.FindClosestDepositWaypoint(transform.position);
                return;
            }
        }

        // Check if the agent is idle.
        if (agentState == AgentState.Idle)
        {
            // Debug.Log("@M total inv" + agentData.TotalInventory);
            oreToMine = agentFunctions.FindBestOre(agentFunctions.stateMachineSearchRadius);

            // Debug.Log("Agent inv after mining: " +
            //           (agentData.TotalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(oreToMine.tag)]));
            // Check if inventory would be full after mining, or is over half full, and if the deposit is closer than the ore, go deposit.
            if ((agentData.TotalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(oreToMine.tag)]) >
                GameData.MaximumInvQty ||
                (agentData.TotalInventory > ((float)GameData.MaximumInvQty / 2) &&
                 agentFunctions.CalculatePathRemainingDistance(transform.position) <
                 agentFunctions.CalculatePathRemainingDistance(oreToMine.transform.position)))
            {
                agentState = AgentState.TravellingToDeposit;
                navMeshAgent.destination = agentFunctions.FindClosestDepositWaypoint(transform.position);
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
                    oreToMine = agentFunctions.FindBestOre(agentFunctions.stateMachineSearchRadius);
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
            if (navMeshAgent.remainingDistance < 1f)
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