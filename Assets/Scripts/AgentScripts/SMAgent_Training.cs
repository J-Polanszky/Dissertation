using UnityEngine;

public class SMAgent_Training : SMAgent
{
   protected new void Awake()
   {
      agentData = GameData.PlayerData;
      depoName = "PlayerDeposit";
   }
}
