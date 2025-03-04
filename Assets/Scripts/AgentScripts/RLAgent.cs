using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEditor;

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

    BufferSensorComponent m_BufferSensor;
    StatsRecorder statsRecorder;
    
    // [SerializeField] NNModel[] brains;
    
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

    private int maxEpisodes = 250;
    
    private int prevScore = 0;
    
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        m_BufferSensor = GetComponent<BufferSensorComponent>();
        
        statsRecorder = Academy.Instance.StatsRecorder;
        
        agentMining = GetComponent<AgentMining>();
        agentFunctions = GetComponent<AgentFunctions>();

        agentMining.onMine += SetAgentToIdle;
        agentMining.agentData = GameData.MachineData;
        
        agentFunctions.agentData = GameData.MachineData;
        agentFunctions.depositBuilding = GameObject.Find("AgentDeposit").transform;   
        normalisationData = agentFunctions.NormalisationData;
        
        startTraining = true;

        StartCoroutine(DelayedStart());
    }

    void FixedUpdate(){
        agentFunctions.UpdateAnimator();
    }
    
    public void Reset()
    {
        agentMining.Stop();
        agentFunctions.StopAllCoroutines();
        agentFunctions.NormalisationData.Reset();
        navMeshAgent.isStopped = true;
        oreToMine = null;
        oreResourceIndex = 0;
        oreResources.Clear();
        navMeshAgent.ResetPath();
        StartCoroutine(DelayedStart());
    }

    void GatherDataCallback(List<OreData> oreData)
    {
        oreResources = oreData;
        oreResourceIndex = 0;
        DecideOnOre();
    }
    
    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(agentFunctions.GatherDataForAgent(GatherDataCallback));
    }
    
    IEnumerator PunishIdle()
    {
        yield return new WaitForSeconds(1);
        AddReward(-0.05f);
    }
    
    void RewardAgent(int score)
    {
        int reward = score - prevScore;
        prevScore = score;
        
        AddReward((float) reward / 10);
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
    
    // void ReCheckOreCallback(List<OreData> oreData)
    // {
    //     oreResources = oreData;
    //     DecideOnOre(true);
    // }

    void PunishOreDestinationChange()
    {
        AddReward(-0.1f);
    }
    
    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode " + (CompletedEpisodes + 1) + " has begun");
        // Reset the agent and environment for a new episode
        
        if(!startTraining)
            return;
        
        // This will reset everything if it is not the first time running.
        TrainingManager.instance.StartGame();
        navMeshAgent.isStopped = false;
        prevScore = 0;
        GameData.MachineData.onScoreUpdated += RewardAgent;
        
        #if UNITY_EDITOR
            if(maxEpisodes == 0)
                return;
            
            if (CompletedEpisodes < maxEpisodes)
                return;

            Debug.Log("Max episodes reached, stopping training");
            EditorApplication.EnterPlaymode();
        #endif
    }

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
        Coroutine punishTravelling;
        switch(action){
            case 0:
            // Move to the nearest deposit
            if (GameData.MachineData.TotalInventory == 0)
            {
                Debug.Log("Punishing for moving to deposit with no inventory");
                AddReward(-1f);
                DecideOnOre();
                break;
            }

            // Using agentFunctions.StartCoroutine instead of StartCoroutine to avoid the error.
            punishTravelling = agentFunctions.StartCoroutine(PunishIdle());
            StartCoroutine(agentFunctions.GoToDepositCoroutine(GatherDataCallback, punishTravelling));
            break;
        case 1:
            // Move to the nearest ore
            punishTravelling = agentFunctions.StartCoroutine(PunishIdle());
            StartCoroutine(agentFunctions.GoToOreAndMineCoroutine(currentOreData, PunishOreDestinationChange, punishTravelling, GatherDataCallback));
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
        navMeshAgent.isStopped = true;
    }
}