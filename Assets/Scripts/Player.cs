using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject depositBuilding in GameObject.FindGameObjectsWithTag("Deposit"))
        {
            depositBuilding.GetComponent<DepositBuilding>().playerData = GameData.PlayerData;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
