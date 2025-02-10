using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

//TODO: When the RL Agent is doing nothing, find all ores within a set distance, and fill in the resource data for each one.
// It is important that when the RL agent makes a decision, he knows everything about the ore, the time left, his inventory space and speed, as well as the distance the ore is from the player and his base.
// When the agent is travelling to mine an ore, it should redo this check after a certain amount of time, such as a second to make sure it is still the best option in the long run.
// The agents actions will end up being similar to the State-Machines, with the agent having more control on which ores to mine, and when to deposit.

public class OreData
{
    public Transform oreToMine;
    public Vector3 orePos;
    public OreType oreType;
    public OreScript oreScript;
    
    public float distanceFromPlayer;
    public float distanceFromAgent;
    public float distanceFromDeposit;
}

// This exists solely to work as a pointer and allow for cleaner code.
public class NormalisationDataClass
{
    public float MinDistanceFromAgent;
    public float MaxDistanceFromAgent;
    public float MinDistanceFromPlayer;
    public float MaxDistanceFromPlayer;
    public float MinDistanceFromBase;
    public float MaxDistanceFromBase;
    
    public void Reset()
    {
        MinDistanceFromAgent = 0;
        MaxDistanceFromAgent = 0;
        MinDistanceFromPlayer = 0;
        MaxDistanceFromPlayer = 0;
        MinDistanceFromBase = 0;
        MaxDistanceFromBase = 0;
    }
    
    public NormalisationDataClass()
    {
        Reset();
    }
}

public class RLAgent : Agent
{
    
    NavMeshAgent navMeshAgent;
    AgentMining agentMining;
    AgentFunctions agentFunctions;

    // BufferSensorComponent m_BufferSensor;
    // StatsRecorder statsRecorder;
    
    // Agent Observation Data
    
    // Ore
    private OreType oreType;
    private int numOfTypes = Enum.GetNames(typeof(OreType)).Length;
    
    // Distances
    private float playerDistanceNormalised;
    private float depositDistanceNormalised;
    
    // Inventory
    private float invAmountNormalised;

    //Calculate min and max distance from ores and deposit for normalisation
    private NormalisationDataClass normalisationData;
    
    int oreResourceIndex = 0;
    
    

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        agentMining = GetComponent<AgentMining>();
        agentFunctions = GetComponent<AgentFunctions>();

        normalisationData = agentFunctions.NormalisationData;
        
        // m_BufferSensor = GetComponent<BufferSensorComponent>();
        // statsRecorder = Academy.Instance.StatsRecorder;
        
        // navMeshAgent.isStopped = true;
        
        agentMining.onMine += SetAgentToIdle;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent and environment for a new episode
        // agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
        // Add any additional reset logic here
    }

    // TODO: Should i have the same code as the state machine agent here, or just give it access to the functions?
    
    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Use the actions received from the neural network
        // var continuousActions = actions.ContinuousActions;
        int action =  actions.DiscreteActions[0];

        // Implement the logic to control the agent based on the actions
        
        switch(action){
            case 0:
            // Move to the nearest deposit
            // MoveToDeposit();
            break;
        case 1:
            // Move to the nearest ore
            // MoveToOre();
            break;
        }
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     // Provide heuristic actions for testing and debugging
    //     var continuousActionsOut = actionsOut.ContinuousActions;
    //     var discreteActionsOut = actionsOut.DiscreteActions;
    //
    //     // Implement heuristic actions
    // }

    private void SetAgentToIdle()
    {
        // agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
    }
}