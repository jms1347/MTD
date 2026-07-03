using Unity.Netcode;
using UnityEngine;

public class CwslProjectileVisual : NetworkBehaviour
{
    private const float VisualScale = 1.35f;
    private const float RetrySeconds = 2f;

    private GameObject missileVisual;
    private float retryUntil;

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
        if (missileVisual != null || Time.time > retryUntil)
            return;

        AttachProjectileVisual();
    }

    private void AttachProjectileVisual()
    {
        ClearProjectileVisual();

        var prefab = CwslGameSession.Instance?.Assets?.darkMissileVfx;
        if (prefab == null)
            return;

        missileVisual = CwslVfxSpawner.TryInstantiate(prefab, transform.position, transform.rotation);
        if (missileVisual == null)
        {
            missileVisual = CreateFallbackVisual();
            if (missileVisual == null)
                return;
        }
        else
        {
            DisablePhysicsOnly(missileVisual);
            missileVisual.transform.localScale = Vector3.one * VisualScale;
        }

        missileVisual.transform.SetParent(transform, false);
        missileVisual.transform.localPosition = Vector3.zero;
        missileVisual.transform.localRotation = Quaternion.identity;

        // 어둠 속에서 다가오는 미사일 빨간 불빛
        CwslThreatLight.Ensure(transform, new Color(1f, 0.15f, 0.08f), 4.5f, 2.8f, Vector3.zero);
    }

    private static GameObject CreateFallbackVisual()
    {
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "ProjectileCore";
        Object.Destroy(core.GetComponent<Collider>());
        core.transform.localScale = Vector3.one * 0.35f;
        CwslMaterialUtil.ApplyColor(core.GetComponent<Renderer>(), new Color(0.35f, 0.15f, 0.55f));
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
