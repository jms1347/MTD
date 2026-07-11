using Unity.Netcode;
using UnityEngine;

/// <summary>
/// WASD = 카메라 기준 이동. Owner는 로컬 즉시 반영 + 서버 동기화.
/// </summary>
[DefaultExecutionOrder(-10)]
public class StllMountedInput : NetworkBehaviour
{
    private const float RmbSteerHoldSeconds = 0.12f;
    private const float RmbTapMaxSeconds = 0.28f;

    private StllHorseMotor motor;
    private StllMountedCharge charge;
    private StllGlaiveCombat combat;
    private StllMinionCommander commander;
    private Camera playerCamera;

    private float rmbDownTime;
    private bool rmbSteerActive;

    private void Awake()
    {
        motor = GetComponent<StllHorseMotor>();
        charge = GetComponent<StllMountedCharge>();
        combat = GetComponent<StllGlaiveCombat>();
        commander = GetComponent<StllMinionCommander>();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        HandleDrive();
        HandleActions();
    }

    private void SubmitSteerDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();
        motor.SetSteerInput(direction, true);

        if (!IsServer)
            motor.SubmitSteerDirectionServerRpc(direction, true);
    }

    private void ReleaseSteer()
    {
        motor.ReleaseSteerInput();

        if (!IsServer)
            motor.SubmitReleaseSteerServerRpc();
    }

    private void HandleDrive()
    {
        if (motor == null)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            rmbDownTime = Time.time;
            rmbSteerActive = false;
        }

        if (Input.GetMouseButton(1))
        {
            if (Time.time - rmbDownTime >= RmbSteerHoldSeconds)
            {
                rmbSteerActive = true;
                if (TryGetGroundSteerDirection(out var mouseDirection))
                    SubmitSteerDirection(mouseDirection);
            }

            return;
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (!rmbSteerActive && Time.time - rmbDownTime <= RmbTapMaxSeconds)
                RequestChargeSpinServerRpc();

            ReleaseSteer();
            rmbSteerActive = false;
            return;
        }

        var wasd = ReadWasdDirection();
        if (wasd.sqrMagnitude > 0.01f)
        {
            SubmitSteerDirection(wasd);
            return;
        }

        ReleaseSteer();
    }

    private Vector3 ReadWasdDirection()
    {
        var forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        var right = playerCamera != null ? playerCamera.transform.right : transform.right;
        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();
        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        var dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            dir += forward;
        if (Input.GetKey(KeyCode.S))
            dir -= forward;
        if (Input.GetKey(KeyCode.A))
            dir -= right;
        if (Input.GetKey(KeyCode.D))
            dir += right;

        return dir;
    }

    private bool TryGetGroundSteerDirection(out Vector3 direction)
    {
        direction = default;
        if (playerCamera == null)
            return false;

        var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, transform.position);
        if (!plane.Raycast(ray, out var distance))
            return false;

        var point = ray.GetPoint(distance);
        var toPoint = point - transform.position;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude < 0.04f)
            return false;

        direction = toPoint.normalized;
        return true;
    }

    private void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            RequestChargeServerRpc();

        if (Input.GetKeyDown(KeyCode.F))
            RequestToggleMinionModeServerRpc();

        if (Input.GetMouseButtonDown(0))
            RequestBasicAttackServerRpc();
    }

    [ServerRpc]
    private void RequestChargeServerRpc()
    {
        charge?.TryStartChargeServer();
    }

    [ServerRpc]
    private void RequestToggleMinionModeServerRpc()
    {
        commander?.ToggleModeServer();
    }

    [ServerRpc]
    private void RequestBasicAttackServerRpc()
    {
        combat?.TryBasicAttackServer();
    }

    [ServerRpc]
    private void RequestChargeSpinServerRpc()
    {
        combat?.TryChargeSpinServer();
    }
}
