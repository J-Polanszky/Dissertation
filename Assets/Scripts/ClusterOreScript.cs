using UnityEngine;

public class ClusterOreScript : MonoBehaviour
{
    public GameObject[] spawnpoints;
    
    public GameObject goldOrePrefab;
    public GameObject silverOrePrefab;
    public GameObject copperOrePrefab;

    int destroyedOreCount = 0;
    System.Action destroyedCallback;

    void oreDestroyed()
    {
        destroyedOreCount++;
        
        if (destroyedOreCount >= 3)
            Destroy(gameObject);
    }
    
    public void SpawnOres(int goldCount, int silverCount, int copperCount, System.Action callback)
    {
        destroyedCallback = callback;
        for (int i = 0; i < spawnpoints.Length; i++)
        {
            int randomNum = UnityEngine.Random.Range(1, 101);
            GameObject spawnedOre;
            
            if (randomNum <= copperCount)
                spawnedOre = Instantiate(copperOrePrefab, spawnpoints[i].transform.position, Quaternion.identity, transform);
            
            else if (randomNum <= copperCount + silverCount)
                spawnedOre = Instantiate(silverOrePrefab, spawnpoints[i].transform.position, Quaternion.identity, transform);
            
            else
                spawnedOre = Instantiate(goldOrePrefab, spawnpoints[i].transform.position, Quaternion.identity, transform);
            
            spawnedOre.GetComponent<OreScript>().destroyedCallback = oreDestroyed;
        }
    }

    void OnDestroy()
    {
        // Stops the game from freezing when exiting
        if (destroyedOreCount >= 3)
            destroyedCallback?.Invoke();
    }
}
