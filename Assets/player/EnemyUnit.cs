using UnityEngine;
using Fusion;
using UnityEngine.AI;
using System.Collections;

public class EnemyUnit : NetworkBehaviour
{
    public float health = 100f;
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Networked] public PlayerRef Owner { get; set; }

    private Castle targetCastle;
    private NavMeshAgent navMeshAgent;
    private bool isInitialized = false;
    private Coroutine findCastleCoroutine;

    public override void Spawned()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found!");
        }

        findCastleCoroutine = StartCoroutine(RetryFindEnemyCastle());
        isInitialized = true;
    }

    private IEnumerator RetryFindEnemyCastle()
    {
        int attempts = 0;
        while (targetCastle == null && attempts < 5)
        {
            FindEnemyCastle();
            attempts++;
            yield return new WaitForSeconds(1f);
        }

        if (targetCastle == null)
        {
            Debug.LogError("Enemy castle not found after multiple attempts!");
        }
        else
        {
            Debug.Log("Enemy castle successfully found.");
        }
    }

    void FindEnemyCastle()
    {
        Debug.Log("Attempting to find enemy castle...");

        GameObject playerCastleObject = GameObject.FindGameObjectWithTag("Player Castle");
        if (playerCastleObject != null)
        {
            targetCastle = playerCastleObject.GetComponent<Castle>();
            if (targetCastle == null)
            {
                Debug.LogError("Found object tagged 'Player Castle' but it does not have a Castle component!");
            }
            else
            {
                Debug.Log("Enemy castle found and assigned.");
            }
        }
        else
        {
            Debug.LogError("Enemy castle not found!");
        }
    }

    public void Initialize(PlayerRef owner)
    {
        Owner = owner;
    }

    public void ChangeOwner(PlayerRef newOwner)
    {
        Owner = newOwner;
        // Update any visuals or logic related to ownership change
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

    void AttackCastle()
    {
        if (targetCastle != null)
        {
            // Implement attack logic, such as dealing damage to the castle
            targetCastle.TakeDamage(attackDamage);
            lastAttackTime = Runner.SimulationTime;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Notify score manager
            if (Owner == Runner.LocalPlayer)
            {
                ScoreManager.Instance.AddPlayerScore(10); // Example score increment
            }
            else
            {
                ScoreManager.Instance.AddEnemyScore(10); // Example score increment
            }
            Destroy(gameObject);
        }
    }
}
