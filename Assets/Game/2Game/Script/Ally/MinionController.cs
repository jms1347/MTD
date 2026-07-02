using System.Collections;
using UnityEngine;

/// <summary>
/// 소환 타워가 뽑는 근접 아군 유닛. 적을 찾아 돌진 공격합니다.
/// </summary>
[RequireComponent(typeof(Health))]
public class MinionController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float groundY = 0.45f;

    private bool isAttacking;
    private float nextAttackTime;
    private Transform attackTarget;
    private Vector3 defaultScale;
    private SummonTowerController ownerTower;
    private UnitGridNavigator navigator;

    public void Initialize(SummonTowerController owner, float health, float speed, float damage)
    {
        ownerTower = owner;
        moveSpeed = speed;
        attackDamage = damage;
        groundY = transform.localScale.x * 0.5f;

        var healthComponent = GetComponent<Health>();
        healthComponent.Initialize(health);
        healthComponent.SetDestroyOnDeath(false);
        healthComponent.OnDeath -= HandleDeath;
        healthComponent.OnDeath += HandleDeath;
    }

    private void Awake()
    {
        defaultScale = transform.localScale;
        navigator = GetComponent<UnitGridNavigator>();
        if (navigator == null)
            navigator = gameObject.AddComponent<UnitGridNavigator>();
    }

    private void OnEnable()
    {
        isAttacking = false;
        nextAttackTime = 0f;
        attackTarget = null;
    }

    private void Update()
    {
        if (isAttacking)
            return;

        attackTarget = FindNearestEnemy();
        if (attackTarget == null)
            return;

        Vector3 toTarget = attackTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(ChargeAttack(toTarget.normalized, attackTarget));
            return;
        }

        if (distance <= attackRange)
            return;

        Vector3 direction = toTarget / distance;
        float radius = transform.localScale.x * 0.5f;
        Vector3 current = transform.position;
        Vector3 nextPosition = navigator.MoveTowards(
            current,
            attackTarget.position,
            moveSpeed,
            radius,
            radius * 2f,
            groundY);
        transform.position = nextPosition;
        Vector3 moveDir = nextPosition - current;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.0001f)
            direction = moveDir.normalized;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 10f);
    }

    private IEnumerator ChargeAttack(Vector3 direction, Transform target)
    {
        isAttacking = true;
        Vector3 start = transform.position;
        start.y = groundY;

        float elapsed = 0f;
        const float lungeDuration = 0.16f;
        Vector3 lungeEnd = start + direction * 0.9f;

        while (elapsed < lungeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lungeDuration;
            Vector3 pos = Vector3.Lerp(start, lungeEnd, t);
            pos.y = groundY;
            transform.position = pos;
            yield return null;
        }

        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange + 0.6f)
            target.GetComponent<Health>()?.TakeDamage(
                DamageInfo.Physical(attackDamage, target.position));

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = float.MaxValue;
        Vector3 origin = transform.position;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, requireLanded: true))
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void HandleDeath()
    {
        ownerTower?.ReleaseMinion(gameObject);
    }
}
