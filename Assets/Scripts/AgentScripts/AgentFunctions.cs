using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class AgentFunctions : MonoBehaviour
{
    // Common variables for both state machine and RL agent
    Animator animator;
    NavMeshAgent navMeshAgent;

    public AgentData agentData;

    public int oreLayer;

    public Transform depositBuilding;

    readonly float defaultAcceleration = 5f;

    // State machine variables
    public bool isStateMachine = false;

    readonly float defaultStateSpeed = 1f;
    public float stateMachineSearchRadius = 10f;

    // RL Agent variables
    AgentMining agentMining;
    GameObject player;

    readonly float defaultRlSpeed = 2f;
    public readonly float RlOreSearchRadius = 10f;

    public NormalisationDataClass NormalisationData = new();

    private void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        agentMining = GetComponent<AgentMining>();
        oreLayer = LayerMask.GetMask("Ore");
        player = GameObject.FindWithTag("Player");
        // depositBuilding = GameObject.Find("AgentDeposit").transform;

        stateMachineSearchRadius += GameData.Difficulty * 3;

        StartCoroutine(DelayedStart());

        if (isStateMachine)
        {
            navMeshAgent.speed = defaultStateSpeed + GameData.Difficulty;
            navMeshAgent.acceleration = defaultAcceleration;
            return;
        }

        navMeshAgent.speed = defaultRlSpeed;
        navMeshAgent.acceleration = defaultAcceleration;
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        agentData.onInventoryUpdated += ChangeSpeed;
    }

    #region CommonFunctions

    // Common functions for both state machine and RL agent

    void ChangeSpeed()
    {
        // State machine speed varies, RlAgent speed is always the same.

        if (agentData.TotalInventory == 0)
        {
            if (isStateMachine)
            {
                navMeshAgent.speed = defaultStateSpeed + (float)GameData.Difficulty / 2;
                navMeshAgent.acceleration = defaultAcceleration;
                return;
            }

            navMeshAgent.speed = defaultRlSpeed;
            navMeshAgent.acceleration = defaultAcceleration;
        }

        // Divide by 60 so that the minimum multiplier is 0.5
        float multiplier = 1 - (float)agentData.TotalInventory / (GameData.MaximumInvQty * 2);

        if (isStateMachine)
        {
            navMeshAgent.speed = (defaultStateSpeed + (float)GameData.Difficulty / 2) * multiplier;
            navMeshAgent.acceleration = defaultAcceleration * multiplier;
            return;
        }

        navMeshAgent.speed = defaultRlSpeed * multiplier;
        navMeshAgent.acceleration = defaultAcceleration * multiplier;
    }

    public float CalculatePathRemainingDistance(Vector3 targetPosition, Vector3 originPosition = new())
    {
        if (originPosition == Vector3.zero)
            originPosition = navMeshAgent.transform.position;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(originPosition, targetPosition, NavMesh.AllAreas, path))
        {
            float distance = 0f;
            for (int i = 0; i < path.corners.Length - 1; ++i)
            {
                distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }

            return distance;
        }

        return -1f; // Return -1 if the path is invalidE
    }

    public Vector3 FindClosestDepositWaypoint(Vector3 originPosition)
    {
        Vector3 closestWaypoint = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        foreach (Transform child in depositBuilding)
        {
            //NOTE: if more children are added, a guard statement will be needed.
            float distance = CalculatePathRemainingDistance(child.position, originPosition);

            if (closestDistance < distance) continue;

            closestDistance = distance;
            closestWaypoint = child.position;
        }

        if (closestWaypoint == Vector3.zero)
            throw new Exception("No Deposit Waypoints Found");

        return closestWaypoint;
    }

    public List<GameObject> FindOres(float oreSearchRadius, int recursiveDepth = 0,
        List<GameObject> oresblacklist = null)
    {
        // It is impossible for no ores to be found within this limit, so while it is higher than i would like, it is the safest option.
        // FIXME: Sometimes this doesnt find ores, even if they are right next to the agent, and it only happens after depositing.
        if (recursiveDepth > 30)
            throw new Exception("No Ores Found After 30 Recursive Calls");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, oreSearchRadius, oreLayer);

        if (hitColliders.Length > 0)
        {
            List<GameObject> ores = hitColliders.Select(x => x.gameObject).ToList();
            foreach (GameObject ore in oresblacklist ?? new List<GameObject>())
            {
                ores.Remove(ore);
            }

            if (ores.Count > 0)
                return ores;
        }

        return FindOres(oreSearchRadius + 5f, recursiveDepth + 1, oresblacklist);
    }

    public void UpdateAnimator()
    {
        if (animator == null || !animator.enabled) return;

        Vector3 velocity = navMeshAgent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        float speed = localVelocity.z;

        animator.SetFloat(vAgentAnimatorParameters.InputVertical, speed);
        animator.SetFloat(vAgentAnimatorParameters.InputMagnitude, speed);
        animator.SetBool(vAgentAnimatorParameters.IsGrounded, true); // Assuming the agent is always grounded
    }

    #endregion

    #region StateMachineFunctions

    // State machine functions

    public GameObject FindBestOre(float oreSearchRadius)
    {
        Vector3 playerPos = GameObject.FindWithTag("Player").transform.position;
        GameObject bestOre = null;
        int bestOreType = -1;

        List<GameObject> ores = FindOres(oreSearchRadius);

        if (ores.Count == 1 && ores[0].GetComponent<OreScript>().isBeingMined)
            return FindBestOre(oreSearchRadius + 5f);

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

            if (curOreType > bestOreType)
            {
                if (Vector3.Distance(transform.position, ore.transform.position) >
                    Vector3.Distance(playerPos, ore.transform.position))
                    continue;
                bestOre = ore;
                bestOreType = curOreType;
                continue;
            }

            if (Vector3.Distance(transform.position, ore.transform.position) >
                Vector3.Distance(playerPos, ore.transform.position))
                continue;

            if (Vector3.Distance(transform.position, ore.transform.position) <
                Vector3.Distance(transform.position, bestOre.transform.position))
            {
                bestOre = ore;
                bestOreType = curOreType;
            }
        }

        return bestOre;
    }

    #endregion

    #region RlAgentFunctions

    // RL Agent functions

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
                ores = FindOres(RlOreSearchRadius, 0, oresblacklist);
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

            NormalisationData.Reset();

            foreach (GameObject ore in ores)
            {
                //TODO: Make a invalid path checker (is this needed, as the agent seems quite proficient at navigating)
                float distanceFromAgent = CalculatePathRemainingDistance(ore.transform.position);
                float distanceFromPlayer =
                    CalculatePathRemainingDistance(ore.transform.position, player.transform.position);
                float distanceFromBase = CalculatePathRemainingDistance(
                    FindClosestDepositWaypoint(ore.transform.position),
                    ore.transform.position);

                if (distanceFromAgent < 0 || distanceFromPlayer < 0 || distanceFromBase < 0)
                {
                    oresblacklist.Add(ore);
                    blacklisted++;
                    continue;
                }

                if (distanceFromAgent > NormalisationData.MaxDistanceFromAgent)
                    NormalisationData.MaxDistanceFromAgent = distanceFromAgent;
                else if (NormalisationData.MinDistanceFromAgent == 0 ||
                         distanceFromAgent < NormalisationData.MinDistanceFromAgent)
                    NormalisationData.MinDistanceFromAgent = distanceFromAgent;

                if (distanceFromPlayer > NormalisationData.MaxDistanceFromPlayer)
                    NormalisationData.MaxDistanceFromPlayer = distanceFromPlayer;
                else if (NormalisationData.MinDistanceFromPlayer == 0 ||
                         distanceFromPlayer < NormalisationData.MinDistanceFromPlayer)
                    NormalisationData.MinDistanceFromPlayer = distanceFromPlayer;

                if (distanceFromBase > NormalisationData.MaxDistanceFromBase)
                    NormalisationData.MaxDistanceFromBase = distanceFromBase;
                else if (NormalisationData.MinDistanceFromBase == 0 ||
                         distanceFromBase < NormalisationData.MinDistanceFromBase)
                    NormalisationData.MinDistanceFromBase = distanceFromBase;

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
    public IEnumerator GoToOreAndMineCoroutine(OreData oreData, Action punishCallback, Coroutine travellingPunish,
        Action<List<OreData>> callback)
    {
        Vector3 orePos = oreData.orePos;
        navMeshAgent.SetDestination(orePos);
        // Not using recheck since gains are small and computational cost is high.
        // Coroutine recheckCoroutine = StartCoroutine(ReCheckOres(recheckCallback));
        Debug.Log("Started GoToOreAndMineCoroutine");

        // On Rare occasions, the pathfinding gets stuck

        float stuckCheckInterval = 2f;
        float stuckCheckTimer = 0f;
        Vector3 lastPosition = transform.position;
        float stuckDistanceThreshold = 1f;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            //This ensures that the destination has been set properly
            if (Vector3.Distance(orePos, navMeshAgent.destination) > 1f)
                continue;

            // Check if the agent is stuck
            stuckCheckTimer += Time.fixedDeltaTime;
            if (stuckCheckTimer >= stuckCheckInterval)
            {
                stuckCheckTimer = 0f;
                float prevDistanceFromOre = Vector3.Distance(lastPosition, orePos);
                float currentDistanceFromOre = Vector3.Distance(transform.position, orePos);
                float distanceMoved = prevDistanceFromOre - currentDistanceFromOre;
                lastPosition = transform.position;

                if (distanceMoved < stuckDistanceThreshold)
                {
                    Debug.LogWarning("Agent is stuck, changing ore");
                    StopCoroutine(travellingPunish);
                    // punishCallback();
                    // Rare error when this is called when the ore does not exist.
                    // While it will hurt performance, it is necessary to ensure that the agent is not stuck.
                    try
                    {
                        StartCoroutine(GatherDataForAgent(callback,
                            new List<GameObject>() { oreData.oreToMine.gameObject }));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        punishCallback();
                        break;
                    }

                    yield break;
                }
            }

            if (navMeshAgent.remainingDistance < 1f && Vector3.Distance(orePos, transform.position) < 1f)
            {
                Debug.Log("Reached destination");
                // StopCoroutine(recheckCoroutine);

                if (oreData.oreScript.isBeingMined)
                {
                    Debug.Log("Ore is being mined by another agent, punishing agent");
                    punishCallback();
                    break;
                }

                StopCoroutine(travellingPunish);
                Debug.Log("Starting mining process");
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
                break;
            }
        }

        Debug.Log("Ending GoToOreAndMineCoroutine");
        StartCoroutine(GatherDataForAgent(callback));
    }

    // GoToDepositCoroutine
    public IEnumerator GoToDepositCoroutine(Action<List<OreData>> oreListCallback, Coroutine travellingPunish)
    {
        Vector3 depositWaypoint = FindClosestDepositWaypoint(transform.position);
        navMeshAgent.SetDestination(depositWaypoint);

        while (true)
        {
            if (navMeshAgent.remainingDistance < 1f)
                break;
            // FixedUpdate is used to ensure that the agent moves smoothly, and improves performance.
            yield return new WaitForFixedUpdate();
        }

        StopCoroutine(travellingPunish);
        StartCoroutine(GatherDataForAgent(oreListCallback));
    }

    #endregion
}

public static partial class vAgentAnimatorParameters
{
    public static int InputHorizontal = Animator.StringToHash("InputHorizontal");
    public static int InputVertical = Animator.StringToHash("InputVertical");
    public static int InputMagnitude = Animator.StringToHash("InputMagnitude");
    public static int IsGrounded = Animator.StringToHash("IsGrounded");
    public static int IsStrafing = Animator.StringToHash("IsStrafing");
    public static int IsSprinting = Animator.StringToHash("IsSprinting");
    public static int GroundDistance = Animator.StringToHash("GroundDistance");
}