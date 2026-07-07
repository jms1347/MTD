using Unity.Netcode;
using UnityEngine;

public class CwslProjectileVisual : NetworkBehaviour
{
    private const float VisualScale = 1.35f;
    private const float TankBulletScale = 1.1f;
    private const float RetrySeconds = 2f;

    private static readonly Quaternion EtfxForwardFix = Quaternion.Euler(90f, 0f, 0f);

    private GameObject missileVisual;
    private float retryUntil;
    private CwslMonsterProjectileKind attachedKind = (CwslMonsterProjectileKind)255;

    public override void OnNetworkSpawn()
    {
        retryUntil = Time.time + RetrySeconds;
        AttachProjectileVisual();
    }

    public override void OnNetworkDespawn()
    {
        ClearProjectileVisual();
    }

    public void RefreshVisual()
    {
        attachedKind = (CwslMonsterProjectileKind)255;
        AttachProjectileVisual();
    }

    private void Update()
    {
        if (missileVisual != null || Time.time > retryUntil)
            return;

        AttachProjectileVisual();
    }

    private void AttachProjectileVisual()
    {
        var projectile = GetComponent<CwslMonsterProjectile>();
        if (projectile == null)
            return;

        var kind = projectile.ProjectileKind;
        if (missileVisual != null && attachedKind == kind)
            return;

        ClearProjectileVisual();
        attachedKind = kind;

        var prefab = ResolveVisualPrefab(kind);
        if (prefab == null)
            return;

        missileVisual = CwslVfxSpawner.TryInstantiate(prefab, transform.position, transform.rotation);
        if (missileVisual == null)
        {
            missileVisual = CreateFallbackVisual(kind);
            if (missileVisual == null)
                return;
        }
        else
        {
            DisablePhysicsOnly(missileVisual);
            missileVisual.transform.localScale = Vector3.one * (kind == CwslMonsterProjectileKind.TankBullet
                ? TankBulletScale
                : VisualScale);
        }

        missileVisual.transform.SetParent(transform, false);
        missileVisual.transform.localPosition = Vector3.zero;
        missileVisual.transform.localRotation = EtfxForwardFix;

        var lightColor = kind == CwslMonsterProjectileKind.TankBullet
            ? new Color(1f, 0.45f, 0.72f)
            : new Color(1f, 0.15f, 0.08f);
        CwslThreatLight.Ensure(transform, lightColor, 4.5f, 2.8f, Vector3.zero);
    }

    private static GameObject ResolveVisualPrefab(CwslMonsterProjectileKind kind)
    {
        var assets = CwslGameSession.Instance?.Assets ?? CwslVisualTestAssetsContext.Assets;
        if (assets == null)
            return null;

        return kind == CwslMonsterProjectileKind.TankBullet
            ? assets.rangedTankProjectileVfx ?? assets.playerMissileVfx
            : assets.darkMissileVfx;
    }

    private static GameObject CreateFallbackVisual(CwslMonsterProjectileKind kind)
    {
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "ProjectileCore";
        Object.Destroy(core.GetComponent<Collider>());
        core.transform.localScale = Vector3.one * 0.35f;
        var color = kind == CwslMonsterProjectileKind.TankBullet
            ? new Color(0.95f, 0.45f, 0.72f)
            : new Color(0.35f, 0.15f, 0.55f);
        CwslMaterialUtil.ApplyColor(core.GetComponent<Renderer>(), color);
        return core;
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
            Destroy(missileVisual);
        missileVisual = null;
    }
}
