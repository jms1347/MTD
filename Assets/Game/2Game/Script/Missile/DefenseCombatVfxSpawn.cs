using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ETFX 노바·폭발 이펙트 스폰. 프리팹마다 루트/자식 회전이 달라 중복 회전을 피합니다.
/// </summary>
public static class DefenseCombatVfxSpawn
{
    public const float DefaultGroundYOffset = 0.12f;
    public const string DefaultScorchKey = "ExplosionDecalPink";
    public const string DefaultFireScorchKey = "ExplosionDecalFire";
    public const string DefaultLightningScorchKey = "ExplosionDecalBlue";
    public const float DefaultScorchLifetime = 2.5f;
    public const float DefaultFireGroundLifetime = 2f;
    public const float DefaultLightningScorchLifetime = 1.8f;
    public const float DefaultLightningScorchRadius = 0.85f;

    public static readonly Quaternion GroundNovaRotation = Quaternion.Euler(-90f, 0f, 0f);

    private static readonly Dictionary<string, string> EditorFallbackPaths = new(System.StringComparer.OrdinalIgnoreCase)
    {
        [DefaultScorchKey] = "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalPink.prefab",
        [DefaultFireScorchKey] = "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalFire.prefab",
        [DefaultLightningScorchKey] =
            "Assets/Epic Toon FX/Prefabs/Combat/Decals/Explosion Decal/ExplosionDecalBlue.prefab",
        ["GasExplosionFire"] = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/GasExplosion/GasExplosionFire.prefab",
        ["LavaBoiling"] = "Assets/Epic Toon FX/Prefabs/Environment/Water/Boiling/LavaBoiling.prefab",
        ["NovaFrost"] = "Assets/Epic Toon FX/Prefabs/Combat/Nova/Frost/NovaFrost.prefab",
        ["ExplosionNovaBlue"] = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NovaExplosion/ExplosionNovaBlue.prefab",
        ["PoisonExplosion"] = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/- Misc/PoisonExplosion.prefab",
        ["NukeExplosionFire"] =
            "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionFire.prefab",
        ["FlamethrowerCartoonyGreen"] =
            "Assets/Epic Toon FX/Prefabs/Combat/Flamethrower/Cartoon/FlamethrowerCartoonyGreen.prefab",
        ["CFXM3_Snow_Storm"] =
            "Assets/JMO Assets/Cartoon FX/CFX3 Prefabs (Mobile)/Environment/CFXM3_Snow_Storm.prefab",
    };

    public static Vector3 SnapToGround(Vector3 worldPoint, float yOffset = DefaultGroundYOffset)
    {
        return new Vector3(worldPoint.x, yOffset, worldPoint.z);
    }

    public static bool TrySpawnGroundBurst(
        string key,
        Vector3 worldPoint,
        float lifetime = 2f,
        float scaleMultiplier = 1f)
    {
        if (!TryLoadBurstPrefab(key, out var prefab) || prefab == null)
            return false;

        SpawnGroundBurst(prefab, worldPoint, lifetime, scaleMultiplier);
        return true;
    }

    public const string BlizzardStormVfxKey = "NovaFrost";
    private static readonly string[] BlizzardActiveLayers = { "SnowFlakes", "SmokeNova" };

    /// <summary>
    /// ETFX NovaFrost 레이어(눈송이·안개)만 사용 — CFX 원형 빌보드 대신 입체감 있는 눈보라.
    /// </summary>
    public static bool TrySpawnBlizzardStorm(
        Vector3 groundPoint,
        float splashRadius,
        float lifetime,
        Transform parent = null,
        float airBurstHeight = 0f)
    {
        if (!TryLoadBurstPrefab(BlizzardStormVfxKey, out var prefab) || prefab == null)
            return false;

        var fx = parent != null
            ? SpawnBlizzardStormAttached(prefab, parent)
            : SpawnBlizzardStormWorld(prefab, groundPoint);

        if (fx == null)
            return false;

        float radius = splashRadius > 0.05f ? splashRadius : 4f;
        float burstHeight = airBurstHeight > 0.05f
            ? airBurstHeight
            : Mathf.Clamp(radius * 0.5f, 1.5f, 3.2f);
        fx.transform.localScale = Vector3.one;

        FilterBlizzardLayers(fx.transform);
        DisableNovaUtilityScripts(fx);
        ConfigureEtfxBlizzardLayers(fx, radius, burstHeight);
        EnsureVisualOnly(fx);
        RestartParticleHierarchy(fx);
        Object.Destroy(fx, lifetime);
        return true;
    }

    private static GameObject SpawnBlizzardStormAttached(GameObject prefab, Transform parent)
    {
        var fx = Object.Instantiate(prefab, parent);
        fx.name = "SnowStormVisual";
        fx.transform.localPosition = Vector3.zero;
        // NovaFrost 루트는 지면용 -90° X 회전이 baked 되어 있어 그대로 쓰면
        // shape Y 오프셋(폭발 높이)이 월드 Z로 밀려 파란 범위와 어긋납니다.
        fx.transform.localRotation = Quaternion.identity;
        return fx;
    }

    private static GameObject SpawnBlizzardStormWorld(GameObject prefab, Vector3 groundPoint)
    {
        var ground = SnapToGround(groundPoint);
        var fx = Object.Instantiate(prefab, ground, Quaternion.identity);
        fx.name = "SnowStormVisual";
        return fx;
    }

    private static void FilterBlizzardLayers(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            bool keep = false;
            for (int j = 0; j < BlizzardActiveLayers.Length; j++)
            {
                if (child.name != BlizzardActiveLayers[j])
                    continue;

                keep = true;
                break;
            }

            if (!keep)
                child.gameObject.SetActive(false);
        }
    }

    private static void DisableNovaUtilityScripts(GameObject root)
    {
        var behaviours = root.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.StartsWith("ETFX") || typeName.Contains("Light") || typeName.Contains("Destroy"))
                behaviour.enabled = false;
        }
    }

    private static void ConfigureEtfxBlizzardLayers(GameObject root, float radius, float burstHeight)
    {
        float radiusT = Mathf.Clamp01(radius / 6f);

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null || !ps.gameObject.activeInHierarchy)
                continue;

            var main = ps.main;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = true;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.maxParticleSize = 1.15f;
                TryDisableSmallMeshCulling(renderer);
            }

            if (ps.gameObject.name == "SnowFlakes")
                ConfigureBlizzardRadialBurst(ps, radius, radiusT, burstHeight, renderer);
            else if (ps.gameObject.name == "SmokeNova")
                ConfigureBlizzardGroundMist(ps, radius, radiusT, renderer);
        }
    }

    /// <summary>폭발 높이에 splash 반경만큼 펼쳐진 우산형 돔에서 범위 전체로 떨어지는 눈송이.</summary>
    private static void ConfigureBlizzardRadialBurst(
        ParticleSystem ps,
        float radius,
        float radiusT,
        float burstHeight,
        ParticleSystemRenderer renderer)
    {
        var main = ps.main;
        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(52f, 105f, radiusT);
        main.maxParticles = 4000;
        main.startSizeMultiplier = Mathf.Clamp(radius * 0.13f, 0.32f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Clamp(radius * 0.22f, 1.2f, 2.8f),
            Mathf.Clamp(radius * 0.42f, 2f, 4.2f));
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Clamp(burstHeight / 2.8f + 0.5f, 1f, 1.7f),
            Mathf.Clamp(radius / 2.1f + 0.4f, 1.4f, 2.5f));
        main.gravityModifier = 1.65f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = radius * 0.97f;
        shape.radiusThickness = 0.22f;
        shape.scale = Vector3.one;
        shape.position = new Vector3(0f, burstHeight, 0f);
        shape.rotation = new Vector3(180f, 0f, 0f);
        shape.sphericalDirectionAmount = 0f;
        shape.randomDirectionAmount = 0.14f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // X/Y/Z 커브 모드가 모두 같아야 Unity 파티클 에러가 나지 않습니다.
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, -3.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotation.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotation.z = new ParticleSystem.MinMaxCurve(-140f, 140f);

        if (renderer != null)
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    /// <summary>파란 범위 원 안 지면 안개.</summary>
    private static void ConfigureBlizzardGroundMist(
        ParticleSystem ps,
        float radius,
        float radiusT,
        ParticleSystemRenderer renderer)
    {
        var main = ps.main;
        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(2.5f, 7f, radiusT);
        main.startSizeMultiplier = Mathf.Clamp(radius * 0.1f, 0.25f, 0.5f);
        main.startSpeedMultiplier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.94f;
        shape.scale = Vector3.one;
        shape.position = new Vector3(0f, 0.08f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        if (renderer != null)
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void TryDisableSmallMeshCulling(ParticleSystemRenderer renderer)
    {
        var property = typeof(ParticleSystemRenderer).GetProperty("smallMeshCulling");
        if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
            return;

        property.SetValue(renderer, false);
    }

    public static GameObject SpawnGroundBurst(
        GameObject prefab,
        Vector3 worldPoint,
        float lifetime = 2f,
        float scaleMultiplier = 1f)
    {
        if (prefab == null)
            return null;

        var fx = Object.Instantiate(
            prefab,
            SnapToGround(worldPoint),
            ResolveGroundBurstRotation(prefab));
        if (scaleMultiplier > 0f && !Mathf.Approximately(scaleMultiplier, 1f))
            fx.transform.localScale = prefab.transform.localScale * scaleMultiplier;

        EnsureVisualOnly(fx);
        RestartParticleHierarchy(fx);
        Object.Destroy(fx, lifetime);
        return fx;
    }

    public static bool TrySpawnGroundScorch(Vector3 worldPoint, float splashRadius, float lifetime = DefaultScorchLifetime)
    {
        return TrySpawnGroundDecal(DefaultScorchKey, worldPoint, splashRadius, lifetime);
    }

    public static bool TrySpawnGroundFireMark(Vector3 worldPoint, float splashRadius, float lifetime = DefaultFireGroundLifetime)
    {
        if (TrySpawnGroundDecal(DefaultFireScorchKey, worldPoint, splashRadius, lifetime))
            return true;

        float fallbackScale = Mathf.Clamp(splashRadius * 0.22f, 0.55f, 1.15f);
        return TrySpawnGroundBurst("GasExplosionFire", worldPoint, lifetime, fallbackScale);
    }

    public static bool TrySpawnLightningStrikeScorch(
        Vector3 strikePoint,
        float radius = DefaultLightningScorchRadius,
        float lifetime = DefaultLightningScorchLifetime)
    {
        return TrySpawnGroundDecal(DefaultLightningScorchKey, strikePoint, radius, lifetime);
    }

    public static GameObject TryAttachGroundZoneLoop(Transform parent, string key, float lifetime)
    {
        if (parent == null || !TryLoadBurstPrefab(key, out var prefab) || prefab == null)
            return null;

        var fx = Object.Instantiate(prefab, parent);
        fx.name = "ZoneVisual";
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = ResolveGroundLoopRotation(key, prefab);
        fx.transform.localScale = Vector3.one;

        RestartParticleHierarchy(fx);
        EnsureVisualOnly(fx);

        if (lifetime > 0f)
            Object.Destroy(fx, lifetime);

        return fx;
    }

    public static void RestartParticleHierarchy(GameObject root)
    {
        if (root == null)
            return;

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Clear(true);
            ps.Play(true);
        }
    }

    public static void DisablePhysicsAndMissileScripts(GameObject root)
    {
        if (root == null)
            return;

        var projectile = root.GetComponent<DefenseProjectile>();
        if (projectile != null)
            projectile.enabled = false;

        var legacy = root.GetComponent<ETFXProjectileScript>();
        if (legacy != null)
            legacy.enabled = false;

        var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    /// <summary>지면 연출 전용 — 콜라이더·파티클 충돌/트리거를 끄고 게임플레이 영향을 제거합니다.</summary>
    public static void EnsureVisualOnly(GameObject root)
    {
        if (root == null)
            return;

        DisablePhysicsAndMissileScripts(root);
        StripParticleInteraction(root);
    }

    private static void StripParticleInteraction(GameObject root)
    {
        if (root == null)
            return;

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null)
                continue;

            var collision = ps.collision;
            if (collision.enabled)
                collision.enabled = false;

            var trigger = ps.trigger;
            if (trigger.enabled)
                trigger.enabled = false;
        }
    }

    private static bool TrySpawnGroundDecal(
        string decalKey,
        Vector3 worldPoint,
        float splashRadius,
        float lifetime)
    {
        if (!TryLoadBurstPrefab(decalKey, out var prefab) || prefab == null)
            return false;

        var scorch = Object.Instantiate(
            prefab,
            SnapToGround(worldPoint, 0.08f),
            ResolveGroundPlacementRotation(prefab.transform));
        float scale = Mathf.Clamp(splashRadius * 0.38f, 0.5f, 2f);
        scorch.transform.localScale = Vector3.one * scale;

        EnsureVisualOnly(scorch);

        var fade = scorch.GetComponent<DefenseGroundScorchFade>();
        if (fade == null)
            fade = scorch.AddComponent<DefenseGroundScorchFade>();

        fade.Play(lifetime);
        return true;
    }

    public static Quaternion ResolveGroundBurstRotation(GameObject prefab)
    {
        return ResolveGroundPlacementRotation(prefab != null ? prefab.transform : null);
    }

    private static Quaternion ResolveGroundPlacementRotation(Transform prefabRoot)
    {
        if (prefabRoot == null)
            return Quaternion.identity;

        // Instantiate(rotation)는 루트 월드 회전을 덮어쓰므로, 프리팹에 baked된 지면 각도를 그대로 사용합니다.
        return prefabRoot.rotation;
    }

    private static Quaternion ResolveGroundLoopRotation(string key, GameObject prefab)
    {
        if (key == "FlamethrowerCartoonyGreen" || key == "FlamethrowerCartoonyFire")
            return Quaternion.Euler(90f, 0f, 0f);

        return ResolveGroundPlacementRotation(prefab != null ? prefab.transform : null);
    }

    public static bool TryLoadBurstPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (DefenseAddressableLoader.TryLoadEffect(key, out prefab) && prefab != null)
            return true;

        if (DefenseAddressableLoader.TryLoadPrefab(key, out prefab) && prefab != null)
            return true;

#if UNITY_EDITOR
        return TryLoadEditorFallbackPrefab(key, out prefab);
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private static bool TryLoadEditorFallbackPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (!EditorFallbackPaths.TryGetValue(key, out var path))
            return false;

        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null;
    }
#endif

    public static bool TrySpawnAt(
        string key,
        Vector3 worldPoint,
        Quaternion rotation,
        float lifetime = 2f)
    {
        if (!TryLoadBurstPrefab(key, out var prefab) || prefab == null)
            return false;

        var fx = Object.Instantiate(prefab, worldPoint, rotation);
        Object.Destroy(fx, lifetime);
        return true;
    }
}
