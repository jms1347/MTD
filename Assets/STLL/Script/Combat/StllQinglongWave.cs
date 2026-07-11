using Unity.Netcode;
using UnityEngine;

/// <summary>청룡검기 — 서버 전용 투사체 (NetworkPrefab 불필요).</summary>
public class StllQinglongWave : MonoBehaviour
{
    private Vector3 direction;
    private float traveled;
    private float damage;
    private ulong attackerClientId;

    public static void Spawn(Vector3 origin, Vector3 direction, float damage, ulong attackerClientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var go = new GameObject("StllQinglongWave");
        go.transform.position = origin + direction * 0.6f + Vector3.up * 0.4f;
        go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, go.transform, Vector3.zero,
            new Vector3(StllGlaiveConstants.QinglongProjectileWidth, 0.08f, 0.9f),
            new Color(0.2f, 0.65f, 1f, 0.85f));

        var wave = go.AddComponent<StllQinglongWave>();
        wave.Init(direction, damage, attackerClientId);
    }

    private void Init(Vector3 dir, float dmg, ulong attackerId)
    {
        direction = dir.normalized;
        damage = dmg;
        attackerClientId = attackerId;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var delta = direction * (StllGlaiveConstants.QinglongProjectileSpeed * Time.deltaTime);
        transform.position += delta;
        traveled += delta.magnitude;

        var hits = Physics.OverlapSphere(transform.position, StllGlaiveConstants.QinglongProjectileWidth * 0.5f);
        for (var i = 0; i < hits.Length; i++)
        {
            var health = hits[i].GetComponentInParent<StllEnemyHealth>();
            if (health == null || !health.IsAlive)
                continue;

            health.TakeDamageServer(damage, attackerClientId, direction * 2f);
        }

        if (traveled >= StllGlaiveConstants.QinglongProjectileRange)
            Destroy(gameObject);
    }
}
