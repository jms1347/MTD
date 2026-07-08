using Unity.Netcode;
using UnityEngine;

public class HumanGun : NetworkBehaviour
{
    [SerializeField] private float range = 18f;

    public void TryFire(Camera camera)
    {
        if (!base.IsOwner || camera == null)
            return;

        PanicAudioCue.PlayGunShot();
        var origin = camera.transform.position;
        var direction = camera.transform.forward;
        if (!Physics.Raycast(origin, direction, out var hit, range))
            return;

        var mosquito = hit.collider.GetComponentInParent<MosquitoController>();
        if (mosquito == null)
            return;

        var networkObject = mosquito.GetComponent<NetworkObject>();
        if (IsServer)
        {
            mosquito.ReceiveGunHit(PanicGameConstants.MosquitoGunDamage, OwnerClientId);
            return;
        }

        if (networkObject != null)
            ReportGunHitServerRpc(networkObject.NetworkObjectId, PanicGameConstants.MosquitoGunDamage);
    }

    [ServerRpc]
    private void ReportGunHitServerRpc(ulong mosquitoNetworkId, float damage)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(mosquitoNetworkId, out var networkObject))
            return;

        networkObject.GetComponent<MosquitoController>()?.ReceiveGunHit(damage, OwnerClientId);
    }

    public static Transform CreateMuzzle(Transform parent, Camera camera)
    {
        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "EffkillaGun";
        gun.transform.SetParent(camera.transform, false);
        gun.transform.localPosition = new Vector3(0.28f, -0.18f, 0.45f);
        gun.transform.localScale = new Vector3(0.08f, 0.08f, 0.35f);
        PanicMaterialFactory.ApplyColor(gun.GetComponent<Renderer>(), new Color(0.2f, 0.2f, 0.22f));
        Object.Destroy(gun.GetComponent<Collider>());
        return gun.transform;
    }
}
