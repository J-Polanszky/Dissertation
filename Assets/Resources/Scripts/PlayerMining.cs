using System;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// 1.6s per swing
public enum OreMiningTime
{
    Gold = 160,
    Silver = 96,
    Iron = 48,
}

public class PlayerMining : MonoBehaviour
{
    public Transform headTransform;
    public Transform[] bodyTransforms;
    public LayerMask oreLayer;

    private GameObject pickaxeHandle;
    vThirdPersonInput input;
    Animator animator;
    bool isMining = false;

    private Dictionary<string, int> oreMiningTime = new()
    {
        { "Gold", (int)OreMiningTime.Gold },
        { "Silver", (int)OreMiningTime.Silver },
        { "Iron", (int)OreMiningTime.Iron }
    };

    Rigidbody playerRigidbody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pickaxeHandle = GameObject.FindGameObjectWithTag("Pickaxe_Handle");
        pickaxeHandle.SetActive(false);
        input = GetComponent<vThirdPersonInput>();
        animator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
    }

    GameObject CheckForOre()
    {
        Ray ray = new Ray(headTransform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.2f, oreLayer))
        {
            Debug.Log("Ore found: " + hit.collider.gameObject.name);
            return hit.collider.gameObject;
        }

        for (int i = 0; i < bodyTransforms.Length; i++)
        {
            Ray newRay = new Ray(bodyTransforms[i].position, bodyTransforms[i].forward);
            RaycastHit newHit;
            
            if (Physics.Raycast(newRay, out newHit, 0.75f, oreLayer))
            {
                Debug.Log("Ore found: " + newHit.collider.gameObject.name);
                return newHit.collider.gameObject;
            }
        }
        
        Debug.Log("No Ore Found");
        return null;
    }

    private void Update()
    {
        DrawRay();
    }

    void DrawRay()
    {
        Ray ray = new Ray(headTransform.position, Camera.main.transform.forward);
        Vector3 endPosition = ray.origin + (ray.direction * 1.2f);
        Debug.DrawLine(ray.origin, endPosition, Color.red);
        ray = new Ray(bodyTransforms[0].position, bodyTransforms[0].forward);
        endPosition = ray.origin + (ray.direction * 0.75f);
        Debug.DrawLine(ray.origin, endPosition, Color.green);
        ray = new Ray(bodyTransforms[1].position, bodyTransforms[1].forward);
        endPosition = ray.origin + (ray.direction * 0.75f);
        Debug.DrawLine(ray.origin, endPosition, Color.blue);
    }

    IEnumerator MiningCoroutine()
    {
        GameObject currentOre = CheckForOre();
        
        if (currentOre == null || !oreMiningTime.ContainsKey(currentOre.tag))
            yield break;

        float timeToMine = (float) oreMiningTime[currentOre.tag] / 10;

        print("Mining");
        currentOre.GetComponent<OreScript>().playerMined = true;
        pickaxeHandle.SetActive(true);
        input.enabled = false;
        input.cc.input.x = 0;
        input.cc.input.z = 0;
        isMining = true;
        animator.SetBool("Mining", true);
        playerRigidbody.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(timeToMine);

        animator.SetBool("Mining", false);
        Destroy(currentOre);    
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


        StartCoroutine(MiningCoroutine());

    }
}
