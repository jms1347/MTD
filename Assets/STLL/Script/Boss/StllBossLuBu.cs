using Unity.Netcode;
using UnityEngine;

/// <summary>호로관 보스 — 여포 3페이즈.</summary>
public class StllBossLuBu : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> maxHealth = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<byte> phase = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float nextSkillTime;
    private float nextAmbushTime;
    private bool enraged;
    private bool isCasting;

    public float CurrentHealth => health.Value;
    public float MaxHealth => maxHealth.Value;
    public bool IsAlive => health.Value > 0f;
    public int Phase => phase.Value;

    public void ConfigureServer(float maxHp)
    {
        if (!IsServer)
            return;

        maxHealth.Value = maxHp;
        health.Value = maxHp;
        BuildVisual();
    }

    public void DamageServer(float amount)
    {
        if (!IsServer || !IsAlive || isCasting && enraged)
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        UpdatePhaseServer();

        if (health.Value <= 0f)
        {
            StllGoldDropper.ServerAwardKillGold(ulong.MaxValue, gameObject);
            NetworkObject.Despawn(true);
            StllRunController.Instance?.ServerNotifyBossDefeated();
        }
    }

    private void Update()
    {
        if (!IsServer || !IsAlive)
            return;

        if (Time.time >= nextAmbushTime)
        {
            nextAmbushTime = Time.time + GetAmbushInterval();
            TryAmbushServer();
        }

        if (Time.time >= nextSkillTime && !isCasting)
        {
            nextSkillTime = Time.time + Random.Range(4f, 6f);
            StartCoroutine(CastSkillRoutine());
        }
    }

    private float GetAmbushInterval()
    {
        return Phase switch
        {
            1 => 8f,
            2 => 5f,
            _ => 3f
        };
    }

    private void UpdatePhaseServer()
    {
        var ratio = health.Value / maxHealth.Value;
        var newPhase = ratio > 0.7f ? 1 : ratio > 0.4f ? 2 : 3;
        if (newPhase != phase.Value)
        {
            phase.Value = (byte)newPhase;
            if (newPhase == 3)
                enraged = true;
        }

        if (!enraged && ratio <= 0.2f)
            StartCoroutine(FinalBurstRoutine());
    }

    private System.Collections.IEnumerator FinalBurstRoutine()
    {
        enraged = true;
        isCasting = true;
        yield return new WaitForSeconds(5f);
        for (var i = 0; i < 2; i++)
        {
            SkillWildSwing();
            yield return new WaitForSeconds(0.5f);
        }

        isCasting = false;
    }

    private System.Collections.IEnumerator CastSkillRoutine()
    {
        isCasting = true;
        switch (Phase)
        {
            case 1:
                SkillDash();
                yield return new WaitForSeconds(1f);
                SkillSweep();
                break;
            case 2:
                SkillCrossStrike();
                yield return new WaitForSeconds(1.2f);
                SkillStomp();
                break;
            default:
                SkillWildSwing();
                yield return new WaitForSeconds(1.5f);
                SkillCharge();
                break;
        }

        isCasting = false;
    }

    private void SkillDash()
    {
        var target = GetRandomPlayer();
        if (target == null)
            return;

        var dir = (target.position - transform.position).normalized;
        transform.position += dir * 10f;
        DamagePlayersInRadiusServer(transform.position, 2f, 40f);
    }

    private void SkillSweep()
    {
        DamagePlayersInRadiusServer(transform.position, 5f, 35f);
    }

    private void SkillCrossStrike()
    {
        DamagePlayersInRadiusServer(transform.position, 6f, 45f);
    }

    private void SkillStomp()
    {
        DamagePlayersInRadiusServer(transform.position, 4f, 30f);
        StllZoneEffect.SpawnFireZoneServer(transform.position, 4f, 2f, 15f, ulong.MaxValue);
    }

    private void SkillWildSwing()
    {
        DamagePlayersInRadiusServer(transform.position, 7f, 25f);
    }

    private void SkillCharge()
    {
        var target = GetFarthestPlayer();
        if (target == null)
            return;

        var dir = (target.position - transform.position).normalized;
        transform.position += dir * 14f;
        DamagePlayersInRadiusServer(transform.position, 3f, 70f);
    }

    private void TryAmbushServer()
    {
        var target = GetRandomPlayer();
        if (target == null)
            return;

        transform.position = target.position - (target.position - transform.position).normalized * 2f;
        DamagePlayersInRadiusServer(transform.position, 2.5f, Phase >= 3 ? 60f : 50f);
    }

    private void DamagePlayersInRadiusServer(Vector3 center, float radius, float damage)
    {
        var players = FindObjectsByType<StllPlayerHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player == null || !player.IsAlive)
                continue;

            if (Vector3.Distance(center, player.transform.position) > radius)
                continue;

            player.DamageServer(damage);
        }
    }

    private Transform GetRandomPlayer()
    {
        var players = FindObjectsByType<StllBrotherhoodRoleState>(FindObjectsSortMode.None);
        if (players.Length == 0)
            return null;

        return players[Random.Range(0, players.Length)].transform;
    }

    private Transform GetFarthestPlayer()
    {
        var players = FindObjectsByType<StllBrotherhoodRoleState>(FindObjectsSortMode.None);
        Transform farthest = null;
        var maxDist = 0f;
        for (var i = 0; i < players.Length; i++)
        {
            var dist = Vector3.Distance(transform.position, players[i].transform.position);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthest = players[i].transform;
            }
        }

        return farthest;
    }

    private void BuildVisual()
    {
        StllVisualUtil.CreatePrimitive(PrimitiveType.Capsule, transform, new Vector3(0f, 1.8f, 0f),
            new Vector3(1.2f, 1.8f, 1.2f), new Color(0.75f, 0.12f, 0.1f));
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, transform, new Vector3(0f, 3.2f, 0f),
            new Vector3(0.8f, 0.3f, 0.5f), new Color(0.9f, 0.75f, 0.15f));

        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.height = 3.6f;
        collider.radius = 1f;
        collider.center = new Vector3(0f, 1.8f, 0f);
    }
}
