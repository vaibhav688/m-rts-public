using UnityEngine;
using Fusion;
using UnityEngine.AI;

public class Monk : Unit
{
    public float conversionRange = 5f;
    public float conversionCooldown = 10f;

    private float lastConversionTime;

    public override void Spawned()
    {
        base.Spawned();
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
        }

        FindEnemyCastle();
        isInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!isInitialized) return;

        if (targetCastle == null) return;

        if (Vector3.Distance(transform.position, targetCastle.transform.position) > attackRange)
        {
            // Move towards the castle
            navMeshAgent.SetDestination(targetCastle.transform.position);
        }
        else
        {
            // Convert enemy units if within range and cooldown allows
            if (Runner.SimulationTime - lastConversionTime >= conversionCooldown)
            {
                ConvertEnemyUnits();
            }
        }
    }

    void ConvertEnemyUnits()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, conversionRange);
        foreach (var hitCollider in hitColliders)
        {
            Unit enemyUnit = hitCollider.GetComponent<Unit>();
            if (enemyUnit != null && enemyUnit.Owner != Owner)
            {
                enemyUnit.ChangeOwner(Owner);
                lastConversionTime = Runner.SimulationTime;
                Debug.Log("Enemy unit converted!");
                break; // Only convert one unit at a time
            }
        }
    }
}
