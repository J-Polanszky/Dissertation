using Invector.vCharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string walkSfx = "event:/SFX_Events/Walk";
    private string runSfx = "event:/SFX_Events/Run";

    FMOD.Studio.EventInstance walkInstance;
    FMOD.Studio.EventInstance runInstance;

    vThirdPersonController cc;
    readonly float defaultWalkSpeed = 2;
    readonly float defaultRunSpeed = 4;
    readonly float defaultSprintSpeed = 6;

    void Start()
    {
        walkInstance = FMODUnity.RuntimeManager.CreateInstance(walkSfx);
        runInstance = FMODUnity.RuntimeManager.CreateInstance(runSfx);

        walkInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        runInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));

        walkInstance.setVolume(0.15f);
        runInstance.setVolume(0.15f);

        cc = GetComponent<vThirdPersonController>();
        cc.strafeSpeed.walkSpeed = defaultWalkSpeed;
        cc.strafeSpeed.runningSpeed = defaultRunSpeed;
        cc.strafeSpeed.sprintSpeed = defaultSprintSpeed;

        cc.OnStartedWalking += HandleStartWalking;
        cc.OnStoppedWalking += HandleStopWalking;
        cc.OnStartedSprinting += HandleStartRun;
        cc.OnStoppedSprinting += HandleStopRun;

        GameData.PlayerData.onInventoryUpdated += ChangeSpeed;

        GetComponent<PlayerMining>().stopSfx += () =>
        {
            runInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            walkInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        };
    }

    void FixedUpdate()
    {
        walkInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
        runInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
    }

    void HandleStartWalking()
    {
        walkInstance.start();
    }

    void HandleStopWalking()
    {
        walkInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    void HandleStartRun()
    {
        walkInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        runInstance.start();
    }

    void HandleStopRun()
    {
        runInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        walkInstance.start();
    }

    void ChangeSpeed()
    {
        // Change the speed of a player, and slow him down to 50% speed if inventory is full
        // Make a multiplier variable between 1 and 0.5 based on the inventory count

        if (GameData.PlayerData.TotalInventory == 0)
        {
            cc.strafeSpeed.walkSpeed = defaultWalkSpeed;
            cc.strafeSpeed.runningSpeed = defaultRunSpeed;
            cc.strafeSpeed.sprintSpeed = defaultSprintSpeed;
        }

        // Divide by double max so that the minimum multiplier is 0.5
        float multiplier = 1 - (float)GameData.PlayerData.TotalInventory / (GameData.MaximumInvQty * 2);

        Debug.Log("Player inventory: " + GameData.PlayerData.TotalInventory + " Multiplier: " + multiplier);
        cc.strafeSpeed.walkSpeed = defaultWalkSpeed * multiplier;
        cc.strafeSpeed.runningSpeed = defaultRunSpeed * multiplier;
        cc.strafeSpeed.sprintSpeed = defaultSprintSpeed * multiplier;
    }

    void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (cc != null)
        {
            cc.OnStoppedWalking -= HandleStopWalking;
            cc.OnStartedWalking -= HandleStartWalking;
            cc.OnStoppedSprinting -= HandleStopRun;
            cc.OnStartedSprinting -= HandleStartRun;
        }

        walkInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        runInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        walkInstance.release();
        runInstance.release();
    }
}