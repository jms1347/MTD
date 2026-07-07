using Unity.Netcode;
using UnityEngine;

/// <summary>디아 오브 클라이언트 비주얼 — 본체/파편 얼음 구체.</summary>
public class CwslFrozenOrbVisual : NetworkBehaviour
{
    private Transform visualRoot;
    private CwslFrozenOrbProjectile projectile;

    public override void OnNetworkSpawn()
    {
        projectile = GetComponent<CwslFrozenOrbProjectile>();
        EnsureBuilt();
    }

    public override void OnNetworkDespawn()
    {
        ClearVisual();
        projectile = null;
    }

    public void EnsureBuilt()
    {
        if (projectile == null)
            projectile = GetComponent<CwslFrozenOrbProjectile>();

        if (projectile == null || visualRoot != null)
            return;

        var isShard = projectile.IsShard;
        var scale = isShard ? Mathf.Max(0.15f, transform.localScale.x) : 1f;

        visualRoot = isShard
            ? CwslFrozenOrbVisualUtility.BuildShardVisual(transform, scale)
            : CwslFrozenOrbVisualUtility.BuildOrbVisual(transform);

        var emitter = GetComponent<CwslFrozenOrbEmitter>();
        if (!isShard && emitter != null && visualRoot != null)
            emitter.BindOrbVisual(visualRoot);
    }

    private void Update()
    {
        if (visualRoot == null)
            EnsureBuilt();
    }

    private void ClearVisual()
    {
        if (visualRoot != null)
            Destroy(visualRoot.gameObject);

        visualRoot = null;
    }
}
