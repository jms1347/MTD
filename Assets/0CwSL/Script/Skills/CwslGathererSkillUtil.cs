using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public static class CwslGathererSkillUtil
{
    public static bool IsMissileOrBombMonster(CwslMonsterHealth monster)
    {
        if (monster == null || !monster.IsAlive)
            return false;

        var type = monster.MonsterType;
        return type is CwslMonsterType.Ranged
            or CwslMonsterType.NexusRanged
            or CwslMonsterType.InkSniper
            or CwslMonsterType.NexusInkSniper
            or CwslMonsterType.Suicide
            or CwslMonsterType.NexusSuicide
            or CwslMonsterType.StickySuicide;
    }

    public static bool IsMissileOrBombPlayer(NetworkObject networkObject)
    {
        if (networkObject == null)
            return false;

        var character = networkObject.GetComponent<CwslPlayerCharacter>();
        return character != null && character.CharacterId == CwslCharacterId.MissileTank;
    }

    public static bool IsSwappableUnit(Transform target)
    {
        if (target == null)
            return false;

        if (target.GetComponent<CwslBarricadeWall>() != null)
            return false;

        if (target.GetComponent<CwslMonsterHealth>() is { IsAlive: true })
            return true;

        if (target.GetComponent<CwslPlayerHealth>() is { IsAlive: true })
            return true;

        if (target.GetComponent<CwslMonsterProjectile>() is { IsActiveProjectile: true })
            return true;

        if (target.GetComponent<CwslPlayerProjectile>() is { IsActiveProjectile: true })
            return true;

        return target.GetComponent<CwslFrozenOrbProjectile>() != null;
    }

    public static void CollectInCircle(
        Vector3 center,
        float radius,
        List<Transform> results,
        bool swappableOnly = false)
    {
        results.Clear();
        var radiusSq = radius * radius;

        foreach (var monster in CwslCombatRegistry.AliveMonsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsInsideFlatRadius(center, monster.transform.position, radiusSq))
                continue;

            if (swappableOnly && !IsSwappableUnit(monster.transform))
                continue;

            results.Add(monster.transform);
        }

        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null)
                    continue;

                var health = playerObject.GetComponent<CwslPlayerHealth>();
                if (health == null || !health.IsAlive)
                    continue;

                if (!IsInsideFlatRadius(center, playerObject.transform.position, radiusSq))
                    continue;

                if (swappableOnly && !IsSwappableUnit(playerObject.transform))
                    continue;

                results.Add(playerObject.transform);
            }
        }

        foreach (var projectile in CwslCombatRegistry.ActiveMonsterProjectiles)
        {
            if (projectile == null || !projectile.IsActiveProjectile)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSq))
                continue;

            if (swappableOnly && !IsSwappableUnit(projectile.transform))
                continue;

            results.Add(projectile.transform);
        }

        foreach (var projectile in CwslCombatRegistry.ActivePlayerProjectiles)
        {
            if (projectile == null || !projectile.IsActiveProjectile)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSq))
                continue;

            if (swappableOnly && !IsSwappableUnit(projectile.transform))
                continue;

            results.Add(projectile.transform);
        }
    }

    public static bool IsInsideFlatRadius(Vector3 center, Vector3 target, float radiusSq)
    {
        var flat = target - center;
        flat.y = 0f;
        return flat.sqrMagnitude <= radiusSq;
    }

    public static void WarpTransform(Transform target, Vector3 destination)
    {
        if (target == null)
            return;

        destination = CwslArenaUtility.ClampToPlayArea(destination, 0.4f);

        var projectile = target.GetComponent<CwslMonsterProjectile>() != null
                         || target.GetComponent<CwslPlayerProjectile>() != null
                         || target.GetComponent<CwslFrozenOrbProjectile>() != null;
        if (projectile)
        {
            target.position = destination;
            return;
        }

        var rammer = target.GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            target.position = destination;
            return;
        }

        var agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(destination);
        else
            target.position = destination;
    }

    public static void PullTowardCenter(Transform target, Vector3 center, float radius, float pullSpeed)
    {
        if (target == null)
            return;

        var flat = center - target.position;
        flat.y = 0f;
        var radiusSq = radius * radius;
        if (flat.sqrMagnitude > radiusSq || flat.sqrMagnitude < 0.2f)
            return;

        var distance = flat.magnitude;
        var strength = Mathf.Lerp(1.35f, 0.5f, distance / Mathf.Max(0.01f, radius));
        var next = target.position + flat.normalized * (pullSpeed * strength * Time.deltaTime);
        next.y = target.position.y;
        WarpTransform(target, next);
    }
}
