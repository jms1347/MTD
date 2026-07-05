using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 스턴 — 이동 정지 + 클라이언트 피드백(사운드·질주자 VFX).
/// </summary>
public class CwslPlayerStun : NetworkBehaviour
{
    private readonly NetworkVariable<float> syncedStunEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerMovement movement;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerHealth playerHealth;

    public bool IsStunned => syncedStunEndTime.Value > Time.time;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
        playerHealth = GetComponent<CwslPlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;
    }

    public override void OnNetworkDespawn()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
    }

    public void ApplyStunServer(float duration, Vector3 impactPosition, CwslStunSource source = CwslStunSource.Rammer)
    {
        if (!IsServer || duration <= 0f)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
            return;

        syncedStunEndTime.Value = Time.time + duration;
        StopMovementServer();
        rammerSkill?.StopMomentumForStunServer();
        PlayStunFeedbackClientRpc(impactPosition, (byte)source);
    }

    public void ClearStunServer()
    {
        if (!IsServer)
            return;

        syncedStunEndTime.Value = 0f;
    }

    private void StopMovementServer()
    {
        movement?.StopMovement();

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void HandleDied()
    {
        if (!IsServer)
            return;

        ClearStunServer();
    }

    [ClientRpc]
    private void PlayStunFeedbackClientRpc(Vector3 impactPosition, byte stunSourceRaw)
    {
        if ((CwslStunSource)stunSourceRaw == CwslStunSource.Lightning)
        {
            GetComponent<CwslPlayerLightningStunVisual>()?.PlayLightningStunVfx(impactPosition);
            return;
        }

        CwslRammerStunFeedback.PlaySound(impactPosition);

        var character = GetComponent<CwslPlayerCharacter>();
        if (character != null && character.CharacterId == CwslCharacterId.MomentumRammer)
            transform.Find("Visual")?.GetComponent<CwslPlayerRammerStunVisual>()?.PlayStunVfx(impactPosition);
    }
}
