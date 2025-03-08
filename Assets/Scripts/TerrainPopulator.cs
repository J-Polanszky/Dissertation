using System.Collections.Generic;
using UnityEngine;

public class TerrainPopulator : MonoBehaviour
{
    public Transform TerrainPopulatorPrefab;
    public GameObject[] grassPrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] treePrefab;
    public MeshRenderer terrainMesh;
    public int grassCount = 150;
    public int bushCount = 30;
    public int treeCount = 20;

    public GameObject OreClusterPrefab;

    int oreClusterCount = 0;

    // Percentage out of 100 based on difficulty
    int goldCount = 0;
    int silverCount = 0;
    int copperCount = 0;

    private float edgeBuffer = 12;
    private List<Vector3> depositPositions = new();

    delegate bool VerifyCallbackType(Vector3 t);

    void Start()
    {
        terrainMesh = GetComponent<MeshRenderer>();
        foreach (GameObject depo in GameObject.FindGameObjectsWithTag("Deposit"))
        {
            depositPositions.Add(depo.transform.position);
        }

        PopulateTerrain();
    }

    public void ResetTerrain()
    {
        Debug.Log("Resetting");
        foreach (Transform child in TerrainPopulatorPrefab)
        {
            Destroy(child.gameObject);
        }

        PopulateTerrain();
    }

    void PopulateTerrain()
    {
        Bounds bounds = terrainMesh.bounds;

        for (int i = 0; i < grassCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, grassPrefabs.Length);
            PlacePrefabRandomly(grassPrefabs[randomIndex], bounds, 1.5f);
        }

        for (int i = 0; i < bushCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, bushPrefabs.Length);
            PlacePrefabRandomly(bushPrefabs[randomIndex], bounds, edgeBuffer);
        }

        for (int i = 0; i < treeCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, treePrefab.Length);
            PlacePrefabRandomly(treePrefab[randomIndex], bounds, edgeBuffer);
        }
    }

    bool IsInCorner(Vector3 position)
    {
        float cornerRadius = 7f; // Adjust as needed
        return Vector3.Distance(position, depositPositions[0]) < cornerRadius ||
               Vector3.Distance(position, depositPositions[1]) < cornerRadius;
    }

    GameObject PlacePrefabRandomly(GameObject prefab, Bounds bounds, float buffer,
        VerifyCallbackType verifyCallback = null)
    {
        Vector3 randomPosition;
        do
        {
            randomPosition = new Vector3(
                Random.Range(bounds.min.x + buffer, bounds.max.x - buffer),
                bounds.min.y,
                Random.Range(bounds.min.z + buffer, bounds.max.z - buffer)
            );
        } while (IsInCorner(randomPosition) || (verifyCallback != null && !verifyCallback(randomPosition)));

        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        return Instantiate(prefab, randomPosition, randomRotation, TerrainPopulatorPrefab);
    }


    public void SpawnOreCluster()
    {
        // print("Spawning Ore Cluster");
        GameObject clusterObject = PlacePrefabRandomly(OreClusterPrefab, terrainMesh.bounds, edgeBuffer,
            ClusterOreScript.CheckIfValidSpawn);

        clusterObject.GetComponent<ClusterOreScript>().SpawnOres(goldCount, silverCount, copperCount, SpawnOreCluster);
    }

    public void SetOreSpawns(int oreCluster, int gold, int silver, int copper)
    {
        // Debug.Log("Spawning");
        oreClusterCount = oreCluster;
        goldCount = gold;
        silverCount = silver;
        copperCount = copper;

        for (int i = 0; i < oreClusterCount; i++)
            SpawnOreCluster();
    }
}