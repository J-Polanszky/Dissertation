using Invector.vCharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    vThirdPersonController cc;
    readonly float defaultWalkSpeed = 2;
    readonly float defaultRunSpeed = 4;
    readonly float defaultSprintSpeed = 6;
    
    void Start()
    {
        cc = GetComponent<vThirdPersonController>();
        cc.strafeSpeed.walkSpeed = defaultWalkSpeed;
        cc.strafeSpeed.runningSpeed = defaultRunSpeed;
        cc.strafeSpeed.sprintSpeed = defaultSprintSpeed;
        
        GameData.PlayerData.onInventoryUpdated += ChangeSpeed;
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
        float multiplier = 1 - (float) GameData.PlayerData.TotalInventory / (GameData.MaximumInvQty * 2);
        
        // Debug.Log("Player inventory: " + GameData.PlayerData.TotalInventory + " Multiplier: " + multiplier);
        cc.strafeSpeed.walkSpeed = defaultWalkSpeed * multiplier;
        cc.strafeSpeed.runningSpeed = defaultRunSpeed * multiplier;
        cc.strafeSpeed.sprintSpeed = defaultSprintSpeed * multiplier;
        
    }
}
