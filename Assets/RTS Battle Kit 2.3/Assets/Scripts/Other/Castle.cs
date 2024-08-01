using UnityEngine;
using Fusion;
using System.Collections;

public class Castle : NetworkBehaviour
{
    [Networked] public float lives { get; set; }
    [Networked] public PlayerRef Owner { get; set; }
    public float size;
    public GameObject fracture;

    public void Initialize(PlayerRef owner)
    {
        Owner = owner;
        lives = 100f; // Set initial lives or any other initialization logic here
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            lives = 100f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (lives <= 0f)
        {
            lives = 0;
            if (Object.HasStateAuthority)
            {
                RPC_DestroyCastle();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DestroyCastle()
    {
        if (fracture && gameObject.name != "Castle gate" && gameObject.name != "1")
        {
            Runner.Spawn(fracture, transform.position, Quaternion.Euler(0, transform.eulerAngles.y, 0));
        }
        else if (fracture)
        {
            Runner.Spawn(fracture, transform.position, Quaternion.Euler(0, 0, 0));
        }

        StartCoroutine(DelayCastleDestruction());
    }

    public IEnumerator DelayCastleDestruction()
    {
        yield return new WaitForSeconds(0.1f);

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
            GameManager.Instance.OnCastleDestroyed(this);
        }
    }

    public void TakeDamage(float damage)
    {
        if (Object.HasStateAuthority)
        {
            lives -= damage;
        }
        else
        {
            RPC_TakeDamage(damage);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        lives -= damage;
    }
}
