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
//     Gold = 12.8f,
//     Silver = 8f,
//     Copper = 4.8f,
// }

public class Mining : MonoBehaviour
{
    string buttonSfx = "event:/SFX_Events/Mine";
    FMOD.Studio.EventInstance mineInstance;
    
    public LayerMask oreLayer;

    private GameObject pickaxeHandle;
    
    protected Animator animator;
    protected bool isMining = false;
    
    static float animSpeed = 1.3f;

    private Dictionary<OreType, float> oreMiningTime = new()
    {
        { OreType.Gold, 12.8f / animSpeed },
        { OreType.Silver, 8f / animSpeed },
        { OreType.Copper, 4.8f / animSpeed }
    };
    
    public Dictionary<OreType, float> OreMiningTime
    {
        get => oreMiningTime;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        mineInstance = FMODUnity.RuntimeManager.CreateInstance(buttonSfx);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pickaxeHandle = GameObject.FindGameObjectWithTag("Pickaxe_Handle");
        pickaxeHandle.SetActive(false);
        animator = GetComponent<Animator>();
    }

    public void Stop()
    {
        StopAllCoroutines();
        PostMine();
        animator.SetBool("Mining", false);
        isMining = false;
        pickaxeHandle.SetActive(false);
    }
    
    protected virtual void PreMine(GameObject ore)
    {
        
    }

    protected virtual void PostMine()
    {
        
    }

    protected IEnumerator MiningCoroutine(GameObject currentOre, AgentData agentData)
    {
        OreScript oreScript = currentOre.GetComponent<OreScript>();
        if (oreScript.isBeingMined)
            yield break;
        Debug.Log("Mining");
        float timeToMine = (float) oreMiningTime[oreScript.oreType];
        oreScript.isBeingMined = true;
        pickaxeHandle.SetActive(true);
        PreMine(currentOre);
        isMining = true;
        animator.SetBool("Mining", true);
        agentData.TimeSpentMining += timeToMine;
        mineInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform));
        mineInstance.start();

        yield return new WaitForSeconds(timeToMine);

        mineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        animator.SetBool("Mining", false);
        Destroy(currentOre);    
        isMining = false;
        pickaxeHandle.SetActive(false);
        PostMine();
    }

    
}
