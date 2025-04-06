using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEditor;
using UnityEngine.SceneManagement;

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
public class normalisationDataClass
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

    public normalisationDataClass()
    {
        Reset();
    }
}

public class RLAgent : Agent
{
    NavMeshAgent navMeshAgent;
    AgentMining agentMining;
    AgentFunctions agentFunctions;

    GameObject player;

    BehaviorParameters behaviorParameters;
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
    private normalisationDataClass normalisationData;

    int oreResourceIndex = 0;
    OreData currentOreData;
    List<OreData> oreResources = new();

    private bool startTraining = false;

    private int maxEpisodes = 250;

    private int prevScore = 0;

    bool isInference = false;

    private Coroutine action;
    bool agentFrozen = false;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        m_BufferSensor = GetComponent<BufferSensorComponent>();

        statsRecorder = Academy.Instance.StatsRecorder;

        agentMining = GetComponent<AgentMining>();
        agentFunctions = GetComponent<AgentFunctions>();

        player = GameObject.FindWithTag("Player");

        agentMining.onMine += SetAgentToIdle;
        agentMining.agentData = GameData.MachineData;

        agentFunctions.agentData = GameData.MachineData;
        agentFunctions.depositBuilding = GameObject.Find("AgentDeposit").transform;
        normalisationData = new();

        startTraining = true;
        navMeshAgent.speed = agentFunctions.defaultSpeed;
        navMeshAgent.acceleration = agentFunctions.defaultAcceleration;

        behaviorParameters = GetComponent<BehaviorParameters>();
        isInference = behaviorParameters.Model != null;
        // behaviorParameters.InferenceDevice = InferenceDevice.ComputeShader;

        if (isInference)
            behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;

        StartCoroutine(DelayedStart());
    }

    void FixedUpdate()
    {
        agentFunctions.UpdateAnimator();
    }

    public void Reset()
    {
        agentMining.Stop();
        agentFunctions.StopAllCoroutines();
        normalisationData.Reset();
        navMeshAgent.isStopped = true;
        oreToMine = null;
        oreResourceIndex = 0;
        oreResources.Clear();
        navMeshAgent.ResetPath();
        StopAllCoroutines();
        agentFrozen = false;
        
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
        StartCoroutine(CheckIfFrozen());
        StartCoroutine(GatherDataForAgent(GatherDataCallback));
    }

    IEnumerator CheckIfFrozen()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);  
            if (action == null)
            {
                if(agentFrozen)
                    StartCoroutine(GatherDataForAgent(GatherDataCallback));
                else
                    agentFrozen = true;
            }
        }
    }
    
    IEnumerator PunishIdle()
    {
        // Since rewards have beem increased, the punishment for travelling has also been increased. This will punish going for further away ores unless they are of higher value.
        yield return new WaitForSeconds(1);
        AddReward(-0.1f);
    }

    void RewardAgent(int score)
    {
        int reward = score - prevScore;
        prevScore = score;

        // Increased the reward amount?
        AddReward((float)reward / 10);
    }

    void DecideOnOre(bool recheck = false)
    {
        void CalculateNormalisedDistances()
        {
            agentDistanceNormalised = (currentOreData.distanceFromAgent - normalisationData.MinDistanceFromAgent) /
                                      (normalisationData.MaxDistanceFromAgent - normalisationData.MinDistanceFromAgent);
            playerDistanceNormalised = (currentOreData.distanceFromPlayer - normalisationData.MinDistanceFromPlayer) /
                                       (normalisationData.MaxDistanceFromPlayer -
                                        normalisationData.MinDistanceFromPlayer);
            depositDistanceNormalised = (currentOreData.distanceFromDeposit - normalisationData.MinDistanceFromBase) /
                                        (normalisationData.MaxDistanceFromBase - normalisationData.MinDistanceFromBase);
        }

        // if (recheck)
        // {
        //     // Current Ore Data will only ever be null due to coding mistake, so no if statement is needed, as it will just reduce performance for no reason.
        //     // if (currentOreData == null)
        //     // {
        //     //     Debug.LogError("Recheck is true, but no ore data was passed");
        //     //     return;
        //     // }
        //     CalculateNormalisedDistances();
        //     // Ensures that the current ore remains the best option
        //     RequestDecision();
        //     return;
        // }

        // On rare occasions, the RL Agent wont find an ore. it is to wait and then decide again.
        if (oreResources.Count == 0)
        {
            Debug.LogError("No ore resources found");
            StartCoroutine(DelayedStart());
            return;
        }

        // Should this loop to the start?
        if (oreResourceIndex >= oreResources.Count)
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

    public IEnumerator GatherDataForAgent(Action<List<OreData>> callback, List<GameObject> oresblacklist = null)
    {
        List<GameObject> ores;
        byte blacklisted;
        List<OreData> oreDataList;

        do
        {
            blacklisted = 0;
            // Debug.LogWarning("STARTING GATHER DATA");
            try
            {
                ores = agentFunctions.FindOres(agentFunctions.searchRadius, 0, oresblacklist);
            }
            catch (Exception e)
            {
                // Likely no ores found, and need to wait till they spawn
                Debug.LogError(e);
                yield break;
            }

            // Debug.LogWarning("ORES FOUND");

            oreDataList = new();

            if (oresblacklist == null)
                oresblacklist = new List<GameObject>();

            normalisationData.Reset();

            foreach (GameObject ore in ores)
            {
                //TODO: Make a invalid path checker (is this needed, as the agent seems quite proficient at navigating)
                float distanceFromAgent = agentFunctions.CalculatePathRemainingDistance(ore.transform.position);
                float distanceFromPlayer =
                    agentFunctions.CalculatePathRemainingDistance(ore.transform.position, player.transform.position);
                float distanceFromBase = agentFunctions.CalculatePathRemainingDistance(
                    agentFunctions.FindClosestDepositWaypoint(ore.transform.position),
                    ore.transform.position);

                if (distanceFromAgent < 0 || distanceFromPlayer < 0 || distanceFromBase < 0)
                {
                    oresblacklist.Add(ore);
                    blacklisted++;
                    continue;
                }

                if (distanceFromAgent > normalisationData.MaxDistanceFromAgent)
                    normalisationData.MaxDistanceFromAgent = distanceFromAgent;
                else if (normalisationData.MinDistanceFromAgent == 0 ||
                         distanceFromAgent < normalisationData.MinDistanceFromAgent)
                    normalisationData.MinDistanceFromAgent = distanceFromAgent;

                if (distanceFromPlayer > normalisationData.MaxDistanceFromPlayer)
                    normalisationData.MaxDistanceFromPlayer = distanceFromPlayer;
                else if (normalisationData.MinDistanceFromPlayer == 0 ||
                         distanceFromPlayer < normalisationData.MinDistanceFromPlayer)
                    normalisationData.MinDistanceFromPlayer = distanceFromPlayer;

                if (distanceFromBase > normalisationData.MaxDistanceFromBase)
                    normalisationData.MaxDistanceFromBase = distanceFromBase;
                else if (normalisationData.MinDistanceFromBase == 0 ||
                         distanceFromBase < normalisationData.MinDistanceFromBase)
                    normalisationData.MinDistanceFromBase = distanceFromBase;

                oreDataList.Add(new OreData()
                {
                    oreToMine = ore.transform,
                    orePos = ore.transform.position,
                    oreType = Enum.Parse<OreType>(ore.tag),
                    oreScript = ore.GetComponent<OreScript>(),
                    distanceFromAgent = distanceFromAgent,
                    distanceFromPlayer = distanceFromPlayer,
                    distanceFromDeposit = distanceFromBase
                });

                // Debug.LogWarning("ORE ADDED");
            }
        } while (blacklisted == ores.Count);

        callback(oreDataList);
    }

    // IEnumerator ReCheckOres(Action<List<OreData>> callback)
    // {
    //     while (true)
    //     {
    //         yield return new WaitForSeconds(1f);
    //         Debug.Log("Rechecking Ores");
    //         StartCoroutine(GatherDataForAgent(callback));
    //         // No need to update current ore data since it would have been caught in the GatherDataForAgent function.
    //     }
    // }

    // GoToOre&MineCoroutine
    public IEnumerator GoToOreAndMineCoroutine(Coroutine travellingPunish)
    {
        //Due to previous version of code, and lack of time for proper refactoring and debugging, a copy of the current OreData will be taken instead
        OreData oreData = currentOreData;
        GameObject ore = oreData.oreToMine.gameObject;
        
        navMeshAgent.SetDestination(oreData.orePos);
        // Not using recheck since gains are small and computational cost is high.
        // Coroutine recheckCoroutine = StartCoroutine(ReCheckOres(recheckCallback));
        // Debug.Log("Started GoToOreAndMineCoroutine");

        // On Rare occasions, the pathfinding gets stuck

        float stuckCheckInterval = 2f;
        float stuckCheckTimer = 0f;
        Vector3 lastPosition = transform.position;
        float stuckDistanceThreshold = 1f;
        int totalStrikes = 0;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (ore == null)
            {
                Debug.LogWarning("Ore is null, punishing agent");
                PunishOreDestinationChange();
                break;
            }

            //This ensures that the destination has been set properly
            if (Vector3.Distance(oreData.orePos, navMeshAgent.destination) > 1f)
                continue;

            // Check if the agent is stuck
            stuckCheckTimer += Time.fixedDeltaTime;
            if (stuckCheckTimer >= stuckCheckInterval)
            {
                stuckCheckTimer = 0f;
                float prevDistanceFromOre = Vector3.Distance(lastPosition, oreData.orePos);
                float currentDistanceFromOre = Vector3.Distance(transform.position, oreData.orePos);
                float distanceMoved = prevDistanceFromOre - currentDistanceFromOre;
                lastPosition = transform.position;

                if (distanceMoved < stuckDistanceThreshold)
                {
                    totalStrikes++;
                    if (totalStrikes < 3 &&
                        agentFunctions.CalculatePathRemainingDistance(oreData.orePos, transform.position) != -1)
                        continue;

                    Debug.LogWarning("Agent is stuck, changing ore\nValid Path Value: " +
                                     agentFunctions.CalculatePathRemainingDistance(oreData.orePos,
                                         navMeshAgent.transform.position));
                    StopCoroutine(travellingPunish);
                    // punishCallback();
                    // Rare error when this is called when the ore does not exist.
                    // While it will hurt performance, it is necessary to ensure that the agent is not stuck.
                    try
                    {
                        StartCoroutine(GatherDataForAgent(GatherDataCallback,
                            new List<GameObject>() { ore }));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        PunishOreDestinationChange();
                        break;
                    }

                    yield break;
                }
            }

            if (navMeshAgent.remainingDistance < 1f && Vector3.Distance(oreData.orePos, transform.position) < 1f)
            {
                // Debug.Log("Reached destination");
                // StopCoroutine(recheckCoroutine);

                if (oreData.oreScript.isBeingMined)
                {
                    Debug.Log("Ore is being mined by another agent, punishing agent");
                    PunishOreDestinationChange();
                    break;
                }

                StopCoroutine(travellingPunish);
                // Debug.Log("Starting mining process");
                Coroutine mining;
                try
                {
                    mining = agentMining.Mine(oreData.oreToMine.gameObject);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    break;
                }

                yield return mining;
                // Added rewards on mining for higher tier ores to help agent learn that sometimes suffering the travel punishment is worth getting the higher tier ore.
                // To check if these rewards should be adjusted.
                if (oreData.oreType == OreType.Silver)
                    RewardOnMine(0.4f);
                if (oreData.oreType == OreType.Gold)
                    RewardOnMine(0.8f);
                break;
            }
        }

        // There is a chance that this will be called before the ore has been destroyed. Wait for the ore to be destroyed before calling the callback.
        // Debug.Log("Ending GoToOreAndMineCoroutine");

        while (oreData.oreToMine != null)
            yield return new WaitForFixedUpdate();

        StartCoroutine(GatherDataForAgent(GatherDataCallback));
    }

    // GoToDepositCoroutine
    public IEnumerator GoToDepositCoroutine(Coroutine travellingPunish)
    {
        Vector3 depositWaypoint = agentFunctions.FindClosestDepositWaypoint(transform.position);
        navMeshAgent.SetDestination(depositWaypoint);

        while (true)
        {
            if (navMeshAgent.remainingDistance < 1f)
                break;
            // FixedUpdate is used to ensure that the agent moves smoothly, and improves performance.
            yield return new WaitForFixedUpdate();
        }

        StopCoroutine(travellingPunish);
        StartCoroutine(GatherDataForAgent(GatherDataCallback));
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode " + (CompletedEpisodes + 1) + " has begun");
        // Reset the agent and environment for a new episode

        if (!startTraining)
            return;

        if(TrainingManager.instance != null)
            TrainingManager.instance.StartGame();
        else if (GameManager.instance != null)
        {
            GameManager.instance.RunGameStartFunctions();
            switch (GameData.Difficulty)
            {
                case 0:
                    ChangeModel(Resources.Load<ModelAsset>("Agent/V3/Easy/Easy"));
                    break;
                case 1:
                    ChangeModel(Resources.Load<ModelAsset>("Agent/V3/Medium/Medium"));
                    break;
                case 2:
                    ChangeModel(Resources.Load<ModelAsset>("Agent/V3/Hard/Hard"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
            EvaluationManager.instance.StartGame();

        navMeshAgent.isStopped = false;
        prevScore = 0;
        GameData.MachineData.onScoreUpdated += RewardAgent;

#if UNITY_EDITOR
        if (maxEpisodes == 0)
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
        sensor.AddObservation((float)GameData.InvStorageQty[oreType] / GameData.MaximumInvQty);

        // Collect normalised distances
        sensor.AddObservation(agentDistanceNormalised);
        sensor.AddObservation(playerDistanceNormalised);
        sensor.AddObservation(depositDistanceNormalised);

        // Collect inventory space observation
        sensor.AddObservation((float)GameData.MachineData.TotalInventory / GameData.MaximumInvQty);

        // Collect time left observation
        sensor.AddObservation((float)GameData.TimeLeft / GameData.InitialTime);
    }

    void RewardOnMine(float score)
    {
        if (score > 0)
            AddReward(score);
    }

    IEnumerator ExecuteAction(int action)
    {
        if (this.action != null)
            yield return this.action;

        agentFrozen = false;
        
        // Implement the logic to control the agent based on the actions
        Coroutine punishTravelling;
        switch (action)
        {
            case 0:
                // Move to the nearest deposit
                if (GameData.MachineData.TotalInventory == 0)
                {
                    Debug.Log("Punishing for moving to deposit with no inventory");
                    AddReward(-1f);
                    DecideOnOre();
                    break;
                }

                punishTravelling = StartCoroutine(PunishIdle());
                this.action = StartCoroutine(GoToDepositCoroutine(punishTravelling));
                break;
            case 1:
                // Move to the decided ore
                // Punishing the agent on wasting time travelling to and mining when he wont be able to store the mined ore.
                if (GameData.MachineData.TotalInventory + GameData.InvStorageQty[currentOreData.oreType] >
                    GameData.MaximumInvQty)
                    AddReward(-1f);

                punishTravelling = StartCoroutine(PunishIdle());
                this.action = StartCoroutine(GoToOreAndMineCoroutine(punishTravelling));
                break;
            case 2:
                // Look for the best ore
                oreResourceIndex++;
                DecideOnOre();
                break;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Use the actions received from the neural network
        // var continuousActions = actions.ContinuousActions;
        int action = actions.DiscreteActions[0];

        StartCoroutine(ExecuteAction(action));
    }

    public void CustomEndEpisode()
    {
        // TODO: Reward agent for empty inventory, and increase punishment based on wasted ores in inventory.
        float reward = 2;
        float punishment = 20 * ((float)GameData.MachineData.TotalInventory / GameData.MaximumInvQty);

        AddReward(reward - punishment);

        if (!isInference)
            statsRecorder.Add("Score", GameData.MachineData.Score);
        EndEpisode();
    }

    private void SetAgentToIdle()
    {
        navMeshAgent.isStopped = true;
    }
    
    public void ChangeModel(ModelAsset newModel)
    {
        SetModel(behaviorParameters.name, newModel, behaviorParameters.InferenceDevice);
    }
}