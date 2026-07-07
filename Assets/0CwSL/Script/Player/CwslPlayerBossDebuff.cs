using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>홍명보 보스 스킬 디버프 — 방향 반전·감염·안전지대 무적.</summary>
public class CwslPlayerBossDebuff : NetworkBehaviour
{
    private readonly NetworkVariable<float> reverseControlEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> infectedEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> safeZoneCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Coroutine infectedRoutine;
    private Transform infectedGlow;

    public bool IsReverseControl => Time.time < reverseControlEndTime.Value;
    public bool IsInfected => Time.time < infectedEndTime.Value;
    public bool IsInBossSafeZone => safeZoneCount.Value > 0;
    public bool IsInvincibleToBossBarrage => IsInBossSafeZone;

    public void ApplyReverseControlServer(float durationSeconds)
    {
        if (!IsServer)
            return;

        reverseControlEndTime.Value = Mathf.Max(reverseControlEndTime.Value, Time.time + durationSeconds);
        NotifyReverseControlClientRpc(durationSeconds);
    }

    public void ApplyInfectedServer(float durationSeconds)
    {
        if (!IsServer)
            return;

        infectedEndTime.Value = Mathf.Max(infectedEndTime.Value, Time.time + durationSeconds);
        NotifyInfectedClientRpc(durationSeconds);

        if (infectedRoutine != null)
            StopCoroutine(infectedRoutine);
        infectedRoutine = StartCoroutine(InfectedSpikeRoutine(durationSeconds));
    }

    public void EnterBossSafeZoneServer()
    {
        if (!IsServer)
            return;

        safeZoneCount.Value++;
    }

    public void ExitBossSafeZoneServer()
    {
        if (!IsServer || safeZoneCount.Value <= 0)
            return;

        safeZoneCount.Value--;
    }

    public static Vector3 ApplyReverseControlIfNeeded(Vector3 worldPoint, Transform playerTransform, CwslPlayerBossDebuff debuff)
    {
        if (debuff == null || !debuff.IsReverseControl || playerTransform == null)
            return worldPoint;

        var delta = worldPoint - playerTransform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0001f)
            return worldPoint;

        return playerTransform.position - delta;
    }

    private IEnumerator InfectedSpikeRoutine(float durationSeconds)
    {
        var endTime = Time.time + durationSeconds;
        while (Time.time < endTime)
        {
            FireInfectedSpikesServer();
            yield return new WaitForSeconds(CwslGameConstants.BossInfectedSpikeInterval);
        }

        infectedRoutine = null;
    }

    private void FireInfectedSpikesServer()
    {
        if (!IsServer || !IsInfected)
            return;

        var origin = transform.position + Vector3.up * 0.6f;
        var directions = new[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        foreach (var dir in directions)
            CwslBossSkillProjectile.SpawnServer(
                origin,
                dir,
                CwslBossSkillProjectileKind.InfectedSpike,
                OwnerClientId);
    }

    private void Update()
    {
        if (IsInfected)
            EnsureInfectedGlow();
        else
            ClearInfectedGlow();
    }

    private void EnsureInfectedGlow()
    {
        if (infectedGlow != null)
            return;

        var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "InfectedGlow";
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        glow.transform.localScale = Vector3.one * 1.6f;
        Object.Destroy(glow.GetComponent<Collider>());
        var renderer = glow.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.95f, 0.12f, 0.1f, 0.35f);
            renderer.material.SetFloat("_Surface", 1f);
        }

        infectedGlow = glow.transform;
    }

    private void ClearInfectedGlow()
    {
        if (infectedGlow == null)
            return;

        if (infectedGlow.gameObject != null)
            Destroy(infectedGlow.gameObject);
        infectedGlow = null;
    }

    [ClientRpc]
    private void NotifyReverseControlClientRpc(float duration)
    {
        CwslSimpleVfx.SpawnBurst(transform.position + Vector3.up, new Color(0.9f, 0.2f, 0.15f), 1.2f, 0.25f);
    }

    [ClientRpc]
    private void NotifyInfectedClientRpc(float duration)
    {
        CwslSimpleVfx.SpawnBurst(transform.position + Vector3.up, new Color(0.85f, 0.05f, 0.05f), 1.8f, 0.35f);
    }
}
