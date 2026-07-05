using System.Collections;
using UnityEngine;

public class CwslMeteorEffectRunner : MonoBehaviour
{
    private const float EffectScaleMultiplier = 0.8f;

    // ETFX 미사일은 보통 local +Z가 진행 방향. 낙하 시 코가 아래를 향하도록 함.
    private static readonly Quaternion FallRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
    // 지상 폭발은 Y-up (옆으로 눕지 않게)
    private static readonly Quaternion ImpactRotation = Quaternion.identity;
    // 그을림 데칼은 바닥에 붙도록 X축 90도
    private static readonly Quaternion BurnRotation = Quaternion.Euler(90f, 0f, 0f);

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

        // 영역 직경에 맞춰 스케일 (데미지 반경만큼 크게)
        var fallScale = areaRadius * 0.55f * EffectScaleMultiplier;
        var impactScale = areaRadius * 0.95f * EffectScaleMultiplier;
        var burnScale = areaRadius * 2.15f * EffectScaleMultiplier;

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
            Destroy(fallVisual);

        var impact = SpawnVisual(assets?.meteorImpactVfx, impactPoint, ImpactRotation, impactScale);
        if (impact == null)
        {
            CwslSimpleVfx.SpawnBurst(impactPoint, new Color(1f, 0.35f, 0.08f), areaRadius * 0.9f * EffectScaleMultiplier, 0.7f);
        }
        else
        {
            Destroy(impact, 5f);
        }

        var burn = SpawnVisual(
            assets?.meteorBurnVfx,
            impactPoint + Vector3.up * 0.03f,
            BurnRotation,
            burnScale);
        if (burn == null)
            burn = CreateFallbackBurn(impactPoint, areaRadius);
        else
            Destroy(burn, burnLifetime);

        if (burn != null)
            Destroy(burn, burnLifetime);

        Destroy(gameObject, burnLifetime + 0.1f);
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

        // 파티클이 스케일을 따라가도록
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

    private static GameObject CreateFallbackBurn(Vector3 position, float areaRadius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "BurnFallback";
        go.transform.position = position + Vector3.up * 0.03f;
        go.transform.localScale = new Vector3(areaRadius * 2f * EffectScaleMultiplier, 0.03f, areaRadius * 2f * EffectScaleMultiplier);
        Object.Destroy(go.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(go.GetComponent<Renderer>(), new Color(0.22f, 0.07f, 0.03f));
        return go;
    }
}
