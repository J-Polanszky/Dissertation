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
    
    public readonly float defaultSpeed = 2f;
    public readonly float defaultAcceleration = 5f;
    public float searchRadius = 15f;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        oreLayer = LayerMask.GetMask("Ore");
        // depositBuilding = GameObject.Find("AgentDeposit").transform;
        
        StartCoroutine(DelayedStart());
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
            navMeshAgent.speed = defaultSpeed;
            navMeshAgent.acceleration = defaultAcceleration;
        }

        // Divide by 60 so that the minimum multiplier is 0.5
        float multiplier = 1 - (float)agentData.TotalInventory / (GameData.MaximumInvQty * 2);
        
        navMeshAgent.speed = defaultSpeed * multiplier;
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
                if (ore == null) continue;
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