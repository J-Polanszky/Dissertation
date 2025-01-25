using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    // Get Nav mesh agent object and on start make player walk towards barn_example object, and when he collides with it, then walks till he reaches the player(whose position always changes),
    
    NavMeshAgent navMeshAgent;
    Animator animator;
    private Vector3 barn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        barn = GameObject.Find("barn_example").transform.position;
        navMeshAgent.destination = barn;
    }

    // Update is called once per frame
    void Update()
    {
        // This is just a debug before developing the state machine since having the entire navmesh working would hasten development
        //Check if agent collided with barn, if so change target to be player, and keep chasing the player.
        //When set as destination, the y may change.
        if (navMeshAgent.destination.x == barn.x && navMeshAgent.destination.z == barn.z)
        {
            Debug.Log("Destination is Barn");
            // Barn needs a smaller remaining distance
            if (navMeshAgent.remainingDistance < 7f)
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

        public void TriggerMiningAnimation()
        {
            animator.SetTrigger("Mine"); // Assuming you have a trigger parameter named "Mine" in your Animator
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