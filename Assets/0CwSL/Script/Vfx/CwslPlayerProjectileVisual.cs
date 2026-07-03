using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectileVisual : NetworkBehaviour
{
    private const float VisualScale = 1.15f;
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

        var prefab = CwslGameSession.Instance?.Assets?.playerMissileVfx;
        if (prefab == null)
            return;

        missileVisual = CwslVfxSpawner.TryInstantiate(prefab, transform.position, transform.rotation);
        if (missileVisual == null)
            return;

        DisablePhysicsOnly(missileVisual);
        missileVisual.transform.SetParent(transform, false);
        missileVisual.transform.localPosition = Vector3.zero;
        missileVisual.transform.localRotation = Quaternion.identity;
        missileVisual.transform.localScale = Vector3.one * VisualScale;
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
