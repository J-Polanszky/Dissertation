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
    void Start()
    {
        terrainMesh = GetComponent<MeshRenderer>();
        PopulateTerrain();
    }

    void PopulateTerrain()
    {
        Bounds bounds = terrainMesh.bounds;

        for (int i = 0; i < grassCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, grassPrefabs.Length);
            PlacePrefabRandomly(grassPrefabs[randomIndex], bounds);
        }

        for (int i = 0; i < bushCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, bushPrefabs.Length);
            PlacePrefabRandomly(bushPrefabs[randomIndex], bounds);
        }

        for (int i = 0; i < treeCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, treePrefab.Length);
            PlacePrefabRandomly(treePrefab[randomIndex], bounds);
        }
    }

    void PlacePrefabRandomly(GameObject prefab, Bounds bounds)
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x - 1.5f),
            bounds.min.y,
            Random.Range(bounds.min.z, bounds.max.z - 1.5f)
        );

        Instantiate(prefab, randomPosition, Quaternion.identity, TerrainPopulatorPrefab);
    }
}