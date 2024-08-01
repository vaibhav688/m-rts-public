using UnityEngine;

public class Arrow : MonoBehaviour
{
    [HideInInspector]
    public GameObject arrowOwner;

    private string archerTag;
    public float damage = 10f;

    void Start()
    {
        if (arrowOwner != null)
        {
            archerTag = arrowOwner.tag;
        }

        Destroy(gameObject, 5);
    }

    void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.tag == "Enemy" || other.gameObject.tag == "Knight") && other.gameObject.tag != archerTag)
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().isKinematic = true;
            transform.parent = other.gameObject.transform;

            Castle castle = other.gameObject.GetComponent<Castle>();
            if (castle != null)
            {
                castle.lives -= damage;
            }
        }
        else if (other.gameObject.tag == "Battle ground")
        {
            Destroy(gameObject);
        }
    }
}