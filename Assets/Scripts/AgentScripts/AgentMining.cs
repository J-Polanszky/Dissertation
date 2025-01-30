using UnityEngine;
using UnityEngine.AI;

public class AgentMining : Mining
{
    public System.Action onMine;
    NavMeshAgent navMeshAgent;

    protected override void Start()
    {
        base.Start();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void Mine(GameObject ore)
    {
        if (ore.GetComponent<OreScript>().isBeingMined || !OreMiningTime.ContainsKey(ore.tag))
            return;
        
        StartCoroutine(MiningCoroutine(ore));
    }

    private void FaceOre(GameObject ore)
    {
        if (ore == null) return;

        Vector3 direction = (ore.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        // transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        transform.rotation = lookRotation;
    }
    
    protected override void PreMine(GameObject ore)
    {
        navMeshAgent.isStopped = true;
        FaceOre(ore);
    }

    protected override void PostMine()
    {
        if (onMine != null)
            onMine();
        
        navMeshAgent.isStopped = false;
    }
    
}
