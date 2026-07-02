using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아크 리피터 등 — 체인 라이트닝 타워와 동일한 연쇄 전기 타격.
/// </summary>
public static class DefenseLightningChainCast
{
    private const float ChainRadius = 6f;
    private const float ChainHopDelay = 0.09f;

    public static void ExecuteFromTower(
        Vector3 origin,
        Transform firstTarget,
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int maxTargets = 3,
        bool playFirstHitSound = true)
    {
        if (firstTarget == null || skill == null)
            return;

        DefenseLightningChainCastRunner.Ensure().Begin(
            origin,
            firstTarget,
            skill,
            tower,
            targetMobility,
            Mathf.Max(1, maxTargets),
            ChainRadius,
            ChainHopDelay,
            playFirstHitSound);
    }

    private sealed class DefenseLightningChainCastRunner : MonoBehaviour
    {
        private static DefenseLightningChainCastRunner instance;

        public static DefenseLightningChainCastRunner Ensure()
        {
            if (instance != null)
                return instance;

            var host = new GameObject(nameof(DefenseLightningChainCastRunner));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DefenseLightningChainCastRunner>();
            return instance;
        }

        public void Begin(
            Vector3 origin,
            Transform firstTarget,
            DefenseSkillData skill,
            DefenseTowerCombatContext tower,
            string targetMobility,
            int maxTargets,
            float chainRadius,
            float hopDelay,
            bool playFirstHitSound)
        {
            StartCoroutine(CastRoutine(
                origin,
                firstTarget,
                skill,
                tower,
                targetMobility,
                maxTargets,
                chainRadius,
                hopDelay,
                playFirstHitSound));
        }

        private static IEnumerator CastRoutine(
            Vector3 origin,
            Transform firstTarget,
            DefenseSkillData skill,
            DefenseTowerCombatContext tower,
            string targetMobility,
            int maxTargets,
            float chainRadius,
            float hopDelay,
            bool playFirstHitSound)
        {
            var hitTargets = new List<Transform>();
            Vector3 fromPosition = origin;
            Transform currentTarget = firstTarget;
            var catalog = DefenseCombatCatalog.Active;
            float damage = tower.baseDamage * skill.damageMultiplier;

            while (currentTarget != null && hitTargets.Count < maxTargets)
            {
                hitTargets.Add(currentTarget);
                Vector3 targetPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(currentTarget);

                if (catalog != null)
                    ChainLightningVisual.PlayBolt(fromPosition, targetPoint, catalog.chainBoltPrefab);
                else
                    DefenseLightningStrike.PlayStrikeVfx(fromPosition, targetPoint);

                if (hitTargets.Count == 1 && playFirstHitSound)
                    DefenseCombatLightningSfx.PlayStrike(targetPoint);

                MonsterStatusCombatResolver.ApplyDamageToEnemy(
                    currentTarget.gameObject,
                    damage,
                    skill.element,
                    targetPoint);

                DefenseEffectApplicator.ApplySkillEffects(currentTarget.gameObject, skill, targetPoint);

                if (catalog?.chainHitExplosionPrefab != null)
                {
                    var hitFx = Object.Instantiate(catalog.chainHitExplosionPrefab, targetPoint, Quaternion.identity);
                    Object.Destroy(hitFx, 2f);
                }

                DefenseCombatVfxSpawn.TrySpawnLightningStrikeScorch(targetPoint);

                if (hitTargets.Count >= maxTargets)
                    break;

                yield return new WaitForSeconds(hopDelay);

                fromPosition = targetPoint;
                currentTarget = FindNextChainTarget(fromPosition, hitTargets, chainRadius, targetMobility);
            }
        }

        private static Transform FindNextChainTarget(
            Vector3 origin,
            List<Transform> alreadyHit,
            float chainRadius,
            string targetMobility)
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform nearest = null;
            float nearestSqr = chainRadius * chainRadius;

            foreach (var enemy in enemies)
            {
                if (!DefenseEnemyQuery.IsLivingEnemy(enemy, requireLanded: true, targetMobility: targetMobility))
                    continue;

                if (alreadyHit.Contains(enemy.transform))
                    continue;

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > nearestSqr)
                    continue;

                nearestSqr = sqr;
                nearest = enemy.transform;
            }

            return nearest;
        }
    }
}
