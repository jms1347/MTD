using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CwslMeteorEffectRunner : MonoBehaviour
{
    private const float EffectScaleMultiplier = 0.8f;
    private const float GroundFireScale = 0.42f;

    // ETFX 미사일은 보통 local +Z가 진행 방향. 낙하 시 코가 아래를 향하도록 함.
    private static readonly Quaternion FallRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
    // 지상 폭발은 Y-up (옆으로 눕지 않게)
    private static readonly Quaternion ImpactRotation = Quaternion.identity;

    public void Play(Vector3 impactPoint, float fallHeight, float fallDuration, float burnLifetime, float areaRadius)
    {
        StartCoroutine(Run(impactPoint, fallHeight, fallDuration, burnLifetime, Mathf.Max(1f, areaRadius)));
    }

    private IEnumerator Run(
        Vector3 impactPoint,
        float fallHeight,
        float fallDuration,
        float burnLifetime,
        float areaRadius)
    {
        var assets = CwslGameSession.Instance?.Assets;
        var start = impactPoint + Vector3.up * fallHeight;

        var fallScale = areaRadius * 0.55f * EffectScaleMultiplier;
        var impactScale = areaRadius * 0.95f * EffectScaleMultiplier;

        var fallVisual = SpawnVisual(assets?.meteorFallVfx, start, FallRotation, fallScale);
        if (fallVisual == null)
            fallVisual = CreateFallbackMeteor(start, fallScale);

        var elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / fallDuration);
            var eased = t * t;
            if (fallVisual != null)
            {
                fallVisual.transform.position = Vector3.Lerp(start, impactPoint + Vector3.up * 0.35f, eased);
                fallVisual.transform.rotation = FallRotation;
            }

            yield return null;
        }

        if (fallVisual != null)
            CwslVfxPool.Release(fallVisual);

        var impact = SpawnVisual(assets?.meteorImpactVfx, impactPoint, ImpactRotation, impactScale);
        if (impact == null)
        {
            CwslSimpleVfx.SpawnBurst(impactPoint, new Color(1f, 0.35f, 0.08f), areaRadius * 0.9f * EffectScaleMultiplier, 0.7f);
        }
        else
        {
            CwslVfxPool.ScheduleRelease(impact, 5f);
        }

        SpawnRandomGroundFires(impactPoint, areaRadius, assets);

        Destroy(gameObject, CwslGameConstants.MeteorGroundFireLifetimeMax + 0.15f);
    }

    private static void SpawnRandomGroundFires(Vector3 center, float radius, CwslGameAssets assets)
    {
        var prefabs = CollectGroundFirePrefabs(assets);
        if (prefabs.Count == 0)
            return;

        var count = Random.Range(
            CwslGameConstants.MeteorGroundFirePatchCountMin,
            CwslGameConstants.MeteorGroundFirePatchCountMax + 1);

        for (var i = 0; i < count; i++)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var dist = Random.Range(radius * 0.08f, radius * 0.88f);
            var position = center + new Vector3(Mathf.Cos(angle) * dist, 0.03f, Mathf.Sin(angle) * dist);
            var prefab = prefabs[Random.Range(0, prefabs.Count)];
            var lifetime = Random.Range(
                CwslGameConstants.MeteorGroundFireLifetimeMin,
                CwslGameConstants.MeteorGroundFireLifetimeMax);
            var scale = Random.Range(0.75f, 1.15f) * GroundFireScale;
            CwslVfxSpawner.Spawn(prefab, position, ImpactRotation, lifetime, scale);
        }
    }

    private static List<GameObject> CollectGroundFirePrefabs(CwslGameAssets assets)
    {
        var list = new List<GameObject>(3);
        if (assets == null)
            return list;

        if (assets.meteorGroundFireSoftAbVfx != null)
            list.Add(assets.meteorGroundFireSoftAbVfx);
        if (assets.meteorGroundFireSoftBigVfx != null)
            list.Add(assets.meteorGroundFireSoftBigVfx);
        if (assets.meteorGroundFireAdditiveVfx != null)
            list.Add(assets.meteorGroundFireAdditiveVfx);
        return list;
    }

    private static GameObject SpawnVisual(GameObject prefab, Vector3 position, Quaternion rotation, float scale)
    {
        if (prefab == null)
            return null;

        var instance = CwslVfxSpawner.TryInstantiate(prefab, position, rotation);
        if (instance == null)
            return null;

        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.one * scale;

        foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        return instance;
    }

    private static GameObject CreateFallbackMeteor(Vector3 position, float scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MeteorFallback";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * Mathf.Max(1.2f, scale);
        Object.Destroy(go.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(go.GetComponent<Renderer>(), new Color(1f, 0.35f, 0.05f));
        CwslThreatLight.Ensure(go.transform, new Color(1f, 0.25f, 0.05f), 8f, 5f, Vector3.zero);
        return go;
    }
}
