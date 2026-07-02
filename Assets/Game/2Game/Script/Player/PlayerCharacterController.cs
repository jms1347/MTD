using UnityEngine;

/// <summary>
/// 바닥 클릭 지점으로 이동하는 플레이어 캐릭터.
/// </summary>
public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float stopDistance = 0.18f;
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float rotateSpeed = 12f;
    [SerializeField] private float bodyRadius = 0.35f;
    [SerializeField] private float bodyHeight = 1.1f;

    private Vector3? moveTarget;
    private bool hasTarget;
    private float stuckTimer;
    private UnitGridNavigator navigator;

    public Vector3 Position => transform.position;
    public bool IsMoving => hasTarget;
    public float DrillRange => 1.35f;
    public float BodyRadius => bodyRadius;
    public float BodyHeight => bodyHeight;
    public float GroundY => groundY;

    private PlayerFarmDrillController drillController;
    private PlayerBuildController buildController;

    private void Awake()
    {
        drillController = GetComponent<PlayerFarmDrillController>();
        buildController = GetComponent<PlayerBuildController>();
        navigator = GetComponent<UnitGridNavigator>();
        if (navigator == null)
            navigator = gameObject.AddComponent<UnitGridNavigator>();
    }

    public bool TrySetMoveTarget(Vector3 worldPoint, bool isBuildControlled = false)
    {
        if (!isBuildControlled && buildController != null && buildController.IsWalkingToSite)
            return false;

        if (drillController != null && drillController.IsDrilling)
            return false;

        if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerMove())
            return false;

        var target = worldPoint;
        target.y = groundY;

        if (!FarmZone.CanMoveBetween(transform.position, target))
            return false;

        moveTarget = target;
        hasTarget = true;
        stuckTimer = 0f;
        return true;
    }

    public void SetMoveTarget(Vector3 worldPoint, bool isBuildControlled = false)
    {
        if (TrySetMoveTarget(worldPoint, isBuildControlled))
        {
            if (!isBuildControlled)
                PlayerMoveMarker.Show(worldPoint);
        }
    }

    public void StopMovement()
    {
        hasTarget = false;
        moveTarget = null;
        stuckTimer = 0f;
        navigator?.ClearPath();
    }

    public void PlaceAtWorld(Vector3 worldPosition)
    {
        var pos = worldPosition;
        pos.y = groundY;
        transform.position = pos;
        StopMovement();
    }

    public bool TryResolveUnstuck(float searchRadius = 2.8f)
    {
        if (!UnitMovementCollision.IsPositionBlocked(transform.position, bodyRadius, bodyHeight, groundY))
            return false;

        if (!UnitMovementCollision.TryFindNearestFreePosition(
                transform.position,
                bodyRadius,
                bodyHeight,
                groundY,
                searchRadius,
                out Vector3 freePosition))
            return false;

        PlaceAtWorld(freePosition);
        return true;
    }

    public float DistanceTo(Vector3 worldPoint)
    {
        Vector3 flat = worldPoint - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }

    private void Update()
    {
        if (drillController != null && drillController.IsDrilling)
            return;

        if (!hasTarget || !moveTarget.HasValue)
            return;

        Vector3 current = transform.position;
        Vector3 target = moveTarget.Value;
        Vector3 toTarget = target - current;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
        {
            current.x = target.x;
            current.z = target.z;
            current.y = groundY;
            transform.position = current;
            hasTarget = false;
            return;
        }

        Vector3 direction = toTarget / distance;
        Vector3 next = navigator != null
            ? navigator.MoveTowards(current, target, moveSpeed, bodyRadius, bodyHeight, groundY)
            : UnitMovementCollision.MoveWithCollision(current, current + direction * (moveSpeed * Time.deltaTime), bodyRadius, bodyHeight, groundY);

        if ((next - current).sqrMagnitude < 0.000001f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 0.4f)
            {
                if (navigator != null)
                    navigator.ClearPath();

                hasTarget = false;
                stuckTimer = 0f;
            }

            return;
        }

        stuckTimer = 0f;

        Vector3 moveDir = next - current;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir.normalized),
                Time.deltaTime * rotateSpeed);
        }

        transform.position = next;
    }

    public static PlayerCharacterController Create(Vector3 spawnPosition)
    {
        var root = new GameObject("Player");
        root.tag = "Player";
        root.transform.position = spawnPosition;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.5f, 0.55f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        head.transform.localScale = Vector3.one * 0.38f;

        var bodyColor = new Color(0.28f, 0.62f, 0.95f);
        var skinColor = new Color(0.96f, 0.82f, 0.68f);
        ApplyColor(body, bodyColor);
        ApplyColor(head, skinColor);

        Destroy(body.GetComponent<Collider>());
        Destroy(head.GetComponent<Collider>());

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.55f, 0f);
        capsule.radius = 0.35f;
        capsule.height = 1.1f;

        var controller = root.AddComponent<PlayerCharacterController>();
        root.AddComponent<UnitGridNavigator>();
        root.AddComponent<PlayerDrillShake>();
        root.AddComponent<PlayerFarmDrillController>();
        root.AddComponent<PlayerBuildController>();
        root.AddComponent<DefensePlayerInput>();
        return controller;
    }

    private static void ApplyColor(GameObject part, Color color)
    {
        var renderer = part.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
    }
}
