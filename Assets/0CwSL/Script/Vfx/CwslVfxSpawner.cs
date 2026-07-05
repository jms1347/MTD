using UnityEngine;

public static class CwslVfxSpawner
{
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 3f, float scale = 1f)
    {
        var instance = TryInstantiate(prefab, position, rotation);
        if (instance == null)
            return null;

        if (Mathf.Abs(scale - 1f) > 0.001f)
            instance.transform.localScale = Vector3.one * scale;

        if (lifetime > 0f)
            Object.Destroy(instance, lifetime);
        return instance;
    }

    public static GameObject SpawnDarkMissile(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(CwslGameSession.Instance?.Assets?.darkMissileVfx, position, rotation, 4f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.15f, 0.1f, 0.2f), 0.35f, 0.25f);
        return spawned;
    }

    public static GameObject SpawnSuicideExplosion(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.suicideExplosionVfx,
            position,
            Quaternion.identity,
            4f,
            CwslGameConstants.SuicideExplosionScale);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.45f, 0.1f), 1.2f, 0.45f);
        return spawned;
    }

    public static GameObject SpawnMeleeHit(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(CwslGameSession.Instance?.Assets?.meleeHitVfx, position, rotation, 1.5f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.9f, 0.35f), 0.8f, 0.3f);
        return spawned;
    }

    public static GameObject SpawnEnemyDeath(Vector3 position, CwslMonsterType monsterType)
    {
        var assets = CwslGameSession.Instance?.Assets;
        var prefab = monsterType == CwslMonsterType.BossHongmyeongbo
            ? assets?.bossDeathVfx ?? assets?.enemyDeathVfx
            : assets?.enemyDeathVfx;

        var spawned = Spawn(prefab, position + Vector3.up * 0.5f, Quaternion.identity, 4f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.9f, 0.35f), 1f, 0.4f);
        return spawned;
    }

    public static GameObject SpawnPlayerDeath(Vector3 position)
    {
        var spawned = Spawn(CwslGameSession.Instance?.Assets?.playerDeathVfx, position + Vector3.up * 0.8f, Quaternion.identity, 4f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.45f, 0.65f, 0.95f), 1.2f, 0.5f);
        return spawned;
    }

    public static GameObject SpawnFortifyAura(Transform parent)
    {
        if (parent == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.fortifyAuraVfx,
            parent.position + Vector3.up * 1.0f,
            Quaternion.identity,
            0f,
            2.2f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, true);
        spawned.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        return spawned;
    }

    public static GameObject SpawnFortifyBlock(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.fortifyBlockVfx,
            position,
            Quaternion.identity,
            1.2f,
            0.9f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.45f, 0.75f, 1f), 0.55f, 0.25f);
        return spawned;
    }

    public static GameObject SpawnGunMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.gunMuzzleVfx,
            position,
            rotation,
            0.45f,
            0.55f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.85f, 0.35f), 0.35f, 0.2f);
        return spawned;
    }

    public static GameObject SpawnRammerStunExplosion(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.rammerStunExplosionVfx,
            position + Vector3.up * 0.35f,
            Quaternion.identity,
            3f,
            0.9f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.85f, 0.2f), 0.9f, 0.4f);
        else
            DisablePhysics(spawned);

        return spawned;
    }

    public static GameObject AttachRammerStunStars(Transform anchor)
    {
        if (anchor == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.rammerStunStarsVfx,
            anchor.position,
            Quaternion.identity,
            0f,
            2.8f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.zero;
        // ETFX 프리팹 루트가 X -90°라 별이 세로로 도는 문제 보정
        spawned.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject SpawnShadowProjectileHit(Vector3 position, Vector3 fireDirection)
    {
        var rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : Quaternion.identity;
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.shadowProjectileHitVfx,
            position + Vector3.up * 0.2f,
            rotation,
            2.5f,
            0.9f);
        if (spawned == null)
        {
            CwslSimpleVfx.SpawnBurst(position, new Color(0.45f, 0.15f, 0.65f), 0.55f, 0.3f);
            return null;
        }

        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject SpawnShadowMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.shadowMuzzleVfx,
            position,
            rotation,
            0.55f,
            1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.55f, 0.2f, 0.85f), 0.35f, 0.18f);
        else
            RestartParticleSystems(spawned);

        return spawned;
    }

    private static void RestartParticleSystems(GameObject root)
    {
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private static void DisablePhysics(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }
    }

    public static GameObject TryInstantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (prefab is not GameObject)
        {
            Debug.LogWarning("[CwSL] VFX 참조가 GameObject가 아닙니다. Tools → CwSL → Setup Game Scene을 실행하세요.");
            return null;
        }

        return Object.Instantiate(prefab, position, rotation);
    }
}
