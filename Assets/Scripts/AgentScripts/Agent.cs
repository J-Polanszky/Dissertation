using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    // Get Nav mesh agent object and on start make player walk towards barn_example object, and when he collides with it, then walks till he reaches the player(whose position always changes),
    
    NavMeshAgent navMeshAgent;
    Animator animator;
    AgentMining agentMining;
    private GameObject goldObject, silverObject, copperObject;
    private Vector3 barn, gold, silver, copper;

    private bool startAgent = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.isStopped = true;
        navMeshAgent.speed += GameData.Difficulty;
        agentMining = GetComponent<AgentMining>();
        animator = GetComponent<Animator>();
        // Take world position
        barn = GameObject.Find("barn_example").transform.position;
        StartCoroutine(WaitAndChangeDestination());
    }
    
    IEnumerator WaitAndChangeDestination()
    {
        yield return new WaitForSeconds(1);
        goldObject = GameObject.FindWithTag("Gold");
        silverObject = GameObject.FindWithTag("Silver");
        copperObject = GameObject.FindWithTag("Copper");
        gold = goldObject.transform.position;
        silver = silverObject.transform.position;
        copper = copperObject.transform.position;
        navMeshAgent.destination = gold;
        navMeshAgent.isStopped = false;
        startAgent = true;
    }

    // Update is called once per frame
    void Update()
    {
        // This is just a debug before developing the state machine since having the entire navmesh working would hasten development
        //Check if agent collided with barn, if so change target to be player, and keep chasing the player.
        //When set as destination, the y may change.
        
        if(!startAgent)
            return;
        
        if (navMeshAgent.destination.x == gold.x && navMeshAgent.destination.z == gold.z)
        {
            Debug.Log("Destination is Gold");
            if (!navMeshAgent.isStopped && navMeshAgent.remainingDistance < 1f)
            {
                Debug.Log("Destination Reached, changing to silver");
                agentMining.onMine += () => navMeshAgent.destination = silver;
                agentMining.Mine(goldObject);
            }
        }
        else if (navMeshAgent.destination.x == silver.x && navMeshAgent.destination.z == silver.z)
        {
            Debug.Log("Destination is Silver");
            if (!navMeshAgent.isStopped && navMeshAgent.remainingDistance < 1f)
            {
                Debug.Log("Destination Reached, changing to copper");
                agentMining.onMine += () => navMeshAgent.destination = copper;
                agentMining.Mine(silverObject);
            }
        }
        else if (navMeshAgent.destination.x == copper.x && navMeshAgent.destination.z == copper.z)
        {
            Debug.Log("Destination is Copper");
            if (!navMeshAgent.isStopped && navMeshAgent.remainingDistance < 1f)
            {
                Debug.Log("Destination Reached, changing to barn");
                agentMining.onMine += () => navMeshAgent.destination = barn;
                agentMining.Mine(copperObject);
            }
        }
        else if (navMeshAgent.destination.x == barn.x && navMeshAgent.destination.z == barn.z)
        {
            Debug.Log("Destination is Barn");
            // Barn needs a larger remaining distance
            if (!navMeshAgent.isStopped && navMeshAgent.remainingDistance < 7f)
            {
                Debug.Log("Destination Reached, changing to player");
                navMeshAgent.destination = GameObject.FindWithTag("Player").transform.position;
            }
        }else{
            Debug.Log("Chasing Player");
            navMeshAgent.destination = GameObject.FindWithTag("Player").transform.position;
            //If agent is chasing player, and player is within 1f distance, then stop the agent.
            if (navMeshAgent.remainingDistance < 1f)
            {
                navMeshAgent.isStopped = true;
                // Destroying to allow testing other game changes without having agent push and take up resources.
                // Destroy(gameObject);
            }else{
                navMeshAgent.isStopped = false;
            }
        }

        UpdateAnimator();
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