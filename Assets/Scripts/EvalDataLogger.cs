using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EvalData
{
    public float time;
    public float totalInventorySpaceTaken;
    public float score;
    public float totalScoreHoarded;
}

public class EvalDataLogger : MonoBehaviour
{
    public static EvalDataLogger instance;

    List<EvalData> evalData = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(GatherDataLoop());
    }

    public void GatherData()
    {
        //Test with normalisation if needed
        EvalData data = new();
        data.time = GameData.InitialTime - GameData.TimeLeft;
        data.totalInventorySpaceTaken = GameData.MachineData.TotalInventory;
        data.score = GameData.MachineData.Score;
        data.totalScoreHoarded = GameData.MachineData.inventory[OreType.Gold].Score +
                                 GameData.MachineData.inventory[OreType.Silver].Score +
                                 GameData.MachineData.inventory[OreType.Copper].Score;

        evalData.Add(data);
    }

    public void WriteData()
    {
        //TODO: Write the data to a csv file
    }

    public void OnApplicationQuit()
    {
        GatherData();
        WriteData();
    }

    IEnumerator GatherDataLoop()
    {
        yield return new WaitForSeconds(45);
        while (true)
        {
            GatherData();
            yield return new WaitForSeconds(15);
        }
    }
}