using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class AgentFunctions : MonoBehaviour
{
    Animator animator;
    NavMeshAgent navMeshAgent;
    public int oreLayer;
    public float oreSearchRadius = 10f;
    
    readonly float defaultStateSpeed = 1f;
    readonly float defaultRlSpeed = 2f;
    readonly float defaultAcceleration = 5f;
    public bool isStateMachine = false;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        oreLayer = LayerMask.GetMask("Ore");

        oreSearchRadius += GameData.Difficulty * 3;
        
        if (isStateMachine)
        {
            navMeshAgent.speed = defaultStateSpeed + (float)GameData.Difficulty / 2;
            navMeshAgent.acceleration = defaultAcceleration;
            return;
        }
        
        navMeshAgent.speed = defaultRlSpeed;
        navMeshAgent.acceleration = defaultAcceleration;
        
        GameData.MachineData.onInventoryUpdated += ChangeSpeed;
    }
    
    void ChangeSpeed()
    {
        // State machine speed varies, RlAgent speed is always the same.

        if (GameData.MachineData.TotalInventory == 0)
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
        float multiplier = 1 - (float) GameData.MachineData.TotalInventory / (GameData.MaximumInvQty * 2);
            
        if (isStateMachine)
        {
            navMeshAgent.speed = (defaultStateSpeed + (float)GameData.Difficulty / 2) * multiplier;
            navMeshAgent.acceleration = defaultAcceleration * multiplier;
            return;
        }
        navMeshAgent.speed = defaultRlSpeed * multiplier;
        navMeshAgent.acceleration = defaultAcceleration * multiplier;
        
    }
    
    public GameObject FindBestOre(float oreSearchRadius = -1, int recursiveDepth = 0)
    {
        // It is impossible for no ores to be found within this limit, so while it is higher than i would like, it is the safest option.
        // FIXME: Sometimes this doesnt find ores, even if they are right next to the agent, and it only happens after depositing.
        if (recursiveDepth > 30)
            throw new Exception("No Ores Found After 30 Recursive Calls");

        if (oreSearchRadius < 0)
            oreSearchRadius = this.oreSearchRadius;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, oreSearchRadius, oreLayer);
        Vector3 playerPos = GameObject.FindWithTag("Player").transform.position;
        GameObject bestOre = null;
        int bestOreType = -1;

        foreach (Collider col in hitColliders)
        {
            if (col.gameObject.GetComponent<OreScript>().isBeingMined)
                continue;

            if (bestOreType == -1)
            {
                bestOre = col.gameObject;
                bestOreType = (int) Enum.Parse<OreType>(col.gameObject.tag);
                continue;
            }
            
            int curOreType = (int) Enum.Parse<OreType>(col.gameObject.tag);

            if (curOreType < bestOreType)
                continue;

            if (curOreType > bestOreType)
            {
                if (Vector3.Distance(transform.position, col.transform.position) >
                    Vector3.Distance(playerPos, col.transform.position))
                    continue;
                bestOre = col.gameObject;
                bestOreType = curOreType;
                continue;
            }

            if (Vector3.Distance(transform.position, col.transform.position) >
                Vector3.Distance(playerPos, col.transform.position))
                continue;

            if (Vector3.Distance(transform.position, col.transform.position) <
                Vector3.Distance(transform.position, bestOre.transform.position))
            {
                bestOre = col.gameObject;
                bestOreType = curOreType;
            }
        }

        if (bestOreType == -1)
            return FindBestOre(this.oreSearchRadius + 5f, recursiveDepth + 1);

        return bestOre;
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
