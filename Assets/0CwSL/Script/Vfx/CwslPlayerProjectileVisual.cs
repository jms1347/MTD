using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectileVisual : NetworkBehaviour
{
    private const float VisualScale = 1.15f;
    private const float FatBulletScale = 1.25f;
    private const float RetrySeconds = 2f;
    private static readonly Quaternion EtfxForwardFix = Quaternion.Euler(90f, 0f, 0f);

    private GameObject missileVisual;
    private float retryUntil;
    private byte attachedKind = byte.MaxValue;

    public override void OnNetworkSpawn()
    {
        retryUntil = Time.time + RetrySeconds;
        AttachProjectileVisual();
    }

    public override void OnNetworkDespawn()
    {
        ClearProjectileVisual();
    }

    private void Update()
    {
        var projectile = GetComponent<CwslPlayerProjectile>();
        var kind = projectile != null ? projectile.VisualKind : (byte)0;
        if (missileVisual != null && attachedKind == kind)
            return;

        if (missileVisual == null && Time.time <= retryUntil)
            AttachProjectileVisual();
        else if (missileVisual != null && attachedKind != kind)
            AttachProjectileVisual();
    }

    private void AttachProjectileVisual()
    {
        var projectile = GetComponent<CwslPlayerProjectile>();
        var kind = projectile != null ? projectile.VisualKind : (byte)0;
        ClearProjectileVisual();
        attachedKind = kind;

        var prefab = ResolveVisualPrefab(kind);
        if (prefab != null)
        {
            missileVisual = CwslVfxSpawner.TryInstantiate(prefab, transform.position, transform.rotation);
            if (missileVisual != null)
            {
                DisablePhysicsOnly(missileVisual);
                missileVisual.transform.SetParent(transform, false);
                missileVisual.transform.localPosition = Vector3.zero;
                missileVisual.transform.localRotation = kind == 4 ? Quaternion.identity : EtfxForwardFix;
                missileVisual.transform.localScale = Vector3.one * (kind == 4 ? FatBulletScale : VisualScale);
                return;
            }
        }

        missileVisual = BuildFallbackBulletVisual(kind);
    }

    private static GameObject ResolveVisualPrefab(byte kind)
    {
        var assets = CwslGameSession.Instance?.Assets;
        if (assets == null)
            return null;

        return kind switch
        {
            1 => assets.missileTankFireAmmoVfx ?? assets.playerMissileVfx,
            2 => assets.missileTankPoisonAmmoVfx ?? assets.playerMissileVfx,
            3 => assets.missileTankLightningAmmoVfx ?? assets.playerMissileVfx,
            4 => assets.missileTankSmokeBombVfx ?? assets.playerMissileVfx,
            _ => assets.playerMissileVfx,
        };
    }

    private GameObject BuildFallbackBulletVisual(byte kind)
    {
        var root = new GameObject("FallbackBullet");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        var bullet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bullet.transform.SetParent(root.transform, false);
        bullet.transform.localPosition = new Vector3(0f, 0f, 0.08f);
        bullet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bullet.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
        Object.Destroy(bullet.GetComponent<Collider>());
        var color = kind switch
        {
            1 => new Color(1f, 0.45f, 0.12f),
            2 => new Color(0.35f, 0.95f, 0.25f),
            3 => new Color(1f, 0.92f, 0.2f),
            4 => new Color(0.45f, 0.95f, 0.35f),
            _ => new Color(0.55f, 0.72f, 0.95f),
        };
        CwslMaterialUtil.ApplyColor(bullet.GetComponent<Renderer>(), color);

        root.transform.localScale = Vector3.one * VisualScale;
        return root;
    }

    private static void DisablePhysicsOnly(GameObject visualRoot)
    {
        foreach (var collider in visualRoot.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (var rigidbody in visualRoot.GetComponentsInChildren<Rigidbody>(true))
            rigidbody.isKinematic = true;
    }

    private void ClearProjectileVisual()
    {
        if (missileVisual != null)
            CwslVfxPool.Release(missileVisual);
        missileVisual = null;
    }
}
