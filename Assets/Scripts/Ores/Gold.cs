using UnityEngine;

public class Gold : OreScript
{
    void Awake()
    { 
        oreScore = 10;
        oreType = OreType.Gold;
    }
}
