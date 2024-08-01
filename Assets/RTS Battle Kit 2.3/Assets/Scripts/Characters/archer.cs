using UnityEngine;
using System.Collections;

public class archer : MonoBehaviour
{
    public GameObject arrow;
    public Transform arrowSpawner;
    public GameObject animationArrow;
    public float moveSpeed = 5f;
    public float attackRange = 10f;
    public float attackRate = 1f;
    public GameObject enemyCastle;  // Reference to the enemy castle
    public LayerMask enemyLayer;  // Layer for enemy units

    private bool shooting;
    private bool addArrowForce;
    private GameObject newArrow;
    private float shootingForce;
    private Animator animator;
    private float nextAttackTime;
    private Collider[] nearbyEnemies;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (enemyCastle == null)
        {
            enemyCastle = GameObject.FindGameObjectWithTag("Enemy Castle");
        }
        
        if (enemyCastle == null)
        {
            Debug.LogError("Enemy castle not found!");
        }
    }

    void Update()
    {
        if (enemyCastle == null) return;

        float distanceToCastle = Vector3.Distance(transform.position, enemyCastle.transform.position);
        
        // Check for nearby enemies
        nearbyEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        
        if (nearbyEnemies.Length > 0)
        {
            // Attack the nearest enemy
            AttackTarget(nearbyEnemies[0].gameObject);
        }
        else if (distanceToCastle <= attackRange)
        {
            // Attack the castle
            AttackTarget(enemyCastle);
            Debug.Log("Attacking castle");
        }
        else
        {
            // Move towards the castle
            MoveTowardsCastle();
            animator.SetBool("Attacking", false);
        }

        // Handle arrow animation
        HandleArrowAnimation();

        // Check if it's time to shoot
        if (animator.GetBool("Attacking") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1 >= 0.95f && !shooting)
        {
            StartCoroutine(shoot());
        }
    }

    void MoveTowardsCastle()
    {
        Vector3 direction = (enemyCastle.transform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
        
        Debug.Log($"Moving towards castle. Current position: {transform.position}, Distance to castle: {Vector3.Distance(transform.position, enemyCastle.transform.position)}");
    }

    void AttackTarget(GameObject target)
    {
        if (target == null) return;

        transform.LookAt(target.transform);
        
        if (Time.time >= nextAttackTime)
        {
            animator.SetBool("Attacking", true);
            nextAttackTime = Time.time + attackRate;
            Debug.Log($"Attacking {target.name}");
        }
    }

    void HandleArrowAnimation()
    {
        if (animationArrow == null) return;
        
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1 > 0.25f && 
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1 < 0.95f)
        {
            animationArrow.SetActive(true);
        }
        else
        {
            animationArrow.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (addArrowForce && newArrow != null && arrowSpawner != null)
        {
            GameObject target = nearbyEnemies.Length > 0 ? nearbyEnemies[0].gameObject : enemyCastle;
            if (target != null)
            {
                Vector3 targetPosition = target.transform.position;
                shootingForce = Vector3.Distance(transform.position, targetPosition);
                
                newArrow.GetComponent<Rigidbody>().AddForce(CalculateArrowForce(targetPosition));
                addArrowForce = false;
                Debug.Log($"Arrow fired at {target.name}");
            }
        }
    }

    Vector3 CalculateArrowForce(Vector3 targetPosition)
    {
        float yOffset = (targetPosition.y - transform.position.y) * 45;
        return transform.TransformDirection(new Vector3(0, shootingForce * 12 + yOffset, shootingForce * 55));
    }

    IEnumerator shoot()
    {
        shooting = true;
        newArrow = Instantiate(arrow, arrowSpawner.position, arrowSpawner.rotation) as GameObject;
        newArrow.GetComponent<Arrow>().arrowOwner = this.gameObject;
        addArrowForce = true;
        yield return new WaitForSeconds(0.5f);
        shooting = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
