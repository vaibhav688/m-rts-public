using UnityEngine;
using Fusion;
using UnityEngine.AI;

public class Unit : NetworkBehaviour
{
    public float movementSpeed = 5f;
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float health = 100f;
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;

    [Networked] public PlayerRef Owner { get; set; }
    protected Castle targetCastle;
    protected float lastAttackTime;

    protected NavMeshAgent navMeshAgent;
    protected bool isInitialized = false;

    public override void Spawned()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
        }

        FindEnemyCastle();
        isInitialized = true;
    }

    public void Initialize(PlayerRef owner)
    {
        Owner = owner;
    }

    public void ChangeOwner(PlayerRef newOwner)
    {
        Owner = newOwner;
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
            // Attack the castle
            if (Runner.SimulationTime - lastAttackTime >= attackCooldown)
            {
                AttackCastle();
            }
        }
    }

    protected void FindEnemyCastle()
    {
        Debug.Log("Attempting to find enemy castle...");
        GameObject enemyCastleObject = GameObject.FindWithTag("Enemy Castle");
        if (enemyCastleObject != null)
        {
            targetCastle = enemyCastleObject.GetComponent<Castle>();
            if (targetCastle != null)
            {
                Debug.Log("Enemy castle found and assigned.");
            }
            else
            {
                Debug.LogError("Found object with 'Enemy Castle' tag, but it does not have a Castle component.");
            }
        }
        else
        {
            Debug.LogError("Enemy castle not found!");
        }
    }



    protected void AttackCastle()
    {
        if (targetCastle != null)
        {
            FireArrow(targetCastle.transform.position);
            lastAttackTime = Runner.SimulationTime;
        }
    }

    public void FireArrow(Vector3 targetPosition)
    {
        GameObject arrowInstance = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
        Arrow1 arrow = arrowInstance.GetComponent<Arrow1>();
        arrow.Initialize(targetPosition, attackDamage, Owner);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Notify score manager
            GameManager.Instance.UpdateScores(Owner == Runner.LocalPlayer);
            Destroy(gameObject);
        }
    }
}
