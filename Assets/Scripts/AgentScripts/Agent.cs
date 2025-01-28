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

public class Agent : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
    Animator animator;

    AgentMining agentMining;

    public bool startAgent = true;
    private float oreSearchRadius = 10f;
    private int oreLayer;
    private AgentState agentState = AgentState.Idle;
    private GameObject oreToMine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        agentMining = GetComponent<AgentMining>();
        animator = GetComponent<Animator>();

        oreLayer = LayerMask.GetMask("Ore");

        navMeshAgent.isStopped = true;
        navMeshAgent.speed += GameData.Difficulty;

        agentMining.onMine += SetAgentToIdle;

        oreSearchRadius += GameData.Difficulty * 3;
    }
    

    // Update is called once per frame
    void Update()
    {
        // Add checker for time left and distance so agent deposits before time runs out.
        // Fix collision avoidance issues.
        
        if (!startAgent)
            return;

        UpdateAnimator();

        // Check if the agent is idle.
        if (agentState == AgentState.Idle)
        {
            Debug.Log("@M total inv" + GameData.MachineData.totalInventory);
            oreToMine = FindBestOre(oreSearchRadius);

            Vector3 depoPos = FindBestDeposit();

            Debug.Log("Agent inv after mining: " + (GameData.MachineData.totalInventory + GameData.InvStorageQty[oreToMine.tag]));
            // Check if inventory would be full after mining, or is over half full, and if the deposit is closer than the ore, go deposit.
            if ((GameData.MachineData.totalInventory + GameData.InvStorageQty[oreToMine.tag]) > 20 ||
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
                    oreToMine = FindBestOre(oreSearchRadius);
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

    private void SetAgentToIdle()
    {
        agentState = AgentState.Idle;
        navMeshAgent.isStopped = true;
    }

    private Vector3 FindBestDeposit()
    {
        GameObject[] deposits = GameObject.FindGameObjectsWithTag("Deposit");
        Transform bestDeposit = null;

        foreach (GameObject deposit in deposits)
        {
            if (bestDeposit == null)
            {
                bestDeposit = deposit.transform;
                continue;
            }

            if (Vector3.Distance(transform.position, deposit.transform.position) <
                Vector3.Distance(transform.position, bestDeposit.position))
                bestDeposit = deposit.transform;
        }

        return bestDeposit.position;
    }

    private GameObject FindBestOre(float oreSearchRadius, int recursiveDepth = 0)
    {
        if (recursiveDepth > 10)
            throw new Exception("No Ores Found After 10 Recursive Calls");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, oreSearchRadius, oreLayer);
        Dictionary<string, int> oreValue = new()
        {
            { "Gold", 3 },
            { "Silver", 2 },
            { "Copper", 1 }
        };
        Vector3 playerPos = GameObject.FindWithTag("Player").transform.position;
        GameObject bestOre = null;

        foreach (Collider col in hitColliders)
        {
            if (col.gameObject.GetComponent<OreScript>().isBeingMined)
                continue;

            if (bestOre == null)
            {
                bestOre = col.gameObject;
                continue;
            }

            if (oreValue[col.gameObject.tag] < oreValue[bestOre.tag])
                continue;

            if (oreValue[col.gameObject.tag] > oreValue[bestOre.tag])
            {
                if (Vector3.Distance(transform.position, col.transform.position) >
                    Vector3.Distance(playerPos, col.transform.position))
                    continue;
                bestOre = col.gameObject;
                continue;
            }

            if (Vector3.Distance(transform.position, col.transform.position) >
                Vector3.Distance(playerPos, col.transform.position))
                continue;

            if (Vector3.Distance(transform.position, col.transform.position) <
                Vector3.Distance(transform.position, bestOre.transform.position))
                bestOre = col.gameObject;
        }

        if (bestOre == null)
            return FindBestOre(this.oreSearchRadius + 5f, recursiveDepth + 1);

        return bestOre;
    }

    private void UpdateAnimator()
    {
        if (animator == null || !animator.enabled) return;

        Vector3 velocity = navMeshAgent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        float speed = localVelocity.z;

        animator.SetFloat(vAgentAnimatorParameters.InputVertical, speed);
        animator.SetFloat(vAgentAnimatorParameters.InputMagnitude, speed);
        animator.SetBool(vAgentAnimatorParameters.IsGrounded, true); // Assuming the agent is always grounded
    }
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