using Unity.Netcode;
using UnityEngine;

/// <summary>디아 오브 클라이언트 비주얼 — CFX3 얼음 구슬·지면 자국·명중 이펙트.</summary>
public class CwslFrozenOrbVisual : NetworkBehaviour
{
    private Transform visualRoot;
    private CwslFrozenOrbProjectile projectile;
    private bool builtAsShard;
    private float nextGroundTrailTime;
    private Vector3 lastGroundTrailPos;
    private bool hasGroundTrailPos;

    public override void OnNetworkSpawn()
    {
        projectile = GetComponent<CwslFrozenOrbProjectile>();
        if (projectile != null)
            projectile.OnVisualKindChanged += HandleVisualKindChanged;

        ResetGroundTrail();
        RebuildVisual();
    }

    public override void OnNetworkDespawn()
    {
        if (projectile != null)
            projectile.OnVisualKindChanged -= HandleVisualKindChanged;

        ClearVisual();
        projectile = null;
        ResetGroundTrail();
    }

    public void EnsureBuilt() => RebuildVisual();

    private void HandleVisualKindChanged()
    {
        ResetGroundTrail();
        RebuildVisual();
    }

    private void Update()
    {
        if (projectile == null)
            projectile = GetComponent<CwslFrozenOrbProjectile>();

        if (projectile == null)
            return;

        if (visualRoot == null || builtAsShard != projectile.IsShard)
            RebuildVisual();

        if (projectile.IsFlightActive)
            TrySpawnGroundTrail();
    }

    private void RebuildVisual()
    {
        if (projectile == null)
            projectile = GetComponent<CwslFrozenOrbProjectile>();

        if (projectile == null)
            return;

        var isShard = projectile.IsShard;
        if (visualRoot != null && builtAsShard == isShard)
            return;

        ClearVisual();
        builtAsShard = isShard;
        ResetGroundTrail();

        var scale = isShard ? Mathf.Max(0.15f, transform.localScale.x) : 1f;
        visualRoot = isShard
            ? CwslFrozenOrbVisualUtility.BuildShardVisual(transform, scale)
            : CwslFrozenOrbVisualUtility.BuildOrbVisual(transform);

        var emitter = GetComponent<CwslFrozenOrbEmitter>();
        if (!isShard && emitter != null && visualRoot != null)
            emitter.BindOrbVisual(visualRoot);

        if (!isShard)
            CwslSkillAudioFeedback.PlayFrozenOrbTravel(transform.position);
    }

    private void TrySpawnGroundTrail()
    {
        if (Time.time < nextGroundTrailTime)
            return;

        var point = transform.position;
        point.y = CwslTankShieldVfxUtil.VisualGroundY;

        if (hasGroundTrailPos)
        {
            var minDistance = CwslGameConstants.RedMageFrozenOrbGroundTrailMinDistance;
            if ((point - lastGroundTrailPos).sqrMagnitude < minDistance * minDistance)
                return;
        }

        CwslVfxSpawner.SpawnFrozenOrbGroundTrail(point);
        lastGroundTrailPos = point;
        hasGroundTrailPos = true;
        nextGroundTrailTime = Time.time + CwslGameConstants.RedMageFrozenOrbGroundTrailInterval;
    }

    private void ResetGroundTrail()
    {
        nextGroundTrailTime = 0f;
        lastGroundTrailPos = Vector3.zero;
        hasGroundTrailPos = false;
    }

    private void ClearVisual()
    {
        if (visualRoot != null)
        {
            ReleaseAttachedVfx(visualRoot);
            Destroy(visualRoot.gameObject);
        }

        visualRoot = null;
    }

    private static void ReleaseAttachedVfx(Transform root)
    {
        if (root == null)
            return;

        var handles = root.GetComponentsInChildren<CwslPooledVfxHandle>(true);
        for (var i = 0; i < handles.Length; i++)
            CwslVfxPool.Release(handles[i].gameObject);
    }
}
