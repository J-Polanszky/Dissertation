using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEditor;

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
    private Transform oreToMine;
    private OreType oreType;
    private int numOfTypes = Enum.GetNames(typeof(OreType)).Length;
    
    // Distances
    private float agentDistanceNormalised;
    private float playerDistanceNormalised;
    private float depositDistanceNormalised;
    
    // Inventory
    private float invAmountNormalised;

    //Calculate min and max distance from ores and deposit for normalisation
    private NormalisationDataClass normalisationData;
    
    int oreResourceIndex = 0;
    OreData currentOreData;
    List<OreData> oreResources = new();

    private bool startTraining = false;

    private int maxEpisodes = 500;
    
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

        StartCoroutine(DelayedStart());
    }

    void GatherDataCallback(List<OreData> oreData)
    {
        oreResources = oreData;
    }
    
    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        startTraining = true;
        StartCoroutine(agentFunctions.GatherDataForAgent(GatherDataCallback));
    }

    void DecideOnOre(bool recheck = false)
    {
        void CalculateNormalisedDistances()
        {
            agentDistanceNormalised = (currentOreData.distanceFromAgent - normalisationData.MinDistanceFromAgent) / (normalisationData.MaxDistanceFromAgent - normalisationData.MinDistanceFromAgent);
            playerDistanceNormalised = (currentOreData.distanceFromPlayer - normalisationData.MinDistanceFromPlayer) / (normalisationData.MaxDistanceFromPlayer - normalisationData.MinDistanceFromPlayer);
            depositDistanceNormalised = (currentOreData.distanceFromDeposit - normalisationData.MinDistanceFromBase) / (normalisationData.MaxDistanceFromBase - normalisationData.MinDistanceFromBase);
        }
        
        if (recheck)
        {
            // Current Ore Data will only ever be null due to coding mistake, so no if statement is needed, as it will just reduce performance for no reason.
            // if (currentOreData == null)
            // {
            //     Debug.LogError("Recheck is true, but no ore data was passed");
            //     return;
            // }
            CalculateNormalisedDistances();
            // Ensures that the current ore remains the best option
            RequestDecision();
            return;
        }
        
        // Should this loop to the start?
        if(oreResourceIndex >= oreResources.Count)
            oreResourceIndex = 0;
        
        currentOreData = oreResources[oreResourceIndex];
        
        oreToMine = currentOreData.oreToMine;
        oreType = currentOreData.oreType;
        
        CalculateNormalisedDistances();
        
        RequestDecision();
    }
    
    void ReCheckOreCallback(List<OreData> oreData)
    {
        oreResources = oreData;
        DecideOnOre(true);
    }
    
    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode " + (CompletedEpisodes + 1) + " has begun");
        // Reset the agent and environment for a new episode
        
        if(!startTraining)
            return;
        
        // Reset the agent
        // Reset the level
        
        #if UNITY_EDITOR
            if(maxEpisodes == 0)
                return;
            
            if (CompletedEpisodes < maxEpisodes)
                return;

            Debug.Log("Max episodes reached, stopping training");
            EditorApplication.EnterPlaymode();
        #endif
    }

    // TODO: Should i have the same code as the state machine agent here, or just give it access to the functions?
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect ore type observation
        sensor.AddOneHotObservation((int)oreType, numOfTypes);

        // Collect normalised inventory space taken observation
        sensor.AddObservation((float) GameData.InvStorageQty[oreType] / GameData.MaximumInvQty);

        // Collect normalised distances
        sensor.AddObservation(agentDistanceNormalised);
        sensor.AddObservation(playerDistanceNormalised);
        sensor.AddObservation(depositDistanceNormalised);

        // Collect inventory space observation
        sensor.AddObservation((float)GameData.MachineData.TotalInventory / GameData.MaximumInvQty);

        // Collect time left observation
        sensor.AddObservation((float)GameData.TimeLeft / GameData.InitialTime);
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
            StartCoroutine(agentFunctions.GoToDepositCoroutine());
            break;
        case 1:
            // Move to the nearest ore
            StartCoroutine(agentFunctions.GoToOreAndMineCoroutine(currentOreData, ReCheckOreCallback));
            break;
        case 2:
            // Look for the best ore
            oreResourceIndex++;
            DecideOnOre();
            break;
        }
    }

    private void SetAgentToIdle()
    {
        // agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
    }
}