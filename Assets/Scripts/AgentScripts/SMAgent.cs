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

    delegate GameObject DecideOnOreType(List<GameObject> gameObjects);

    private DecideOnOreType decideOnOreFunction;
    
    int mistakesDone = 0;

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

        // navMeshAgent.speed = defaultSpeed + ((float)GameData.Difficulty / 2);
        navMeshAgent.speed = agentFunctions.defaultSpeed;
        navMeshAgent.acceleration = agentFunctions.defaultAcceleration;

        if (GameData.Difficulty == 0)
            decideOnOreFunction = DecideOnOreEasy;
        else if (GameData.Difficulty == 1)
            decideOnOreFunction = DecideOnOreMedium;
        else
            decideOnOreFunction = DecideOnOreHard;

        StartCoroutine(DelayedStart());
    }

    public void Reset()
    {
        agentMining.Stop();
        agentFunctions.StopAllCoroutines();
        navMeshAgent.isStopped = true;
        agentState = AgentState.Idle;
        oreToMine = null;
        navMeshAgent.ResetPath();
        startAgent = false;
        StartCoroutine(DelayedStart());
    }


    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
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
                navMeshAgent.SetDestination(agentFunctions.FindClosestDepositWaypoint(transform.position));
                return;
            }
        }

        // Check if the agent is idle.
        if (agentState == AgentState.Idle)
        {
            // Debug.Log("@M total inv" + agentData.TotalInventory);
            oreToMine = FindBestOre(agentFunctions.searchRadius);

            if (oreToMine == null)
                return;

            agentState = AgentState.TravellingToMine;
            navMeshAgent.SetDestination(oreToMine.transform.position);
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
                    oreToMine = FindBestOre(agentFunctions.searchRadius);
                    navMeshAgent.SetDestination(oreToMine.transform.position);
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

    bool CheckIfInventoryFull(GameObject ore)
    {
        // Debug.Log("Agent inv after mining: " +
        //           (agentData.TotalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(oreToMine.tag)]));
        // Check if inventory would be full after mining, or is over half full, and if the deposit is closer than the ore, go deposit.
        return (agentData.TotalInventory + GameData.InvStorageQty[Enum.Parse<OreType>(ore.tag)]) >
               GameData.MaximumInvQty ||
               (agentData.TotalInventory > ((float)GameData.MaximumInvQty / 2) &&
                agentFunctions.CalculatePathRemainingDistance(transform.position) <
                agentFunctions.CalculatePathRemainingDistance(ore.transform.position));
    }

    void GoToDeposit()
    {
        agentState = AgentState.TravellingToDeposit;
        navMeshAgent.SetDestination(agentFunctions.FindClosestDepositWaypoint(transform.position));
        navMeshAgent.isStopped = false;
    }

    GameObject DecideOnOreEasy(List<GameObject> ores)
    {
        GameObject closestOre = null;
        Vector3 closestOrePos = Vector3.zero;

        foreach (GameObject ore in ores)
        {
            // if (ore.GetComponent<OreScript>().isBeingMined)
            //     continue;

            if (closestOre == null)
            {
                closestOre = ore;
                closestOrePos = ore.transform.position;
                continue;
            }

            if (agentFunctions.CalculatePathRemainingDistance(closestOrePos, transform.position) <=
                agentFunctions.CalculatePathRemainingDistance(closestOrePos, ore.transform.position))
                continue;

            closestOre = ore;
            closestOrePos = closestOre.transform.position;
        }

        if (!CheckIfInventoryFull(closestOre))
            return closestOre;

        
        // 75% chance to over mine, with a maximum of 3 mistakes.
        int random = UnityEngine.Random.Range(0, 4);
        random -= mistakesDone;
        if (random <= 0)
        {
            mistakesDone = 0;
            return closestOre;
        }

        GoToDeposit();
        mistakesDone++;
        return null;
    }

    GameObject DecideOnOreMedium(List<GameObject> ores)
    {
        GameObject closestOre = null;
        Vector3 closestOrePos = Vector3.zero;

        foreach (GameObject ore in ores)
        {
            // if (ore.GetComponent<OreScript>().isBeingMined)
            //     continue;

            if (closestOre == null)
            {
                closestOre = ore;
                closestOrePos = ore.transform.position;
                continue;
            }

            if (agentFunctions.CalculatePathRemainingDistance(closestOrePos, transform.position) <=
                agentFunctions.CalculatePathRemainingDistance(closestOrePos, ore.transform.position))
                continue;

            closestOre = ore;
            closestOrePos = closestOre.transform.position;
        }
        
        if (!CheckIfInventoryFull(closestOre))
            return closestOre;
        
        // 25% chance to over mine, but will only do it once
        int random = UnityEngine.Random.Range(0, 4);
        random -= mistakesDone;
        if (random <= 2)
        {
            mistakesDone = 0;
            return closestOre;
        }

        GoToDeposit();
        mistakesDone++;
        return null;
    }

    GameObject DecideOnOreHard(List<GameObject> ores)
    {
        Vector3 playerPos = GameObject.FindWithTag("Player").transform.position;
        GameObject bestOre = null;
        int bestOreType = -1;
        foreach (GameObject ore in ores)
        {
            if (ore.GetComponent<OreScript>().isBeingMined)
                continue;

            if (bestOreType == -1)
            {
                bestOre = ore;
                bestOreType = (int)Enum.Parse<OreType>(ore.tag);
                continue;
            }

            int curOreType = (int)Enum.Parse<OreType>(ore.tag);

            if (curOreType < bestOreType)
                continue;

            if (agentFunctions.CalculatePathRemainingDistance(ore.transform.position, transform.position) >
                agentFunctions.CalculatePathRemainingDistance(ore.transform.position, playerPos))
                continue;

            if (agentFunctions.CalculatePathRemainingDistance(ore.transform.position, transform.position) <
                agentFunctions.CalculatePathRemainingDistance(bestOre.transform.position, transform.position))
            {
                bestOre = ore;
                bestOreType = curOreType;
                continue;
            }

            if (curOreType > bestOreType)
            {
                bestOre = ore;
                bestOreType = curOreType;
            }
        }
        
        if (!CheckIfInventoryFull(bestOre))
            return bestOre;
        
        GoToDeposit();
        return null;
    }

    GameObject FindBestOre(float oreSearchRadius)
    {
        List<GameObject> ores = agentFunctions.FindOres(oreSearchRadius);

        if (ores.Count == 1 && ores[0].GetComponent<OreScript>().isBeingMined)
            return FindBestOre(oreSearchRadius + 5f);

        return decideOnOreFunction(ores);
    }

    private void SetAgentToIdle()
    {
        agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
    }
}