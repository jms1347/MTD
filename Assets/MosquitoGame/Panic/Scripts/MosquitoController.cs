using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class MosquitoController : NetworkBehaviour
{
    private readonly NetworkVariable<float> syncedHealth = new(100f);

    private Rigidbody body;
    private MosquitoHumanTracker tracker;
    private MobileDualTouchInput touchInput = new();
    private readonly HashSet<MosquitoCoilTrap> activeCoils = new();

    private float yaw;
    private float pitch = 12f;
    private float stickyTimer;
    private float coilJitterSeed;
    private bool isDead;
    private MosquitoBloodSuck bloodSuck;

    public bool IsAlive => !isDead && syncedHealth.Value > 0f;
    public bool IsAttached => bloodSuck != null && bloodSuck.IsAttached;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.linearDamping = 2.2f;
        body.angularDamping = 3f;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (GetComponent<NetworkTransform>() == null)
            gameObject.AddComponent<NetworkTransform>();

        MosquitoVisualBuilder.Build(transform);
        tracker = gameObject.AddComponent<MosquitoHumanTracker>();
        tracker.EnsureCamera();
        bloodSuck = gameObject.AddComponent<MosquitoBloodSuck>();
        tag = PanicGameConstants.MosquitoTag;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            PanicGameManager.Instance?.RegisterMosquitoSpawned();

        tracker?.EnsureCamera();
        tracker?.SetOwnerCameraActive(IsOwner);

        if (IsOwner)
            HumanTargetRegistry.Instance?.GetPrimaryHuman()?.EnsureMosquitoVisibleOutline();
    }

    private void Update()
    {
        if (!IsOwner || !IsAlive)
            return;

        // 부착 중에는 대시/호버 중단, 흡혈만 유지
        if (IsAttached)
        {
            if (stickyTimer > 0f)
                stickyTimer -= Time.deltaTime;

            touchInput.Update(enableTouch: Application.isMobilePlatform, enableKeyboardFallback: !Application.isMobilePlatform);
            if (touchInput.FirePressed || Input.GetKeyDown(KeyCode.Escape))
                bloodSuck?.Detach();
            return;
        }

        if (stickyTimer > 0f)
        {
            stickyTimer -= Time.deltaTime;
            return;
        }

        touchInput.Update(enableTouch: Application.isMobilePlatform, enableKeyboardFallback: !Application.isMobilePlatform);
        yaw += touchInput.LookDelta.x;
        pitch = Mathf.Clamp(pitch - touchInput.LookDelta.y, -20f, 65f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (touchInput.MoveDelta.sqrMagnitude > 0.04f)
            TryDash(touchInput.MoveDelta);

        if (touchInput.DashPressed)
            TryDash(transform.forward);

        if (touchInput.FirePressed)
            bloodSuck?.TryStartSuck();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !IsAlive || IsAttached)
            return;

        if (stickyTimer > 0f)
        {
            body.linearVelocity = Vector3.zero;
            return;
        }

        body.AddForce(Vector3.up * PanicGameConstants.MosquitoHoverForce, ForceMode.Acceleration);

        if (activeCoils.Count > 0)
            ApplyCoilForces();
        else
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, PanicGameConstants.MosquitoMaxSpeed);
    }

    private void LateUpdate()
    {
        if (!IsOwner || tracker == null)
            return;

        tracker.UpdateCamera(pitch, yaw);
    }

    private void TryDash(Vector2 moveDelta)
    {
        var direction = new Vector3(moveDelta.x, 0f, moveDelta.y);
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        direction = transform.TransformDirection(direction.normalized);
        direction.y = Input.GetKey(KeyCode.Space) ? 0.35f : 0f;

        var impulse = PanicGameConstants.MosquitoDashImpulse;
        if (activeCoils.Count > 0)
            impulse *= PanicGameConstants.CoilSlowMultiplier;

        body.AddForce(direction * impulse, ForceMode.Impulse);
    }

    private void ApplyCoilForces()
    {
        coilJitterSeed += Time.fixedDeltaTime * 24f;
        var jitter = new Vector3(
            Mathf.PerlinNoise(coilJitterSeed, 0.1f) - 0.5f,
            0f,
            Mathf.PerlinNoise(0.2f, coilJitterSeed) - 0.5f) * PanicGameConstants.CoilJitterStrength;

        body.AddForce(jitter, ForceMode.Acceleration);
        body.linearVelocity = Vector3.ClampMagnitude(
            body.linearVelocity * PanicGameConstants.CoilSlowMultiplier,
            PanicGameConstants.MosquitoMaxSpeed * PanicGameConstants.CoilSlowMultiplier);
    }

    public void ApplyCoilEffect(MosquitoCoilTrap coil) => activeCoils.Add(coil);
    public void ClearCoilEffect(MosquitoCoilTrap coil) => activeCoils.Remove(coil);

    public void ApplyStickyStun(float seconds)
    {
        if (!IsOwner)
            return;

        stickyTimer = Mathf.Max(stickyTimer, seconds);
    }

    public void ReceiveGunHit(float damage, ulong attackerClientId)
    {
        if (!IsServer || !IsAlive)
            return;

        syncedHealth.Value = Mathf.Max(0f, syncedHealth.Value - damage);
        if (syncedHealth.Value <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        PanicGameManager.Instance?.NotifyMosquitoEliminated();
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
