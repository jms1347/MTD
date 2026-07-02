using System.Collections;
using UnityEngine;

public class MeleeMonster : MonsterAttack
{
    private bool isAttacking;
    private float nextAttackTime;
    private Transform attackTarget;

    public override void Reset(Monster monster)
    {
        StopAllCoroutines();
        isAttacking = false;
        nextAttackTime = 0f;
        attackTarget = null;
    }

    public override void Interrupt(Monster monster)
    {
        StopAllCoroutines();
        isAttacking = false;
        monster.transform.localScale = monster.DefaultScale;
    }

    public override void Tick(Monster monster)
    {
        if (isAttacking)
            return;

        var health = monster.GetComponent<Health>();
        if (health != null && !health.IsAlive)
            return;

        attackTarget = Monster.FindAttackTarget(monster);
        if (attackTarget == null)
            return;

        Vector3 toTarget = attackTarget.position - monster.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= monster.AttackRange && monster.CanAttack && Time.time >= nextAttackTime)
        {
            monster.StartCoroutine(ChargeAttack(monster, toTarget.normalized, attackTarget));
            return;
        }

        if (distance <= monster.AttackRange || !monster.CanMove)
            return;

        Vector3 moveTarget = MonsterMovement.ResolveMoveTarget(monster, attackTarget);
        MoveTowardsNexus(monster, moveTarget, toTarget, distance);
    }

    private static void MoveTowardsNexus(Monster monster, Vector3 targetPosition, Vector3 toTarget, float distance)
    {
        var current = monster.transform.position;
        var direction = toTarget / distance;
        var nextPosition = monster.MoveAlongPath(targetPosition);
        monster.transform.position = nextPosition;

        var moveDir = nextPosition - current;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.0001f)
            direction = moveDir.normalized;

        monster.transform.rotation = Quaternion.Slerp(
            monster.transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 10f);
    }

    private IEnumerator ChargeAttack(Monster monster, Vector3 direction, Transform target)
    {
        if (!monster.CanAttack)
            yield break;

        isAttacking = true;
        monster.GetComponent<MonsterSlimeVisual>()?.PlayAttack();
        Vector3 startPosition = monster.transform.position;
        startPosition.y = monster.GroundY;

        Vector3 windUpPosition = startPosition - direction * monster.WindUpDistance;
        float elapsed = 0f;
        const float windUpDuration = 0.28f;

        while (elapsed < windUpDuration)
        {
            if (!monster.CanAttack || !monster.IsAlive)
            {
                isAttacking = false;
                monster.transform.localScale = monster.DefaultScale;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / windUpDuration;
            Vector3 position = Vector3.Lerp(startPosition, windUpPosition, t);
            position.y = monster.GroundY;
            monster.transform.position = position;
            monster.transform.localScale = Vector3.Lerp(
                monster.DefaultScale,
                new Vector3(monster.DefaultScale.x * 1.25f, monster.DefaultScale.y * 0.75f, monster.DefaultScale.z * 1.25f),
                Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        Vector3 lungePosition = startPosition + direction * monster.LungeDistance;
        elapsed = 0f;
        const float lungeDuration = 0.14f;

        while (elapsed < lungeDuration)
        {
            if (!monster.CanAttack || !monster.IsAlive)
            {
                isAttacking = false;
                monster.transform.localScale = monster.DefaultScale;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / lungeDuration;
            Vector3 position = Vector3.Lerp(windUpPosition, lungePosition, t);
            position.y = monster.GroundY;
            monster.transform.position = position;
            monster.transform.localScale = Vector3.Lerp(
                new Vector3(monster.DefaultScale.x * 0.85f, monster.DefaultScale.y * 1.25f, monster.DefaultScale.z * 0.85f),
                monster.DefaultScale,
                t);
            yield return null;
        }

        if (target != null && Vector3.Distance(monster.transform.position, target.position) <= monster.AttackRange + 0.8f)
        target.GetComponent<Health>()?.TakeDamage(
            DamageInfo.Physical(monster.AttackDamage, monster.transform.position));

        elapsed = 0f;
        Vector3 recoverPosition = startPosition + direction * 0.25f;
        const float recoverDuration = 0.2f;

        while (elapsed < recoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverDuration;
            Vector3 position = Vector3.Lerp(lungePosition, recoverPosition, t);
            position.y = monster.GroundY;
            monster.transform.position = position;
            yield return null;
        }

        monster.transform.localScale = monster.DefaultScale;
        nextAttackTime = Time.time + monster.AttackCooldown;
        isAttacking = false;
    }
}
