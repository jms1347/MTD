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

    public event System.Action<bool> OnStunStateChanged;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
        playerHealth = GetComponent<CwslPlayerHealth>();

        syncedStunEndTime.OnValueChanged += HandleStunEndTimeChanged;

        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;

        NotifyStunStateChanged(IsStunned);
    }

    public override void OnNetworkDespawn()
    {
        syncedStunEndTime.OnValueChanged -= HandleStunEndTimeChanged;

        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        var stunned = IsStunned;
        if (stunned == lastReportedStunned)
            return;

        lastReportedStunned = stunned;
        NotifyStunStateChanged(stunned);
    }

    private bool lastReportedStunned;

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

        if (syncedStunEndTime.Value <= 0f)
            return;

        syncedStunEndTime.Value = 0f;
    }

    private void HandleStunEndTimeChanged(float previous, float current)
    {
        var wasStunned = previous > Time.time;
        var isStunned = current > Time.time;
        if (wasStunned == isStunned)
            return;

        lastReportedStunned = isStunned;
        NotifyStunStateChanged(isStunned);
    }

    private void NotifyStunStateChanged(bool stunned)
    {
        OnStunStateChanged?.Invoke(stunned);
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
            GetComponentInChildren<CwslPlayerRammerStunVisual>(true)?.PlayStunVfx(impactPosition);
    }
}
