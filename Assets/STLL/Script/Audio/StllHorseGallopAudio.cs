using Unity.Netcode;
using UnityEngine;

/// <summary>말 발굽 — StllHorseMotor 속도에 따라 pitch/볼륨 조절.</summary>
[RequireComponent(typeof(AudioSource))]
public class StllHorseGallopAudio : MonoBehaviour
{
    private const float MinPlaySpeed = 0.45f;
    private const float StopSpeed = 0.25f;
    private const float MinPitch = 0.72f;
    private const float MaxPitch = 1.95f;
    private const float BaseVolume = 0.62f;

    [SerializeField] private AudioClip gallopClip;

    private AudioSource source;
    private StllHorseMotor motor;
    private NetworkObject networkObject;
    private Vector3 lastRootPosition;

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

        motor = GetComponentInParent<StllHorseMotor>();
        networkObject = GetComponentInParent<NetworkObject>();
        lastRootPosition = transform.root.position;
    }

    private void Start()
    {
        EnsureClip();
        ApplySpatialSettings();
    }

    public void AssignClip(AudioClip clip)
    {
        if (clip == null)
            return;

        gallopClip = clip;
        source.clip = clip;
    }

    private void Update()
    {
        EnsureClip();
        if (source.clip == null)
            return;

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

    private void EnsureClip()
    {
        if (source.clip != null)
            return;

        if (gallopClip == null)
            gallopClip = StllHorseGallopAudioUtil.ResolveClip();

        if (gallopClip != null)
            source.clip = gallopClip;
    }

    private void ApplySpatialSettings()
    {
        var isLocalOwner = networkObject != null && networkObject.IsOwner;
        source.spatialBlend = isLocalOwner ? 0.25f : 1f;
        source.volume = BaseVolume;
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

        if (motor != null && motor.CurrentSpeed > estimatedSpeed)
            return motor.CurrentSpeed;

        return estimatedSpeed;
    }
}
