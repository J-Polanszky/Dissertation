using UnityEngine;

public class Agent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject depositBuilding in GameObject.FindGameObjectsWithTag("Deposit"))
        {
            depositBuilding.GetComponent<DepositBuilding>().agentData = GameData.MachineData;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
