using System.Collections;
using UnityEngine;

/// <summary>
/// 메테오 타워 본체가 파괴된 뒤, 마커가 멈춘 위치에 쿨마다 메테오를 떨어뜨립니다.
/// </summary>
public class MeteorBeaconController : MonoBehaviour
{
    private Vector3 strikeAnchor;
    private Vector3 referenceTowerPosition;
    private GameObject marker;
    private GameObject meteorProjectilePrefab;
    private GameObject meteorExplosionPrefab;
    private GameObject activeWarningZone;

    private float strikeCooldown;
    private float strikeWarningDelay;
    private float strikeDamage;
    private float impactRadius;
    private float meteorDropHeight;
    private float meteorFallSpeed;
    private float meteorDiagonalSpread;
    private float explosionVisualScalePerRadius;
    private Color warningZoneColor;

    private float nextStrikeTime;
    private bool isStriking;

    private const float GroundY = 0.05f;

    public void Activate(
        Vector3 anchor,
        Vector3 towerPosition,
        GameObject persistentMarker,
        GameObject projectilePrefab,
        GameObject explosionPrefab,
        float cooldown,
        float warningDelay,
        float damage,
        float radius,
        float dropHeight,
        float fallSpeed,
        float diagonalSpread,
        float explosionScalePerRadius,
        Color warningColor)
    {
        strikeAnchor = new Vector3(anchor.x, GroundY, anchor.z);
        referenceTowerPosition = towerPosition;
        marker = persistentMarker;
        meteorProjectilePrefab = projectilePrefab;
        meteorExplosionPrefab = explosionPrefab;
        strikeCooldown = cooldown;
        strikeWarningDelay = warningDelay;
        strikeDamage = damage;
        impactRadius = radius;
        meteorDropHeight = dropHeight;
        meteorFallSpeed = fallSpeed;
        meteorDiagonalSpread = diagonalSpread;
        explosionVisualScalePerRadius = explosionScalePerRadius;
        warningZoneColor = warningColor;

        transform.position = strikeAnchor;
        nextStrikeTime = Time.time + strikeCooldown * 0.35f;
    }

    private void OnDestroy()
    {
        ClearWarningZone();
        if (marker != null)
            Destroy(marker);
    }

    private void Update()
    {
        if (isStriking || Time.time < nextStrikeTime)
            return;

        StartCoroutine(StrikeRoutine());
        nextStrikeTime = Time.time + strikeCooldown;
    }

    private IEnumerator StrikeRoutine()
    {
        isStriking = true;

        ClearWarningZone();
        activeWarningZone = CreateWarningZone(strikeAnchor);

        float warningElapsed = 0f;
        while (warningElapsed < strikeWarningDelay)
        {
            warningElapsed += Time.deltaTime;
            PulseWarningZone(activeWarningZone, warningElapsed / strikeWarningDelay);
            yield return null;
        }

        SpawnFallingMeteor(strikeAnchor);
        ClearWarningZone();

        float timeout = meteorDropHeight / Mathf.Max(meteorFallSpeed, 1f) + 2f;
        yield return new WaitForSeconds(timeout);

        isStriking = false;
    }

    private void SpawnFallingMeteor(Vector3 strikeGround)
    {
        if (meteorProjectilePrefab == null)
        {
            MeteorStrikeProjectile.ExecuteInstantImpact(
                strikeGround,
                strikeDamage,
                impactRadius,
                meteorExplosionPrefab,
                null,
                explosionVisualScalePerRadius);
            return;
        }

        Vector3 spawnPosition = strikeGround + GetDiagonalSpawnOffset(strikeGround);
        Vector3 fallDirection = (strikeGround - spawnPosition).normalized;
        var meteor = Instantiate(meteorProjectilePrefab, spawnPosition, Quaternion.LookRotation(fallDirection));

        var projectile = meteor.GetComponent<MeteorStrikeProjectile>();
        if (projectile == null)
            projectile = meteor.AddComponent<MeteorStrikeProjectile>();

        projectile.Launch(
            fallDirection * meteorFallSpeed,
            strikeDamage,
            impactRadius,
            meteorExplosionPrefab,
            explosionVisualScalePerRadius);
    }

    private Vector3 GetDiagonalSpawnOffset(Vector3 strikeGround)
    {
        Vector3 awayFromTower = strikeGround - referenceTowerPosition;
        awayFromTower.y = 0f;
        if (awayFromTower.sqrMagnitude < 0.01f)
            awayFromTower = Vector3.forward;

        awayFromTower.Normalize();
        return awayFromTower * (meteorDropHeight * meteorDiagonalSpread) + Vector3.up * meteorDropHeight;
    }

    private void ClearWarningZone()
    {
        if (activeWarningZone == null)
            return;

        var renderer = activeWarningZone.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
            Destroy(renderer.material);

        Destroy(activeWarningZone);
        activeWarningZone = null;
    }

    private GameObject CreateWarningZone(Vector3 groundPoint)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = "MeteorWarningZone";
        zone.transform.position = groundPoint + Vector3.up * 0.04f;
        zone.transform.localScale = new Vector3(impactRadius * 2f, 0.04f, impactRadius * 2f);

        var collider = zone.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = zone.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateTransparentMaterial(warningZoneColor);

        return zone;
    }

    private static void PulseWarningZone(GameObject zone, float normalizedTime)
    {
        if (zone == null)
            return;

        var renderer = zone.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null)
            return;

        float pulse = 0.55f + Mathf.Sin(normalizedTime * Mathf.PI * 8f) * 0.25f;
        var color = renderer.material.color;
        color.a = Mathf.Clamp01(0.45f * pulse);
        renderer.material.color = color;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        return material;
    }
}
