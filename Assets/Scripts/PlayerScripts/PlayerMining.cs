using System;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMining : Mining
{
    public Transform headTransform;
    public Transform[] bodyTransforms;
    vThirdPersonInput input;
    Rigidbody agentRigidbody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        input = GetComponent<vThirdPersonInput>();
        agentRigidbody = GetComponent<Rigidbody>();
    }
    
    protected override void PreMine(GameObject ore)
    {
        input.enabled = false;
        input.cc.input.x = 0;
        input.cc.input.z = 0;
        agentRigidbody.linearVelocity = Vector3.zero;
        ore.GetComponent<OreScript>().playerMined = true;
    }

    protected override void PostMine()
    {
        input.enabled = true;
    }

    GameObject RaycastCheck(int layerMask)
    {
        Ray ray = new Ray(headTransform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1.2f, layerMask))
        {
            Debug.Log("Raycast found: " + hit.collider.gameObject.name);
            return hit.collider.gameObject;
        }

        for (int i = 0; i < bodyTransforms.Length; i++)
        {
            Ray newRay = new Ray(bodyTransforms[i].position, bodyTransforms[i].forward);
            RaycastHit newHit;
            
            if (Physics.Raycast(newRay, out newHit, 0.75f, layerMask))
            {
                Debug.Log("Raycast found: " + newHit.collider.gameObject.name);
                return newHit.collider.gameObject;
            }
        }
        
        Debug.Log("Raycast found nothing");
        return null;
    }
    
    // private void Update()
    // {
    //     DrawRay();
    // }

    // void DrawRay()
    // {
    //     Ray ray = new Ray(headTransform.position, Camera.main.transform.forward);
    //     Vector3 endPosition = ray.origin + (ray.direction * 1.2f);
    //     Debug.DrawLine(ray.origin, endPosition, Color.red);
    //     ray = new Ray(bodyTransforms[0].position, bodyTransforms[0].forward);
    //     endPosition = ray.origin + (ray.direction * 0.75f);
    //     Debug.DrawLine(ray.origin, endPosition, Color.green);
    //     ray = new Ray(bodyTransforms[1].position, bodyTransforms[1].forward);
    //     endPosition = ray.origin + (ray.direction * 0.75f);
    //     Debug.DrawLine(ray.origin, endPosition, Color.blue);
    // }
    
    public void Mine(InputAction.CallbackContext context)
    {
        if (isMining)
            return;

        if (!context.performed)
            return;

        if (!input.cc.isGrounded)
            return;

        GameObject currentOre = RaycastCheck(oreLayer);
        
        if (currentOre == null || !oreMiningTime.ContainsKey(currentOre.tag))
            return;

        float timeToMine = (float) oreMiningTime[currentOre.tag] / 10;
        
        StartCoroutine(MiningCoroutine(currentOre, timeToMine));
    }
}
