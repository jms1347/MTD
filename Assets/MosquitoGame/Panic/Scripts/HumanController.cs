using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class HumanController : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(PanicGameConstants.HumanMaxHealth);

    private CharacterController characterController;
    private Camera firstPersonCamera;
    private HumanGun gun;
    private HumanHeartbeatRadar heartbeatRadar;
    private HumanVisualBuilder visualBuilder;
    private HumanTargetOutline targetOutline;
    private MobileDualTouchInput touchInput = new();

    private float yaw;
    private float pitch;
    private float heartbeatIntensity;
    private float heartbeatSin;
    private float revealTimer;
    private Vector3 revealPosition;
    private PanicTrapType selectedTrap = PanicTrapType.MosquitoCoil;

    public bool IsLocalOwner => NetworkManager.Singleton == null || base.IsOwner;
    public bool IsAlive => health.Value > 0f;
    public float CurrentHealth => health.Value;
    public Camera FirstPersonCamera => firstPersonCamera;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.32f;
        characterController.center = new Vector3(0f, 0.92f, 0f);
        tag = PanicGameConstants.HumanTag;

        EnsureNetworkTransform();
        BuildFirstPersonCamera();
        EnsureHumanVisual();

        gun = gameObject.AddComponent<HumanGun>();
        HumanGun.CreateMuzzle(transform, firstPersonCamera);
        heartbeatRadar = gameObject.AddComponent<HumanHeartbeatRadar>();
        heartbeatRadar.Bind(this);
    }

    private void EnsureNetworkTransform()
    {
        if (GetComponent<NetworkTransform>() == null)
            gameObject.AddComponent<NetworkTransform>();
    }

    private void BuildFirstPersonCamera()
    {
        var cameraGo = new GameObject("HumanCamera");
        cameraGo.transform.SetParent(transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        firstPersonCamera = cameraGo.AddComponent<Camera>();
        firstPersonCamera.nearClipPlane = 0.05f;
        firstPersonCamera.fieldOfView = 75f;
        cameraGo.AddComponent<AudioListener>();
        PanicVisionLayers.ApplyHumanCameraCulling(firstPersonCamera);
    }

    public override void OnNetworkSpawn()
    {
        EnsureHumanVisual();
        PanicGameManager.Instance?.RegisterHuman(this);

        HumanTargetRegistry.Instance?.RegisterHuman(this);

        if (!IsLocalOwner)
        {
            firstPersonCamera.enabled = false;
            var listener = firstPersonCamera.GetComponent<AudioListener>();
            if (listener != null)
                Destroy(listener);
            return;
        }

        firstPersonCamera.enabled = true;
        PanicVisionLayers.ApplyHumanCameraCulling(firstPersonCamera);
    }

    public override void OnNetworkDespawn()
    {
        HumanTargetRegistry.Instance?.UnregisterHuman(this);
    }

    public Vector3 GetAimPoint()
    {
        if (visualBuilder != null && visualBuilder.BodyRoot != null)
            return visualBuilder.BodyRoot.position + Vector3.up * 0.9f;

        return transform.position + Vector3.up * 1.5f;
    }

    public Vector3 GetNearestAttachPoint(Vector3 fromPosition)
    {
        EnsureHumanVisual();
        return visualBuilder != null
            ? visualBuilder.GetNearestAttachPoint(fromPosition)
            : GetAimPoint();
    }

    public Transform EnsureMosquitoVisibleOutline()
    {
        EnsureHumanVisual();
        visualBuilder?.SetOutlineVisible(true);
        return visualBuilder != null ? visualBuilder.OutlineRoot : null;
    }

    private void EnsureHumanVisual()
    {
        if (visualBuilder == null)
            visualBuilder = GetComponent<HumanVisualBuilder>() ?? gameObject.AddComponent<HumanVisualBuilder>();

        visualBuilder.Build();

        if (targetOutline == null)
            targetOutline = GetComponent<HumanTargetOutline>() ?? gameObject.AddComponent<HumanTargetOutline>();

        targetOutline.Build();
    }

    private void Update()
    {
        if (!IsLocalOwner || !IsAlive)
            return;

        touchInput.Update(enableTouch: Application.isMobilePlatform, enableKeyboardFallback: !Application.isMobilePlatform);
        HandleLook();
        HandleMove();
        HandleTrapPlacement();
        HandleTrapHotkeys();

        if (touchInput.FirePressed)
            gun.TryFire(firstPersonCamera);

        if (revealTimer > 0f)
            revealTimer -= Time.deltaTime;

        ReportMissionHold();
    }

    private void ReportMissionHold()
    {
        if (!IsLocalOwner || PanicGameManager.Instance == null)
            return;

        var holding = PanicGameManager.Instance.IsPlay && IsHoldingInteract();
        if (IsServer)
            ApplyMissionHoldLocal(Time.deltaTime, holding);
        else
            ReportMissionHoldServerRpc(transform.position, Time.deltaTime, holding);
    }

    [ServerRpc]
    private void ReportMissionHoldServerRpc(Vector3 humanPosition, float deltaSeconds, bool holding)
    {
        ApplyMissionHoldLocal(deltaSeconds, holding, humanPosition);
    }

    private void ApplyMissionHoldLocal(float deltaSeconds, bool holding, Vector3? humanPosition = null)
    {
        var position = humanPosition ?? transform.position;
        var missions = FindObjectsByType<MissionObject>(FindObjectsSortMode.None);
        var holdingAny = false;

        foreach (var mission in missions)
        {
            if (mission == null || !mission.IsHumanInRange(position))
                continue;

            if (holding && PanicGameManager.Instance != null && PanicGameManager.Instance.IsPlay)
            {
                mission.ReportHold(deltaSeconds);
                holdingAny = true;
            }
        }

        if (!holdingAny && PanicGameManager.Instance != null && PanicGameManager.Instance.IsPlay)
        {
            foreach (var mission in missions)
            {
                if (mission != null)
                    mission.ReportHoldDecay(deltaSeconds);
            }
        }
    }

    public bool IsHoldingInteract()
    {
        if (Input.GetKey(KeyCode.F))
            return true;

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.position.x < Screen.width * 0.5f && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                return true;
        }

        return false;
    }

    private void HandleLook()
    {
        yaw += touchInput.LookDelta.x * PanicGameConstants.HumanLookSensitivity;
        pitch = Mathf.Clamp(pitch - touchInput.LookDelta.y * PanicGameConstants.HumanLookSensitivity, -85f, 85f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        var move = new Vector3(touchInput.MoveDelta.x, 0f, touchInput.MoveDelta.y);
        if (move.sqrMagnitude < 0.01f)
            move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        move = transform.TransformDirection(Vector3.ClampMagnitude(move, 1f)) * PanicGameConstants.HumanMoveSpeed;
        move.y = -4f;
        characterController.Move(move * Time.deltaTime);
    }

    private void HandleTrapPlacement()
    {
        if (!PanicGameManager.Instance || !PanicGameManager.Instance.IsPrep || TrapManager.Instance == null)
            return;

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        var position = transform.position + firstPersonCamera.transform.forward * 1.2f;
        if (IsServer)
            TrapManager.Instance?.RequestPlaceTrap(selectedTrap, position, Quaternion.identity);
        else
            PlaceTrapServerRpc(selectedTrap, position, Quaternion.identity);
    }

    [ServerRpc]
    private void PlaceTrapServerRpc(PanicTrapType trapType, Vector3 position, Quaternion rotation)
    {
        TrapManager.Instance?.RequestPlaceTrap(trapType, position, rotation);
    }

    private void HandleTrapHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedTrap = PanicTrapType.MosquitoCoil;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedTrap = PanicTrapType.StickyPad;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedTrap = PanicTrapType.DecoyHuman;
    }

    public void ReceiveBloodTick(ulong mosquitoClientId)
    {
        if (!IsServer)
            return;

        health.Value = Mathf.Max(0f, health.Value - 1f);
        if (health.Value <= 0f)
            PanicGameManager.Instance?.NotifyHumanHealthDepleted();
    }

    public void ShowMosquitoReveal(Vector3 mosquitoPosition, float durationSeconds)
    {
        if (!IsLocalOwner)
            return;

        revealPosition = mosquitoPosition;
        revealTimer = durationSeconds;
    }

    public void SetHeartbeatIntensity(float intensity, float sinWave)
    {
        if (!IsLocalOwner)
            return;

        heartbeatIntensity = intensity;
        heartbeatSin = sinWave;
        PanicAudioCue.PlayHeartbeat(intensity);
    }

    private void OnGUI()
    {
        if (!IsLocalOwner)
            return;

        DrawHeartbeat();
        if (revealTimer > 0f)
            DrawRevealMarker();
    }

    private void DrawHeartbeat()
    {
        if (heartbeatIntensity <= 0.01f)
            return;

        var size = Mathf.Lerp(36f, 84f, heartbeatIntensity) * (1f + heartbeatSin * 0.18f);
        var rect = new Rect(Screen.width * 0.5f - size * 0.5f, Screen.height * 0.5f - size * 0.5f, size, size);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * 0.75f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = new Color(1f, 0.2f, 0.25f, Mathf.Lerp(0.35f, 1f, heartbeatIntensity));
        GUI.Label(rect, "♥", style);
    }

    private void DrawRevealMarker()
    {
        if (firstPersonCamera == null)
            return;

        var screen = firstPersonCamera.WorldToScreenPoint(revealPosition);
        if (screen.z < 0f)
            return;

        var rect = new Rect(screen.x - 20f, Screen.height - screen.y - 20f, 40f, 40f);
        var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.red;
        GUI.Box(rect, "모기!", style);
    }
}
