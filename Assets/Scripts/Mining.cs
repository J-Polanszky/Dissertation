using System;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// 1.6s per swing animation. multiply the amount below with the anim speed.
// public enum OreMiningTime
// {
//     Gold = 128,
//     Silver = 80,
//     Copper = 48,
// }

public class Mining : MonoBehaviour
{
    public LayerMask oreLayer;

    private GameObject pickaxeHandle;
    
    protected Animator animator;
    protected bool isMining = false;
    
    static float animSpeed = 1.3f;

    protected Dictionary<string, float> oreMiningTime = new()
    {
        { "Gold", 128f / animSpeed },
        { "Silver", 80f / animSpeed },
        { "Copper", 48f / animSpeed }
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pickaxeHandle = GameObject.FindGameObjectWithTag("Pickaxe_Handle");
        pickaxeHandle.SetActive(false);
        animator = GetComponent<Animator>();
    }

    protected virtual void PreMine(GameObject ore)
    {
        
    }

    protected virtual void PostMine()
    {
        
    }

    protected IEnumerator MiningCoroutine(GameObject currentOre)
    {
        print("Mining");
        float timeToMine = (float) oreMiningTime[currentOre.tag] / 10;
        currentOre.GetComponent<OreScript>().isBeingMined = true;
        pickaxeHandle.SetActive(true);
        PreMine(currentOre);
        isMining = true;
        animator.SetBool("Mining", true);

        yield return new WaitForSeconds(timeToMine);

        animator.SetBool("Mining", false);
        Destroy(currentOre);    
        isMining = false;
        pickaxeHandle.SetActive(false);
        PostMine();
    }

    
}
