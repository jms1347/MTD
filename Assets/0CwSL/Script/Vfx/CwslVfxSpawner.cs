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
            CwslVfxPool.ScheduleRelease(instance, lifetime);
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
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnMeleeHit(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(ResolveAssets()?.meleeHitVfx, position, rotation, 1.5f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.9f, 0.35f), 0.8f, 0.3f);
        return spawned;
    }

    public static GameObject SpawnEnemyDeath(Vector3 position, CwslMonsterType monsterType)
    {
        var assets = ResolveAssets();
        var prefab = assets?.enemyDeathVfx
                     ?? assets?.bossDeathVfx
                     ?? assets?.suicideBomberDeathVfx;

        var spawned = Spawn(prefab, position + Vector3.up * 0.5f, Quaternion.identity, 4f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.9f, 0.35f), 1f, 0.4f);
        else
            PrepareEffect(spawned);
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

    public static GameObject AttachMissileTankPowerBoostGlow(Transform parent)
    {
        if (parent == null)
            return null;

        var prefab = ResolvePrefab(
            ResolveAssets()?.missileTankPowerBoostVfx,
            CwslVfxPaths.MissileTankPowerBoostGlow);
        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, 1f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        spawned.transform.localRotation = Quaternion.identity;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnFortifyBlock(Vector3 position)
    {
        var spawned = Spawn(
            ResolveAssets()?.fortifyBlockVfx,
            position,
            Quaternion.identity,
            1.2f,
            0.9f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.45f, 0.75f, 1f), 0.55f, 0.25f);
        return spawned;
    }

    public static GameObject SpawnRangedTankMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        var spawned = Spawn(
            ResolveAssets()?.rangedTankMuzzleVfx,
            position,
            rotation,
            0.45f,
            0.65f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.55f, 0.78f), 0.35f, 0.2f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnRangedTankProjectileHit(Vector3 position, Vector3 fireDirection)
    {
        var rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : Quaternion.identity;
        var spawned = Spawn(
            ResolveAssets()?.rangedTankProjectileHitVfx,
            position + Vector3.up * 0.2f,
            rotation,
            2f,
            0.85f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.45f, 0.72f), 0.55f, 0.3f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachInkBlindAura(Transform anchor)
    {
        if (anchor == null)
            return null;

        var spawned = Spawn(
            ResolveAssets()?.inkBlindAuraVfx ?? ResolveAssets()?.fogZoneLocalVfx,
            anchor.position,
            Quaternion.identity,
            0f,
            1.35f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        PrepareEffect(spawned);
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
            ResolveAssets()?.rammerStunExplosionVfx,
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
            ResolveAssets()?.rammerStunStarsVfx,
            anchor.position,
            Quaternion.identity,
            0f,
            2.8f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.zero;
        // ETFX 프리팹 루트가 X -90°라 별이 세로로 도는 문제 보정
        spawned.transform.localRotation = CwslEtfxVfxOrientation.HeadStatusAttachRotation;
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

    public static void SpawnGoldSpendFountain(Vector3 position, float scale = 1f)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.goldMagnetTrailVfx,
            position + Vector3.up * 0.25f,
            Quaternion.Euler(-70f, Random.Range(0f, 360f), 0f),
            1.2f,
            scale);
        if (spawned != null)
            PrepareEffect(spawned);

        Spawn(
            CwslGameSession.Instance?.Assets?.goldBurstVfx,
            position + Vector3.up * 0.35f,
            Quaternion.identity,
            1.5f,
            scale * 0.65f);
    }

    public static GameObject AttachGatherChargeCircle(Transform parent)
    {
        if (parent == null)
            return null;

        var prefab = ResolvePrefab(
            ResolveAssets()?.gatherChargeCircleVfx,
            CwslVfxPaths.GatherChargeCircle);
        var spawned = Spawn(
            prefab,
            parent.position,
            Quaternion.identity,
            0f,
            1f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, true);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachGatherSlowEnchant(Transform anchor)
    {
        if (anchor == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.gatherSlowEnchantVfx,
            anchor.position,
            Quaternion.identity,
            0f,
            0.85f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.up * 0.35f;
        spawned.transform.localRotation = Quaternion.identity;
        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject SpawnGatherMaxReady(Vector3 center)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.gatherMaxReadyVfx,
            center + Vector3.up * 0.25f,
            Quaternion.identity,
            1.6f,
            1.15f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(center, new Color(1f, 0.9f, 0.25f), 1.2f, 0.35f);
        else
            RestartParticleSystems(spawned);
        return spawned;
    }

    public static void SpawnGatherPull(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.PlayPull(center, radius);
    }

    public static GameObject SpawnBossTeleportDepart(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.bossTeleportDepartVfx,
            position + Vector3.up * 0.15f,
            Quaternion.identity,
            2.4f,
            1.35f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.35f, 0.95f), 1.4f, 0.28f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnBossTeleportArrive(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.bossTeleportArriveVfx,
            position + Vector3.up * 0.15f,
            Quaternion.identity,
            2.6f,
            1.35f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.95f, 0.15f, 0.1f), 1.6f, 0.3f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnBossPhaseTransition(Vector3 position, CwslBossPhase phase)
    {
        var scale = phase switch
        {
            CwslBossPhase.BlackTeleport => 1.1f,
            CwslBossPhase.WhiteTeamBall => 1.25f,
            CwslBossPhase.RedFight => 1.45f,
            CwslBossPhase.GoldFinal => 1.8f,
            _ => 1f
        };

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.bossPhaseTransitionVfx,
            position + Vector3.up * 0.5f,
            Quaternion.identity,
            2.8f,
            scale);
        if (spawned == null)
        {
            var color = phase switch
            {
                CwslBossPhase.BlackTeleport => new Color(0.15f, 0.15f, 0.2f),
                CwslBossPhase.WhiteTeamBall => new Color(0.95f, 0.95f, 0.95f),
                CwslBossPhase.RedFight => new Color(0.95f, 0.15f, 0.1f),
                CwslBossPhase.GoldFinal => new Color(0.95f, 0.78f, 0.15f),
                _ => Color.white
            };
            CwslSimpleVfx.SpawnBurst(position, color, scale * 1.2f, 0.35f);
        }
        else
            PrepareEffect(spawned);

        return spawned;
    }

    public static GameObject AttachFightZoneAura(Transform parent, float zoneDiameter)
    {
        return AttachGroundLoop(
            CwslGameSession.Instance?.Assets?.fightZoneAuraVfx,
            parent,
            zoneDiameter / 6f);
    }

    public static GameObject AttachZoneAura(GameObject prefab, Transform parent, float diameter)
    {
        return AttachGroundLoop(prefab, parent, diameter / 6f);
    }

    public static GameObject AttachBlackHoleVortex(Transform parent, float diameter)
    {
        if (parent == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.blackHoleVortexVfx,
            parent.position,
            Quaternion.identity,
            0f,
            diameter / 4.2f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.15f;
        spawned.transform.localRotation = Quaternion.identity;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnTrapPadTrigger(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.trapPadTriggerVfx,
            position + Vector3.up * 0.25f,
            Quaternion.identity,
            1.4f,
            1.1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.95f, 0.2f, 0.12f), 1.2f, 0.35f);
        else
            RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject AttachFogZone(Transform parent, float diameter)
    {
        if (parent == null)
            return null;

        var assets = CwslGameSession.Instance?.Assets;
        var heavyScale = diameter / 4.2f;
        var livelyScale = diameter / 6.5f;

        GameObject heavy = null;
        if (assets?.fogZoneHeavyVfx != null)
        {
            heavy = Spawn(assets.fogZoneHeavyVfx, parent.position, Quaternion.identity, 0f, heavyScale);
            if (heavy != null)
            {
                heavy.transform.SetParent(parent, true);
                heavy.transform.localPosition = Vector3.up * 0.35f;
                heavy.transform.localRotation = Quaternion.identity;
                PrepareEffect(heavy);
            }
        }

        if (assets?.fogZoneLivelyVfx != null)
        {
            var lively = Spawn(assets.fogZoneLivelyVfx, parent.position, Quaternion.identity, 0f, livelyScale);
            if (lively != null)
            {
                lively.transform.SetParent(parent, true);
                lively.transform.localPosition = Vector3.up * 0.55f;
                lively.transform.localRotation = Quaternion.identity;
                PrepareEffect(lively);
            }
        }

        return heavy;
    }

    public static GameObject AttachLocalFogOverlay(Transform parent, float scale = 1.35f)
    {
        if (parent == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.fogZoneLocalVfx,
            parent.position,
            Quaternion.identity,
            0f,
            scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        spawned.transform.localRotation = Quaternion.identity;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachTeamBallVisual(Transform parent)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.teamBallVisualVfx,
            parent.position,
            Quaternion.identity,
            0f,
            1.15f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachTeamBallTrail(Transform parent)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.teamBallTrailVfx,
            parent.position,
            Quaternion.identity,
            0f,
            0.85f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnTeamBallHit(Vector3 hitPoint)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.teamBallHitVfx,
            hitPoint + Vector3.up * 0.25f,
            Quaternion.identity,
            2f,
            0.95f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(hitPoint, new Color(1f, 0.85f, 0.2f), 0.55f, 0.22f);
        else
            PrepareEffect(spawned);

        Spawn(CwslGameSession.Instance?.Assets?.goldBurstVfx, hitPoint + Vector3.up * 0.35f, Quaternion.identity, 2f, 0.75f);
        return spawned;
    }

    public static GameObject SpawnCornerStoneBreak(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.cornerStoneBreakVfx,
            position + Vector3.up * 0.35f,
            Quaternion.identity,
            3f,
            1.1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.85f, 0.1f, 0.08f), 1.2f, 0.25f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnKarmaMilestone(Vector3 position, int milestone)
    {
        var scale = 1f + milestone * 0.35f;
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.karmaMilestoneVfx,
            position + Vector3.up * 0.6f,
            Quaternion.identity,
            3.5f,
            scale);
        if (spawned == null)
        {
            var color = milestone switch
            {
                1 => new Color(0.8f, 0.65f, 0.2f),
                2 => new Color(0.9f, 0.4f, 0.15f),
                _ => new Color(0.95f, 0.15f, 0.1f)
            };
            CwslSimpleVfx.SpawnBurst(position, color, 2.4f + milestone * 0.6f, 0.35f);
        }
        else
            PrepareEffect(spawned);

        return spawned;
    }

    public static GameObject AttachSilhouetteAura(Transform parent)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.silhouetteAuraVfx,
            parent.position,
            Quaternion.identity,
            0f,
            2.4f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachPressConferenceRing(Transform parent, float diameter)
    {
        return AttachGroundLoop(
            CwslGameSession.Instance?.Assets?.pressConferenceRingVfx,
            parent,
            diameter / 4.5f);
    }

    public static GameObject AttachFinalPhaseRing(Transform parent, float diameter)
    {
        return AttachGroundLoop(
            CwslGameSession.Instance?.Assets?.finalPhaseRingVfx,
            parent,
            diameter / 5f);
    }

    public static GameObject AttachLighthouseGlow(Transform parent)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lighthouseGlowVfx,
            parent.position,
            Quaternion.identity,
            0f,
            1.6f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachWatchGlare(Transform parent)
    {
        if (parent == null)
            return null;

        var assets = CwslGameSession.Instance?.Assets;
        var glare = Spawn(
            assets?.watchGlareVfx,
            parent.position,
            Quaternion.identity,
            0f,
            2.2f);
        if (glare != null)
        {
            glare.transform.SetParent(parent, false);
            glare.transform.localPosition = Vector3.up * 2.5f;
            glare.transform.localRotation = Quaternion.identity;
            PrepareEffect(glare);
        }

        var sparkle = Spawn(
            assets?.watchSparkleVfx,
            parent.position,
            Quaternion.identity,
            0f,
            1.4f);
        if (sparkle != null)
        {
            sparkle.transform.SetParent(parent, false);
            sparkle.transform.localPosition = Vector3.up * 2.85f;
            sparkle.transform.localRotation = Quaternion.identity;
            PrepareEffect(sparkle);
        }

        return glare;
    }

    public static GameObject AttachBossFightShield(Transform parent)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.bossFightShieldVfx,
            parent.position + Vector3.up * 1.2f,
            Quaternion.identity,
            0f,
            2.6f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, true);
        spawned.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        PrepareEffect(spawned);
        return spawned;
    }

    private static GameObject AttachGroundLoop(GameObject prefab, Transform parent, float scale)
    {
        if (parent == null)
            return null;

        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, true);
        spawned.transform.localPosition = Vector3.up * 0.05f;
        spawned.transform.localRotation = CwslEtfxVfxOrientation.GroundSlamRotationOffset;
        PrepareLoopingGroundEffect(spawned);
        return spawned;
    }

    public static void PrepareLoopingGroundEffect(GameObject root)
    {
        PrepareEffect(root);
        if (root == null)
            return;

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.loop = true;
            ps.Play(true);
        }
    }

    private static void PrepareEffect(GameObject root)
    {
        DisableCartoonFxAutoDestruct(root);
        DisablePhysics(root);
        RestartParticleSystems(root);
        RestartAudioSources(root);
    }

    private static void DisableCartoonFxAutoDestruct(GameObject root)
    {
        foreach (var autoDestruct in root.GetComponentsInChildren<CFX_AutoDestructShuriken>(true))
            Object.Destroy(autoDestruct);

        foreach (var autoDestruct in root.GetComponentsInChildren<CFX_AutodestructWhenNoChildren>(true))
            Object.Destroy(autoDestruct);
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

    private static void RestartAudioSources(GameObject root)
    {
        foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
        {
            if (source.clip == null)
                continue;

            source.Stop();
            source.Play();
        }
    }

    private static void TintParticleSystems(GameObject root, Color tint)
    {
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            var current = main.startColor;
            if (current.mode == ParticleSystemGradientMode.Color)
            {
                main.startColor = tint;
                continue;
            }

            if (current.mode == ParticleSystemGradientMode.TwoColors)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(
                    current.colorMin * tint,
                    current.colorMax * tint);
            }
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

        return CwslVfxPool.Acquire(prefab, position, rotation);
    }

    public static void PrepareReusedEffect(GameObject root)
    {
        PrepareEffect(root);
    }

    public static GameObject SpawnFakeGoldExplosion(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.fakeGoldExplosionVfx,
            position + Vector3.up * 0.25f,
            Quaternion.identity,
            1.6f,
            1.15f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.25f, 0.1f), 1.4f, 0.4f);
        else
            RestartParticleSystems(spawned);
        return spawned;
    }

    public static void SpawnDonationBurst(Vector3 origin)
    {
        Spawn(
            CwslGameSession.Instance?.Assets?.goldBurstVfx,
            origin + Vector3.up * 0.35f,
            Quaternion.identity,
            1.4f,
            1.2f);
    }

    public static void SpawnOffsideLaserLine(Transform parent, Vector3 lineStart, Vector3 lineEnd)
    {
        var delta = lineEnd - lineStart;
        delta.y = 0f;
        var length = delta.magnitude;
        if (length < 0.1f)
            return;

        var direction = delta / length;
        var spacing = 2.4f;
        var count = Mathf.CeilToInt(length / spacing);
        for (var i = 0; i <= count; i++)
        {
            var pos = lineStart + direction * (i * spacing);
            pos.y = 0.35f;
            var rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            var segment = Spawn(
                CwslGameSession.Instance?.Assets?.offsideLaserMissileVfx,
                pos,
                rotation,
                0f,
                0.85f);
            if (segment == null)
                continue;

            segment.transform.SetParent(parent, true);
            DisablePhysics(segment);
            RestartParticleSystems(segment);
        }
    }

    public static GameObject AttachBadGrassAura(Transform parent, float diameter)
    {
        return AttachGroundLoop(
            CwslGameSession.Instance?.Assets?.badGrassAuraVfx,
            parent,
            diameter / 6f);
    }

    public static GameObject AttachHealingSpringAura(Transform parent, float diameter)
    {
        var assets = CwslGameSession.Instance?.Assets;
        return AttachGroundLoop(
            assets?.healingSpringAuraVfx ?? assets?.karmaHalfZoneAuraVfx,
            parent,
            diameter / 5.5f);
    }

    public static GameObject AttachTailwindGrassAura(Transform parent, float diameter)
    {
        var assets = CwslGameSession.Instance?.Assets;
        return AttachGroundLoop(
            assets?.tailwindGrassAuraVfx ?? assets?.gatherPullBurstVfx,
            parent,
            diameter / 6f);
    }

    public static GameObject AttachRallyZoneAura(Transform parent, float diameter)
    {
        var assets = CwslGameSession.Instance?.Assets;
        return AttachGroundLoop(
            assets?.rallyZoneAuraVfx ?? assets?.karmaHalfZoneAuraVfx,
            parent,
            diameter / 6f);
    }

    public static GameObject AttachGoldSpringAura(Transform parent, float diameter)
    {
        var assets = CwslGameSession.Instance?.Assets;
        return AttachGroundLoop(
            assets?.goldSpringAuraVfx ?? assets?.donationPadGlowVfx,
            parent,
            diameter / 5.5f);
    }

    public static GameObject SpawnGoldSpringBurst(Vector3 position)
    {
        var assets = CwslGameSession.Instance?.Assets;
        return Spawn(
            assets?.goldSpringBurstVfx ?? assets?.watchSparkleVfx,
            position,
            Quaternion.identity,
            1.2f,
            1.4f);
    }

    public static GameObject SpawnPillBurst(Vector3 position, CwslPillType pillType)
    {
        var assets = CwslGameSession.Instance?.Assets;
        var prefab = pillType switch
        {
            CwslPillType.Green => assets?.pillBuffGreenVfx,
            CwslPillType.Yellow => assets?.pillBuffYellowVfx,
            _ => assets?.pillBuffBlueVfx
        };

        var spawned = Spawn(prefab, position, Quaternion.identity, 2f, 1.15f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachPillSpeedAura(Transform parent)
    {
        if (parent == null)
            return null;

        var assets = CwslGameSession.Instance?.Assets;
        var spawned = Spawn(
            assets?.pillSphereBlueVfx ?? assets?.fortifyAuraVfx,
            parent.position + Vector3.up * 0.9f,
            Quaternion.identity,
            0f,
            1.8f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.9f;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachPillFreeSkillAura(Transform parent)
    {
        if (parent == null)
            return null;

        var assets = CwslGameSession.Instance?.Assets;
        var spawned = Spawn(
            assets?.pillSphereYellowVfx ?? assets?.pressConferenceRingVfx,
            parent.position + Vector3.up * 0.9f,
            Quaternion.identity,
            0f,
            1.8f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.9f;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachPillHealAura(Transform parent)
    {
        if (parent == null)
            return null;

        var assets = CwslGameSession.Instance?.Assets;
        var spawned = Spawn(
            assets?.pillBuffYellowVfx ?? assets?.pillSphereYellowVfx,
            parent.position + Vector3.up * 0.9f,
            Quaternion.identity,
            0f,
            1.6f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.9f;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachDonationPadGlow(Transform parent, float scale)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.donationPadGlowVfx,
            parent.position + Vector3.up * 0.08f,
            Quaternion.identity,
            0f,
            scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.08f;
        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject AttachLightningZoneAura(Transform parent, float diameter)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningZoneAuraVfx,
            parent.position,
            Quaternion.identity,
            0f,
            diameter / 8f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject AttachLightningOrb(Transform parent, float scale)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningOrbVfx,
            parent.position,
            Quaternion.identity,
            0f,
            scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnLightningMissile(Vector3 origin, Vector3 target)
    {
        return SpawnMissile(
            CwslGameSession.Instance?.Assets?.lightningMissileVfx,
            origin,
            target,
            0f,
            0.95f);
    }

    public static GameObject SpawnMissile(
        GameObject prefab,
        Vector3 origin,
        Vector3 target,
        float lifetime,
        float scale)
    {
        if (prefab == null)
            return null;

        var flat = target - origin;
        flat.y = 0f;
        var rotation = flat.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(flat.normalized, Vector3.up)
            : Quaternion.identity;

        var spawned = Spawn(prefab, origin, rotation, lifetime, scale);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static void PlayLightningBolt(
        Vector3 origin,
        Vector3 target,
        float speed,
        GameObject missilePrefab = null,
        GameObject strikePrefab = null,
        bool randomRedMageStrike = false)
    {
        var runner = new GameObject("CwslLightningBolt");
        runner.transform.position = origin;
        runner.AddComponent<CwslLightningMissileRunner>()
            .Play(origin, target, speed, missilePrefab, strikePrefab, randomRedMageStrike);
    }

    public static void PlayRedMageLightningBolt(Vector3 orbPosition, Vector3 targetPoint)
    {
        var assets = ResolveAssets();
        PlayLightningBolt(
            orbPosition,
            targetPoint,
            CwslGameConstants.LightningMissileSpeed,
            assets?.redMageLightningBoltVfx,
            null,
            randomRedMageStrike: true);
    }

    public static GameObject SpawnRedMageLightningStrike(Vector3 strikePoint)
    {
        var assets = ResolveAssets();
        var useTall = Random.value >= 0.5f;
        var prefab = useTall
            ? ResolvePrefab(
                assets?.redMageLightningStrikeTallVfx,
                CwslVfxPaths.RedMageLightningStrikeTall)
              ?? ResolvePrefab(
                  assets?.redMageLightningStrikeVfx,
                  CwslVfxPaths.RedMageLightningStrike)
            : ResolvePrefab(
                  assets?.redMageLightningStrikeVfx,
                  CwslVfxPaths.RedMageLightningStrike)
              ?? ResolvePrefab(
                  assets?.redMageLightningStrikeTallVfx,
                  CwslVfxPaths.RedMageLightningStrikeTall);

        if (prefab == null)
            return SpawnLightningStrike(strikePoint);

        var spawned = Spawn(
            prefab,
            strikePoint + Vector3.up * 0.08f,
            Quaternion.identity,
            1.25f,
            1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(strikePoint, new Color(0.45f, 0.75f, 1f), 1.1f, 0.3f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachRedMageLightningOrbGroundRadius(Transform parent, float radius)
    {
        if (parent == null)
            return null;

        var prefab = ResolvePrefab(
            ResolveAssets()?.redMageLightningOrbRadiusVfx,
            CwslVfxPaths.RedMageLightningOrbRadius);
        if (prefab == null)
            return null;

        var diameter = Mathf.Max(0.5f, radius * 2f);
        var scale = diameter / CwslGameConstants.RedMageLightningOrbGroundRadiusVfxDiameterDivisor;
        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = CwslEtfxVfxOrientation.GroundSlamRotationOffset;
        PrepareLoopingGroundEffect(spawned);
        return spawned;
    }

    public static GameObject AttachFrozenOrbIceBall(Transform parent, float scale)
    {
        if (parent == null)
            return null;

        var prefab = ResolvePrefab(
            ResolveAssets()?.frozenOrbIceBallVfx,
            CwslVfxPaths.FrozenOrbIceBall);
        if (prefab == null)
            return null;

        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        spawned.transform.localScale = Vector3.one;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnFrozenOrbHitAir(Vector3 hitPoint)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.frozenOrbHitAirVfx,
            CwslVfxPaths.FrozenOrbHitAir);
        var spawned = Spawn(
            prefab,
            hitPoint + Vector3.up * 0.45f,
            Quaternion.identity,
            1.4f,
            1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(hitPoint, new Color(0.65f, 0.9f, 1f), 0.9f, 0.35f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnFrozenOrbGroundTrail(Vector3 groundPoint)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.frozenOrbGroundTrailVfx,
            CwslVfxPaths.FrozenOrbGroundTrail);
        var point = groundPoint;
        point.y = CwslTankShieldVfxUtil.VisualGroundY;
        var spawned = Spawn(
            prefab,
            point,
            CwslCfx3VfxOrientation.GroundHitLocalRotation,
            1.6f,
            1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(point, new Color(0.55f, 0.85f, 1f), 0.75f, 0.28f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnLightningStrike(Vector3 strikePoint)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningStrikeVfx,
            strikePoint + Vector3.up * 0.05f,
            Quaternion.identity,
            1.2f,
            1f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(strikePoint, new Color(0.55f, 0.75f, 1f), 1.1f, 0.3f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnLightningStunExplosion(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningStunExplosionVfx,
            position + Vector3.up * 0.25f,
            Quaternion.identity,
            2f,
            0.95f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.45f, 0.95f), 1f, 0.35f);
        else
        {
            DisablePhysics(spawned);
            RestartParticleSystems(spawned);
        }

        return spawned;
    }

    public static GameObject SpawnLightningStunStrike(Vector3 position)
    {
        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningStunStrikeVfx,
            position,
            Quaternion.identity,
            1.4f,
            0.85f);
        if (spawned == null)
            return null;

        DisablePhysics(spawned);
        RestartParticleSystems(spawned);
        return spawned;
    }

    public static GameObject AttachLightningStunAura(Transform anchor)
    {
        if (anchor == null)
            return null;

        var spawned = Spawn(
            CwslGameSession.Instance?.Assets?.lightningOrbVfx,
            anchor.position,
            Quaternion.identity,
            0f,
            0.38f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.zero;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachHazardPad(CwslHazardPadKind kind, Transform parent, float diameter)
    {
        var prefab = kind switch
        {
            CwslHazardPadKind.Acid => CwslGameSession.Instance?.Assets?.hazardAcidPadVfx,
            CwslHazardPadKind.Lava => CwslGameSession.Instance?.Assets?.hazardLavaPadVfx,
            CwslHazardPadKind.Water => CwslGameSession.Instance?.Assets?.hazardWaterPadVfx,
            _ => null
        };

        return AttachGroundLoop(prefab, parent, diameter / 5.5f);
    }

    public static GameObject AttachEventWarningZone(GameObject prefab, Transform parent, float diameter)
    {
        return AttachGroundLoop(prefab, parent, diameter / 6.2f);
    }

    public static GameObject SpawnShieldSlamGroundHit(
        Vector3 position,
        Quaternion rotation,
        float scale,
        bool empowered)
    {
        var assets = ResolveAssets();
        var prefab = empowered ? assets?.shieldSlamCartoonyVfx : assets?.shieldSlamSoftVfx;
        var lifetime = empowered ? 2.8f : 2.2f;
        var spawned = Spawn(prefab, position, rotation, lifetime, scale);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(0.75f, 0.68f, 0.35f), scale * 0.9f, 0.35f);
        else
            PrepareEffect(spawned);

        return spawned;
    }

    public static GameObject AttachShieldDashWave(Transform visualRoot, float scale, Vector3 dashDirection)
    {
        if (visualRoot == null)
            return null;

        var spawned = Spawn(
            ResolveAssets()?.shieldDashWaveVfx,
            visualRoot.position,
            Quaternion.identity,
            0f,
            scale);
        if (spawned == null)
            return null;

        // 플레이어 전방(이동 방향) 기준으로 부착 — 방패 로컬 회전 오차(90° 등) 영향 제거.
        spawned.transform.SetParent(visualRoot, false);
        spawned.transform.localPosition = CwslTankShieldVfxUtil.GetDashWaveLocalOffset(scale) + Vector3.up * 0.42f;
        spawned.transform.localRotation = CwslTankShieldVfxUtil.GetDashWaveLocalRotation();
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachShieldWhirlwind(Transform shield, float scale, float lifetime)
    {
        if (shield == null)
            return null;

        var spawned = Spawn(
            ResolveAssets()?.shieldWhirlwindVfx,
            shield.position,
            shield.rotation,
            lifetime > 0f ? lifetime : 0f,
            scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(shield, false);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = CwslTankShieldVfxUtil.GetShieldWhirlwindAttachLocalRotation();
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachMonsterStatusEffect(
        GameObject prefab,
        Transform parent,
        Vector3 localPosition,
        float scale,
        Quaternion localRotation)
    {
        if (prefab == null || parent == null)
            return null;

        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, scale <= 0f ? 1f : scale);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = localPosition;
        spawned.transform.localRotation = localRotation;
        PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachMonsterStatusEffect(
        GameObject prefab,
        Transform parent,
        Vector3 localPosition,
        float scale)
    {
        return AttachMonsterStatusEffect(prefab, parent, localPosition, scale, Quaternion.identity);
    }

    public static GameObject SpawnRammerBrakeBurst(Vector3 position, float scale)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.rammerBrakeBurstVfx, CwslVfxPaths.RammerBrakeBurst);
        var spawned = Spawn(prefab, position + Vector3.up * 0.2f, Quaternion.identity, 2f, scale);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.75f, 0.25f), 1.4f * scale, 0.4f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnRammerRopeAttach(Vector3 position)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.rammerRopeAttachVfx, CwslVfxPaths.RammerRopeAttach);
        var spawned = Spawn(prefab, position + Vector3.up * 0.8f, Quaternion.identity, 1.4f, 1.1f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnRammerRopeFling(Vector3 position, Vector3 direction)
    {
        var rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
        var prefab = ResolvePrefab(ResolveAssets()?.rammerRopeFlingVfx, CwslVfxPaths.RammerRopeFling);
        var spawned = Spawn(prefab, position + Vector3.up * 0.7f, rotation, 1.2f, 1.3f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachRammerFireTrail(Transform parent)
    {
        if (parent == null)
            return null;

        ClearRammerFireTrail(parent);
        var prefab = ResolvePrefab(ResolveAssets()?.rammerFireTrailVfx, CwslVfxPaths.RammerFireTrail);
        var spawned = Spawn(prefab, parent.position, Quaternion.identity, 0f, 1.15f);
        if (spawned == null)
            return null;

        spawned.name = "RammerFireTrailVfx";
        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.2f;
        PrepareEffect(spawned);
        Object.Destroy(spawned, CwslGameConstants.RammerFireTrailDuration + 0.4f);
        return spawned;
    }

    public static void ClearRammerFireTrail(Transform parent)
    {
        if (parent == null)
            return;

        var existing = parent.Find("RammerFireTrailVfx");
        if (existing != null)
            Object.Destroy(existing.gameObject);
    }

    public static GameObject SpawnRammerFireTrailZone(Vector3 position)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.rammerFireTrailZoneVfx ?? ResolveAssets()?.meteorGroundFireSoftAbVfx,
            CwslVfxPaths.RammerFireTrailZone);
        var spawned = Spawn(
            prefab,
            position,
            Quaternion.identity,
            CwslGameConstants.RammerFireTrailZoneLifetime + 0.2f,
            0.85f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnGathererYank(Vector3 from, Vector3 to)
    {
        var mid = (from + to) * 0.5f;
        mid.y = 0.2f;
        var prefab = ResolvePrefab(
            ResolveAssets()?.gathererYankBurstVfx ?? ResolveAssets()?.gatherPullVortexVfx,
            CwslVfxPaths.GathererYankBurst);
        var spawned = Spawn(prefab, mid, Quaternion.identity, 1.2f, 1.35f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnGathererSwap(Vector3 position)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.gathererSwapPortalVfx,
            CwslVfxPaths.GathererSwapPortal);
        var spawned = Spawn(prefab, position + Vector3.up * 0.1f, Quaternion.identity, 1.1f, 1.1f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnGathererBlackHole(Vector3 center, float radius, float duration)
    {
        var root = new GameObject("GathererBlackHoleVfx");
        root.transform.position = center;
        var prefab = ResolvePrefab(
            ResolveAssets()?.gathererBlackHoleVfx ?? ResolveAssets()?.blackHoleVortexVfx,
            CwslVfxPaths.GathererBlackHole);
        var spawned = Spawn(prefab, center, Quaternion.identity, 0f, radius / 3.2f);
        if (spawned != null)
        {
            spawned.transform.SetParent(root.transform, true);
            PrepareEffect(spawned);
        }

        Object.Destroy(root, duration + 0.1f);
        return root;
    }

    public static GameObject SpawnGathererMuzzle(Vector3 position)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.gathererMissileVfx, CwslVfxPaths.GathererMissileVisual);
        var spawned = Spawn(prefab, position, Quaternion.identity, 0.6f, 0.55f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnBarricadeDetonate(Vector3 position)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.barricadeDetonateExplosionVfx,
            CwslVfxPaths.BarricadeDetonateExplosion);
        var spawned = Spawn(prefab, position + Vector3.up * 0.2f, Quaternion.identity, 2.2f, 1.2f);
        if (spawned == null)
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.4f, 0.1f), 1.6f, 0.45f);
        else
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnBarricadeJumpPad(Vector3 position, float radius, float duration)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.barricadeJumpPadAuraVfx,
            CwslVfxPaths.BarricadeJumpPadAura);
        var spawned = Spawn(prefab, position, Quaternion.identity, duration, radius / 3.5f);
        if (spawned != null)
            PrepareEffect(spawned);
        PlayBarricadeJumpPadSound(position);
        return spawned;
    }

    private static void PlayBarricadeJumpPadSound(Vector3 position)
    {
        var clip = ResolveAssets()?.barricadeJumpPadSound;
#if UNITY_EDITOR
        if (clip == null)
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BarricadeJumpPadSound);
#endif
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, 0.95f);
    }

    public static GameObject SpawnBarricadeRepair(Vector3 position)
    {
        var prefab = ResolvePrefab(
            ResolveAssets()?.barricadeRepairSparksVfx,
            CwslVfxPaths.BarricadeRepairSparks);
        var spawned = Spawn(prefab, position, Quaternion.identity, 1.2f, 1.1f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnBarricadeBuildDust(Vector3 position)
    {
        return SpawnBarricadeRepair(position);
    }

    public static GameObject SpawnHealerHealPad(Vector3 position, float radius, float duration)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.healerHealPadVfx, CwslVfxPaths.HealerHealPad);
        var spawned = Spawn(prefab, position, CwslEtfxVfxOrientation.GroundSlamRotationOffset, duration, radius / 3.8f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject SpawnHealerHealBurst(Vector3 position, Color? tint = null)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.healerHealBurstVfx, CwslVfxPaths.HealerHealBurst);
        var spawned = Spawn(prefab, position + Vector3.up * 0.8f, CwslEtfxVfxOrientation.GroundSlamRotationOffset, 1.6f, 1.15f);
        if (spawned != null)
        {
            PrepareEffect(spawned);
            if (tint.HasValue)
                TintParticleSystems(spawned, tint.Value);
        }
        return spawned;
    }

    public static GameObject SpawnHealerPoisonPad(Vector3 position, float radius, float duration)
    {
        var prefab = ResolvePrefab(ResolveAssets()?.healerPoisonPadVfx, CwslVfxPaths.HealerPoisonPad);
        var spawned = Spawn(prefab, position + Vector3.up * 0.4f, CwslEtfxVfxOrientation.GroundSlamRotationOffset, duration, radius / 3.5f);
        if (spawned != null)
            PrepareEffect(spawned);
        return spawned;
    }

    public static GameObject AttachHealerHasteBuff(Transform parent, float duration)
    {
        if (parent == null)
            return null;

        // R 버프는 항상 노란 계열 프리팹 경로를 우선 사용해 색상을 고정한다.
        var prefab = ResolvePrefab(null, CwslVfxPaths.HealerHasteBuff)
                     ?? ResolvePrefab(ResolveAssets()?.healerHasteBuffVfx, CwslVfxPaths.HealerHasteBuff);
        var spawned = Spawn(prefab, parent.position + Vector3.up * 0.9f, Quaternion.identity, 0f, 1.35f);
        if (spawned == null)
            return null;

        spawned.transform.SetParent(parent, false);
        spawned.transform.localPosition = Vector3.up * 0.9f;
        spawned.transform.localRotation = CwslEtfxVfxOrientation.HeadStatusAttachRotation;
        PrepareEffect(spawned);
        Object.Destroy(spawned, duration + 0.05f);
        return spawned;
    }

    public static CwslGameAssets GetAssets() => ResolveAssets();

    private static GameObject ResolvePrefab(GameObject assigned, string assetPath) =>
        CwslVfxPrefabResolver.Resolve(assigned, assetPath);

    private static CwslGameAssets ResolveAssets() =>
        CwslGameSession.Instance?.Assets ?? CwslVisualTestAssetsContext.Assets;
}
