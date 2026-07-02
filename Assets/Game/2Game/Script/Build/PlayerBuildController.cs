using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건설 지점으로 이동한 뒤 골격이 올라가며 건물을 짓습니다. 건설 시간(초) = 골드 비용.
/// </summary>
[RequireComponent(typeof(PlayerCharacterController))]
public class PlayerBuildController : MonoBehaviour
{
    private static readonly Vector2Int[] NeighborOffsets =
    {
        new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    [SerializeField] private float buildRange = 1.55f;
    [SerializeField] private float rotateSpeed = 10f;
    [Header("Walk-to-site failover")]
    [SerializeField] private float repathCooldown = 0.45f;
    [SerializeField] private float cancelAfterSeconds = 8f;

    private PlayerCharacterController player;
    private FarmDrillCountdownUI countdownUi;

    private DefenseBuildJob pendingJob;
    private DefenseBuildJob activeJob;
    private DefenseBuildSiteVisual siteVisual;
    private float buildProgress;
    private float walkStuckTimer;
    private float timeSinceRequested;
    private int approachAttempt;
    private Vector3 currentApproachPoint;

    public bool IsWalkingToSite => pendingJob != null;
    public bool IsBuilding => activeJob != null;
    public bool HasActiveBuild => pendingJob != null || activeJob != null;
    public float BuildRemainingSeconds =>
        activeJob == null ? 0f : Mathf.Max(0f, activeJob.DurationSeconds - buildProgress);

    private void Awake()
    {
        player = GetComponent<PlayerCharacterController>();
        countdownUi = FarmDrillCountdownUI.Create(transform);
        countdownUi.Hide();
    }

    public bool TryStartBuild(DefenseBuildJob job)
    {
        if (job == null || HasActiveBuild)
            return false;

        if (!CanBuildNow())
            return false;

        GetComponent<PlayerFarmDrillController>()?.CancelDrill();

        pendingJob = job;
        buildProgress = 0f;
        walkStuckTimer = 0f;
        timeSinceRequested = 0f;
        approachAttempt = 0;

        float cellSize = DefenseBuildManager.Instance != null
            ? DefenseBuildManager.Instance.CellSize
            : 1f;
        siteVisual = DefenseBuildSiteVisual.Create(job.SitePosition, job, cellSize);
        siteVisual.SetProgress(0f);

        WalkToSite();
        return true;
    }

    public void CancelBuild()
    {
        var job = activeJob ?? pendingJob;
        if (job == null)
            return;

        if (job.GoldSpent && GameManager.Instance != null)
            GameManager.Instance.AddMoney(job.GoldCost);

        DefenseBuildManager.Instance?.CancelReservedCell(job.Cell);
        CleanupBuildState();
        player.TryResolveUnstuck();
    }

    private void Update()
    {
        if (pendingJob != null)
        {
            UpdateWalkingToSite();
            return;
        }

        if (activeJob != null)
            UpdateBuilding();
    }

    private void UpdateWalkingToSite()
    {
        if (!CanBuildNow())
        {
            CancelBuild();
            return;
        }

        timeSinceRequested += Time.deltaTime;
        if (timeSinceRequested >= cancelAfterSeconds)
        {
            CancelBuild();
            return;
        }

        FaceSite(pendingJob.SitePosition);

        if (player.IsMoving)
        {
            walkStuckTimer = 0f;
            return;
        }

        if (CanBeginConstructionAtCurrentPosition())
        {
            BeginConstruction();
            return;
        }

        walkStuckTimer += Time.deltaTime;
        if (walkStuckTimer >= repathCooldown)
        {
            walkStuckTimer = 0f;
            approachAttempt++;
            WalkToSite();
        }
    }

    private bool CanBeginConstructionAtCurrentPosition()
    {
        if (pendingJob == null)
            return false;

        float cellSize = GetCellSize();
        float dist = player.DistanceTo(pendingJob.SitePosition);
        if (dist > buildRange || dist < cellSize * 0.38f)
            return false;

        if (UnitMovementCollision.IsPositionBlocked(
                transform.position,
                player.BodyRadius,
                player.BodyHeight,
                player.GroundY))
            return false;

        return IsValidApproachPoint(transform.position, pendingJob.SitePosition);
    }

    private void BeginConstruction()
    {
        if (!TrySpendGold(pendingJob))
        {
            DefenseBuildManager.Instance?.CancelReservedCell(pendingJob.Cell);
            CleanupBuildState();
            return;
        }

        activeJob = pendingJob;
        pendingJob = null;
        buildProgress = 0f;
        player.StopMovement();
        FaceSite(activeJob.SitePosition);
        countdownUi.SetPrefix("건설");
        countdownUi.SetRemaining(activeJob.DurationSeconds);
        FarmBuildAudio.PlayLoop(transform);
    }

    private void UpdateBuilding()
    {
        if (!CanBuildNow())
        {
            CancelBuild();
            return;
        }

        buildProgress += Time.deltaTime;
        FaceSite(activeJob.SitePosition);

        float normalized = buildProgress / activeJob.DurationSeconds;
        siteVisual?.SetProgress(normalized);
        countdownUi.SetRemaining(BuildRemainingSeconds);

        if (buildProgress >= activeJob.DurationSeconds)
            FinishBuild();
    }

    private void FinishBuild()
    {
        var job = activeJob;
        CleanupBuildState();
        DefenseBuildManager.Instance?.CompleteBuild(job);
        ReleasePlayerFromBuildSite(job);
    }

    private void ReleasePlayerFromBuildSite(DefenseBuildJob job)
    {
        if (job == null)
            return;

        float cellSize = GetCellSize();
        float minSafeDist = cellSize * 0.52f;
        if (player.DistanceTo(job.SitePosition) < minSafeDist
            || UnitMovementCollision.IsPositionBlocked(
                transform.position,
                player.BodyRadius,
                player.BodyHeight,
                player.GroundY))
        {
            if (TryFindBestApproachPoint(job.SitePosition, job.Cell, out Vector3 escapePoint))
                player.PlaceAtWorld(escapePoint);
            else
                player.TryResolveUnstuck(cellSize * 3.5f);
        }
    }

    private void CleanupBuildState()
    {
        pendingJob = null;
        activeJob = null;
        buildProgress = 0f;
        player.StopMovement();
        FarmBuildAudio.Stop();
        countdownUi.Hide();

        if (siteVisual != null)
        {
            siteVisual.DestroyVisual();
            siteVisual = null;
        }
    }

    private bool TrySpendGold(DefenseBuildJob job)
    {
        if (job.GoldSpent)
            return true;

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(job.GoldCost))
            return false;

        job.GoldSpent = true;
        return true;
    }

    private void WalkToSite()
    {
        if (pendingJob == null)
            return;

        if (!TryFindBestApproachPoint(
                pendingJob.SitePosition,
                pendingJob.Cell,
                out Vector3 approach,
                approachAttempt))
        {
            return;
        }

        currentApproachPoint = approach;
        if (!player.TrySetMoveTarget(approach, true))
            approachAttempt++;
    }

    private bool TryFindBestApproachPoint(
        Vector3 sitePosition,
        Vector2Int siteCell,
        out Vector3 approachPoint,
        int skip = 0)
    {
        approachPoint = sitePosition;
        var candidates = CollectApproachCandidates(sitePosition, siteCell);
        if (candidates.Count == 0)
            return false;

        int index = Mathf.Clamp(skip, 0, candidates.Count - 1);
        approachPoint = candidates[index];
        return true;
    }

    private List<Vector3> CollectApproachCandidates(Vector3 sitePosition, Vector2Int siteCell)
    {
        float cellSize = GetCellSize();
        var playerPos = transform.position;
        var ranked = new List<(float score, Vector3 point)>();

        foreach (var offset in NeighborOffsets)
        {
            Vector2Int approachCell = siteCell + offset;
            if (DefenseBuildManager.Instance != null
                && DefenseBuildManager.Instance.IsCellBlockedForApproach(approachCell))
                continue;

            Vector3 candidate = sitePosition + new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);
            candidate.y = player.GroundY;

            if (!IsValidApproachPoint(candidate, sitePosition))
                continue;

            float distToSite = Vector3.Distance(candidate, sitePosition);
            if (distToSite > buildRange || distToSite < cellSize * 0.38f)
                continue;

            float distToPlayer = Vector3.Distance(candidate, playerPos);
            bool reachable = DefenseMapPathfinder.IsReady
                && DefenseMapPathfinder.HasPath(playerPos, candidate);

            float score = distToPlayer + (reachable ? 0f : 4f);
            ranked.Add((score, candidate));
        }

        ranked.Sort((a, b) => a.score.CompareTo(b.score));

        var results = new List<Vector3>(ranked.Count);
        foreach (var entry in ranked)
            results.Add(entry.point);

        return results;
    }

    private bool IsValidApproachPoint(Vector3 candidate, Vector3 sitePosition)
    {
        if (UnitMovementCollision.IsPositionBlocked(
                candidate,
                player.BodyRadius,
                player.BodyHeight,
                player.GroundY))
            return false;

        return true;
    }

    private float GetCellSize()
    {
        return DefenseBuildManager.Instance != null
            ? DefenseBuildManager.Instance.CellSize
            : 1f;
    }

    private void FaceSite(Vector3 sitePosition)
    {
        Vector3 look = sitePosition - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(look.normalized),
            Time.deltaTime * rotateSpeed);
    }

    private static bool CanBuildNow()
    {
        if (DefenseStageTimerManager.Instance == null)
            return true;

        return DefenseStageTimerManager.Instance.CanPlayerBuild();
    }
}
