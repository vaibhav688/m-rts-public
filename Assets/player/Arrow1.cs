using UnityEngine;
using Fusion;

public class Arrow1 : NetworkBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    private Vector3 targetPosition;
    private float damage;
    private PlayerRef owner;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector3 targetPos, float arrowDamage, PlayerRef arrowOwner)
    {
        targetPosition = targetPos;
        damage = arrowDamage;
        owner = arrowOwner;
    }

    private void Update()
    {
        if (targetPosition != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= 0.1f)
            {
                // Hit the target
                HitTarget();
            }
        }
    }

    private void HitTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var collider in colliders)
        {
            Unit unit = collider.GetComponent<Unit>();
            if (unit != null && unit.Owner != owner)
            {
                unit.TakeDamage(damage);
            }

            Castle castle = collider.GetComponent<Castle>();
            if (castle != null && castle.Owner != owner)
            {
                castle.RPC_TakeDamage(damage);
            }
        }

        Destroy(gameObject); // Destroy the arrow after hitting the target
    }
}
