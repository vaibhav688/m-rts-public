using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.AI;
using Fusion;

public class CharacterMultiplayer : NetworkBehaviour {

    public float lives;
    public float damage;
    public float minAttackDistance;
    public float castleStoppingDistance;
    public GameObject dieParticles;
    public string unitType;
    public GameObject healthBarFill;

    [Space(10)]
    public bool wizard;
    public ParticleSystem spawnEffect;
    public GameObject skeleton;
    public int skeletonAmount;
    public float newSkeletonsWaitTime;

    [HideInInspector]
    public string attackTag = "Untagged";
    [HideInInspector]
    public string attackCastleTag = "Untagged";

    [HideInInspector]
    public bool selected;
    [HideInInspector]
    public Transform currentTarget;
    [HideInInspector]
    public Vector3 castleAttackPosition;

    private NavMeshAgent agent;
    private GameObject[] enemies;
    private GameObject health;
    private GameObject healthbar;
    private GameObject selectedObject;
    private float defaultStoppingDistance;
    private GameObject castle;
    private bool wizardSpawns;

    public Vector3 clickedPosition;
    public Vector3 currentTargetPosition;
    public bool goingToClickedPos;
    [HideInInspector]
    public int destination;

    void Start() {
        selected = false;
        goingToClickedPos = false;
        agent = gameObject.GetComponent<NavMeshAgent>();

        health = transform.Find("Health").gameObject;
        healthbar = health.transform.Find("Healthbar").gameObject;
        selectedObject = transform.Find("selected object").gameObject;
        selectedObject.SetActive(false);

        healthbar.GetComponent<Slider>().maxValue = lives;
        defaultStoppingDistance = agent.stoppingDistance;

        if (wizard)
            StartCoroutine(spawnSkeletons());
    }

    void Update() {
        health.transform.LookAt(2 * transform.position - Camera.main.transform.position);
        healthbar.GetComponent<Slider>().value = lives;

        if (selected) {
            selectedObject.SetActive(true);
        } else {
            selectedObject.SetActive(false);
        }

        if (destination == 1 && currentTarget != null && !goingToClickedPos) {
            agent.SetDestination(currentTargetPosition);

            if (Vector3.Distance(currentTargetPosition, transform.position) <= agent.stoppingDistance) {
                Vector3 currentTargetPosition = currentTarget.position;
                currentTargetPosition.y = transform.position.y;
                transform.LookAt(currentTargetPosition);

                AnimatorSetBool("Attacking", true);

                currentTarget.gameObject.GetComponent<CharacterMultiplayer>().lives -= Time.deltaTime * damage;
            }
        } else if (destination == 2) {
            agent.SetDestination(castleAttackPosition);

            if (!wizardSpawns)
                agent.isStopped = false;

            AnimatorSetBool("Attacking", false);
        } else if (destination == 3) {
            agent.SetDestination(clickedPosition);

            AnimatorSetBool("Attacking", false);
        } else if (destination == 4) {
            if (castle != null) {
                if (Vector3.Distance(transform.position, castleAttackPosition) <= castleStoppingDistance + castle.GetComponent<Castle>().size) {
                    agent.isStopped = true;
                    AnimatorSetBool("Attacking", true);
                }

                Vector3 currentCastlePosition = castle.transform.position;
                currentCastlePosition.y = transform.position.y;
                transform.LookAt(currentCastlePosition);

                castle.GetComponent<Castle>().lives -= Time.deltaTime * damage;
            }
        }

        checkForClickedPosition();

        if (!Runner)
            return;

        enemies = GameObject.FindGameObjectsWithTag(attackTag);

        float closestDistance = Mathf.Infinity;

        foreach (GameObject potentialTarget in enemies) {
            if (Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance && potentialTarget != null) {
                closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                if (currentTarget == null) {
                    NetworkObject networkObject = potentialTarget.GetComponent<NetworkObject>();
                    if (networkObject != null) {
                        RpcSetCurrentTarget(networkObject.Id);
                    }
                }
            }
        }

        if (currentTarget != null) {
            currentTargetPosition = currentTarget.position;
        }

        if (castle == null && GameObject.FindGameObjectsWithTag(attackCastleTag) != null) {
            GameObject[] castles = GameObject.FindGameObjectsWithTag(attackCastleTag);

            GameObject lastCastle = castle;

            float closestCastle = Mathf.Infinity;

            foreach (GameObject potentialCastle in castles) {
                if (Vector3.Distance(transform.position, potentialCastle.transform.position) < closestCastle && potentialCastle != null) {
                    closestCastle = Vector3.Distance(transform.position, potentialCastle.transform.position);
                    castle = potentialCastle;
                }
            }

            if (castle != null && castle != lastCastle) {
                RpcSetClosestCastle(castle.transform.position, int.Parse(castle.name));
            }
        }

        if (!goingToClickedPos) {
            if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) < minAttackDistance && destination != 4) {
                if (destination != 1) {
                    RpcChangeDestination(1);
                }

                if (Vector3.Distance(currentTarget.position, transform.position) > agent.stoppingDistance) {
                    if (GetComponent<Animator>().GetBool("Attacking") == true) {
                        RpcSetAnimation("Attacking", false);
                    }
                }
            } else {
                if (castle != null && Vector3.Distance(transform.position, castleAttackPosition) <= castleStoppingDistance + castle.GetComponent<Castle>().size) {
                    if (destination != 4) {
                        RpcChangeDestination(4);
                    }
                } else {
                    if (destination != 2) {
                        RpcChangeDestination(2);
                    }
                }
            }
        } else {
            if (Vector3.Distance(transform.position, clickedPosition) < agent.stoppingDistance + 1 || !CharacterManagerMultiplayer.selectionMode) {
                if (agent.velocity == Vector3.zero) {
                    RpcResumeAgent();
                }
            }
        }

        if (lives < 1) {
            Vector3 position = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z);
            GameObject particles = Instantiate(dieParticles, position, transform.rotation) as GameObject;

            Runner.Spawn(particles);

            CharacterManagerMultiplayer[] managers = GameObject.FindObjectsOfType<CharacterManagerMultiplayer>();
            foreach (CharacterManagerMultiplayer manager in managers) {
                if (manager.gameObject.transform.Find("Camera").gameObject.activeSelf) {
                    manager.enemyUnitKilled(gameObject.tag, unitType);
                }
            }

            Destroy(gameObject);
        }
    }

    void checkForClickedPosition() {
        RaycastHit hit;

        if (selected && ((Input.GetMouseButtonDown(1) && !GameObject.Find("Mobile")) || (Input.GetMouseButtonDown(0) && GameObject.Find("Mobile") && Mobile.selectionModeMove)
            || (Input.GetMouseButtonDown(0) && GameObject.Find("Mobile multiplayer") && MobileMultiplayer.selectionModeMove))) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.collider.gameObject.CompareTag("Battle ground")) {
                    CharacterManagerMultiplayer.target.transform.position = hit.point;
                    CharacterManagerMultiplayer.target.SetActive(true);

                    if (Runner) {
                        goToClickedPosition(hit.point);
                    } else {
                        CmdGoToClickedPosition(hit.point);
                    }
                }
        }
    }

    void goToClickedPosition(Vector3 position) {
        goingToClickedPos = true;
        clickedPosition = position;

        RpcChangeDestination(3);

        if (GetComponent<archer>() != null) {
            agent.stoppingDistance = 2;
        }

        if (GetComponent<Animator>().GetBool("Attacking") == true) {
            RpcSetAnimation("Attacking", false);
        }
    }

    IEnumerator spawnSkeletons() {
        spawnEffect.Stop();

        while (true) {
            if (GetComponentsInChildren<Animator>()[0].GetBool("Attacking") == false && ((currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) >= minAttackDistance) || !currentTarget)) {
                wizardSpawns = true;

                agent.isStopped = true;

                AnimatorSetBool("Spawning", true);

                yield return new WaitForSeconds(0.5f);
                spawnEffect.Play();

                if (Runner) {
                    for (int i = 0; i < skeletonAmount; i++) {
                        spawnSkeleton();
                        yield return new WaitForSeconds(2f / skeletonAmount);
                    }
                }

                spawnEffect.Stop();

                yield return new WaitForSeconds(0.5f);

                wizardSpawns = false;

                AnimatorSetBool("Spawning", false);
            }

            yield return new WaitForSeconds(newSkeletonsWaitTime);
        }
    }

    public void spawnSkeleton() {
        Vector3 position = new Vector3(transform.position.x + Random.Range(1f, 2f), transform.position.y, transform.position.z + Random.Range(-0.5f, 0.5f));
        foreach (CharacterManagerMultiplayer manager in GameObject.FindObjectsOfType<CharacterManagerMultiplayer>()) {
            if (manager.Object.HasInputAuthority)
                manager.SpawnUnit(position, skeleton, true, (healthBarFill.GetComponent<Image>().color == Color.blue));
        }
    }

    void RpcSetClosestCastle(Vector3 attackPosition, int castleId) {
        castleAttackPosition = attackPosition;

        GameObject[] castles = null;

        if (gameObject.CompareTag("Player 1 unit")) {
            castles = GameObject.FindGameObjectsWithTag("Player 2 castle");
        } else if (gameObject.CompareTag("Player 2 unit")) {
            castles = GameObject.FindGameObjectsWithTag("Player 1 castle");
        }

        if (castles != null && castles.Length > 0) {
            for (int i = 0; i < castles.Length; i++) {
                if (castles[i].name == castleId.ToString()) {
                    castle = castles[i];
                }
            }
        }
    }

    void RpcSetCurrentTarget(NetworkId netID) {
        NetworkObject netObj = Runner.FindObject(netID);
        if (netObj != null) {
            currentTarget = netObj.transform;
        }
    }

    void RpcChangeDestination(int newDestination) {
        if (newDestination != 0) {
            destination = newDestination;
        }
        if (agent != null) {
            agent.isStopped = false;
        }
    }

    void RpcSetAnimation(string animation, bool state) {
        if (animation != "") {
            foreach (Animator animator in GetComponentsInChildren<Animator>()) {
                animator.SetBool(animation, state);
            }
        }
    }

    void RpcResumeAgent() {
        goingToClickedPos = false;
        agent.stoppingDistance = defaultStoppingDistance;

        if (agent != null) {
            agent.isStopped = false;
        }
    }

    // tell the server that this character must move towards the clicked position
    void CmdGoToClickedPosition(Vector3 position) {
        goToClickedPosition(position);
    }

    void AnimatorSetBool(string animation, bool state) {
        foreach (Animator animator in GetComponentsInChildren<Animator>()) {
            animator.SetBool(animation, state);
        }
    }
}
