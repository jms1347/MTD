using UnityEngine;

/// <summary>화염 장판·얼음 지뢰 — 서버 로컬 이펙트.</summary>
public class StllZoneEffect : MonoBehaviour
{
    private float radius;
    private float duration;
    private float dotPerSecond;
    private ulong attackerId;
    private float timer;
    private bool isIceMine;
    private bool triggered;

    public static void SpawnFireZoneServer(Vector3 position, float radius, float duration, float dot, ulong attackerId)
    {
        var go = new GameObject("FireZone");
        go.transform.position = position;
        var zone = go.AddComponent<StllZoneEffect>();
        zone.Configure(radius, duration, dot, attackerId, false);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, go.transform, Vector3.zero,
            new Vector3(radius * 0.5f, 0.05f, radius * 0.5f), new Color(0.95f, 0.35f, 0.1f));
    }

    public static void SpawnIceMineServer(Vector3 position, ulong attackerId)
    {
        var go = new GameObject("IceMine");
        go.transform.position = position;
        var zone = go.AddComponent<StllZoneEffect>();
        zone.Configure(2f, 30f, 50f, attackerId, true);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Sphere, go.transform, Vector3.up * 0.2f,
            Vector3.one * 0.5f, new Color(0.5f, 0.8f, 1f));
    }

    private void Configure(float zoneRadius, float zoneDuration, float damage, ulong attacker, bool iceMine)
    {
        radius = zoneRadius;
        duration = zoneDuration;
        dotPerSecond = damage;
        attackerId = attacker;
        isIceMine = iceMine;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        if (isIceMine)
        {
            if (triggered)
                return;

            var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if (Vector3.Distance(transform.position, enemy.transform.position) > radius)
                    continue;

                enemy.TakeDamageServer(dotPerSecond, attackerId, Vector3.zero);
                triggered = true;
                Destroy(gameObject);
                return;
            }

            return;
        }

        if (Mathf.FloorToInt(timer) != Mathf.FloorToInt(timer - Time.deltaTime))
        {
            var enemies = FindObjectsByType<StllEnemyHealth>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if (Vector3.Distance(transform.position, enemy.transform.position) > radius)
                    continue;

                enemy.TakeDamageServer(dotPerSecond, attackerId, Vector3.zero);
            }
        }
    }
}
