using System.Collections;
using Invector.vCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMining : MonoBehaviour
{
    private GameObject pickaxeHandle;
    vThirdPersonInput input;
    Animator animator;
    bool isMining = false;
    
    internal Rigidbody rigidbody;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pickaxeHandle = GameObject.FindGameObjectWithTag("Pickaxe_Handle");
        pickaxeHandle.SetActive(false);
        print(pickaxeHandle);
        input = GetComponent<vThirdPersonInput>();
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator MiningCoroutine()
    {
       // TODO: Calculate the time taken till mining is complete
       // TODO: Change animation
       rigidbody.linearVelocity = Vector3.zero;
       yield return new WaitForSeconds(2f);
       // TODO: Change back animation
       // TODO: Break the ore
       input.enabled = true;
       isMining = false;
       pickaxeHandle.SetActive(false);
    }

    public void Mine(InputAction.CallbackContext context)
    {
        if (isMining)
            return;
        
        if (!context.performed)
            return;
        
        if (!input.cc.isGrounded)
            return;
        
        print("Mining");
        pickaxeHandle.SetActive(true);
        input.enabled = false;
        input.cc.input.x = 0;
        input.cc.input.z = 0;
        isMining = true;
        StartCoroutine(MiningCoroutine());
        
    }
}
