using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자 말발굽 — 이동 속도에 따라 루프 pitch/볼륨 조절.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CwslPlayerHorseGallopAudio : MonoBehaviour
{
    private const float MinPlaySpeed = 0.45f;
    private const float StopSpeed = 0.25f;
    private const float MinPitch = 0.72f;
    private const float MaxPitch = 1.95f;
    private const float BaseVolume = 0.62f;

    private AudioSource source;
    private NetworkObject networkObject;
    private CwslPlayerStun playerStun;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private bool clipAssigned;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 1.2f;
        source.maxDistance = 28f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        BindReferences();
    }

    private void Start()
    {
        BindReferences();
        EnsureClip();
        ApplySpatialSettings();
    }

    private void BindReferences()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        rammerSkill = GetComponentInParent<CwslMomentumRammerSkill>();
        playerStun = GetComponentInParent<CwslPlayerStun>();
        movement = GetComponentInParent<CwslPlayerMovement>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void ApplySpatialSettings()
    {
        var isLocalOwner = networkObject != null && networkObject.IsOwner;
        source.spatialBlend = isLocalOwner ? 0.25f : 1f;
        source.volume = BaseVolume;
    }

    private void Update()
    {
        EnsureClip();

        if (source.clip == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            StopGallop();
            return;
        }

        if (rammerSkill != null && rammerSkill.IsStunned)
        {
            StopGallop();
            return;
        }

        if (playerStun != null && playerStun.IsStunned)
        {
            StopGallop();
            return;
        }

        var speed = ReadSpeed();
        if (speed < StopSpeed)
        {
            StopGallop();
            return;
        }

        if (speed < MinPlaySpeed)
        {
            if (source.isPlaying)
                source.volume = Mathf.MoveTowards(source.volume, 0f, Time.deltaTime * 4f);
            if (source.volume <= 0.01f)
                source.Stop();
            return;
        }

        var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        var targetPitch = Mathf.Lerp(MinPitch, MaxPitch, speedRatio);
        var targetVolume = BaseVolume * Mathf.Lerp(0.55f, 1f, speedRatio);

        if (!source.isPlaying)
        {
            source.pitch = targetPitch;
            source.volume = targetVolume;
            source.Play();
        }
        else
        {
            source.pitch = Mathf.Lerp(source.pitch, targetPitch, Time.deltaTime * 8f);
            source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 6f);
        }
    }

    public void AssignClip(AudioClip clip)
    {
        if (clip == null)
            return;

        source.clip = clip;
        clipAssigned = true;
    }

    private void EnsureClip()
    {
        if (clipAssigned && source.clip != null)
            return;

        var clip = CwslRammerAudioFeedback.ResolveHorseGallopClip();
        if (clip == null)
            return;

        source.clip = clip;
        clipAssigned = true;
    }

    private void StopGallop()
    {
        if (!source.isPlaying)
            return;

        source.Stop();
        source.volume = BaseVolume;
    }

    private float ReadSpeed()
    {
        var root = transform.root;
        var flatDelta = root.position - lastRootPosition;
        flatDelta.y = 0f;
        lastRootPosition = root.position;
        var estimatedSpeed = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;

        if (rammerSkill != null && rammerSkill.CurrentSpeed > estimatedSpeed)
            return rammerSkill.CurrentSpeed;
        if (movement != null && movement.CurrentMoveSpeed > estimatedSpeed)
            return movement.CurrentMoveSpeed;
        if (agent != null && agent.enabled && agent.velocity.magnitude > estimatedSpeed)
            return agent.velocity.magnitude;
        return estimatedSpeed;
    }
}
