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
        if (prefab != null)
        {
            missileVisual = CwslVfxSpawner.TryInstantiate(prefab, transform.position, transform.rotation);
            if (missileVisual != null)
            {
                DisablePhysicsOnly(missileVisual);
                missileVisual.transform.SetParent(transform, false);
                missileVisual.transform.localPosition = Vector3.zero;
                missileVisual.transform.localRotation = Quaternion.identity;
                missileVisual.transform.localScale = Vector3.one * VisualScale;
                return;
            }
        }

        missileVisual = BuildFallbackBulletVisual();
    }

    private GameObject BuildFallbackBulletVisual()
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
        CwslMaterialUtil.ApplyColor(bullet.GetComponent<Renderer>(), new Color(0.55f, 0.72f, 0.95f));

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
            Destroy(missileVisual);
        missileVisual = null;
    }
}
