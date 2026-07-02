using UnityEngine;



/// <summary>

/// 농장 흙 타일로 이동한 뒤 5초간 드릴하여 골드를 획득합니다.

/// </summary>

[RequireComponent(typeof(PlayerCharacterController))]

public class PlayerFarmDrillController : MonoBehaviour

{

    [SerializeField] private float drillDuration = FarmGoldEconomy.DrillDurationSeconds;

    [SerializeField] private float rotateSpeed = 10f;

    [SerializeField] private float debrisInterval = 0.22f;



    private PlayerCharacterController player;

    private PlayerDrillShake drillShake;

    private FarmDrillTile pendingTile;

    private FarmDrillTile activeTile;

    private FarmDrillCountdownUI countdownUi;

    private float drillProgress;

    private float nextDebrisTime;

    private bool isDrilling;



    public bool IsDrilling => isDrilling;

    public float DrillRemainingSeconds => Mathf.Max(0f, drillDuration - drillProgress);



    private void Awake()

    {

        player = GetComponent<PlayerCharacterController>();

        drillShake = GetComponent<PlayerDrillShake>();

        if (drillShake == null)

            drillShake = gameObject.AddComponent<PlayerDrillShake>();



        countdownUi = FarmDrillCountdownUI.Create(transform);

    }



    public void RequestDrill(FarmDrillTile tile)

    {

        if (tile == null || isDrilling)

            return;

        if (GetComponent<PlayerBuildController>() is { HasActiveBuild: true })

            return;

        if (!CanDrillNow())

            return;



        CancelPending();

        pendingTile = tile;



        if (player.DistanceTo(tile.transform.position) <= player.DrillRange && !player.IsMoving)

        {

            if (!tile.CanDrill())

            {

                WalkToTile(tile);

                return;

            }



            BeginDrill(tile);

            return;

        }



        WalkToTile(tile);

    }



    public void CancelDrill()

    {

        if (isDrilling)

            EndDrillState();



        CancelPending();

    }



    private void Update()

    {

        if (!isDrilling && pendingTile != null)

        {

            if (player.IsMoving)

                return;



            if (player.DistanceTo(pendingTile.transform.position) <= player.DrillRange)

            {

                var tile = pendingTile;

                pendingTile = null;



                if (tile.CanDrill())

                    BeginDrill(tile);

            }

        }



        if (!isDrilling)

            return;



        if (!CanDrillNow() || activeTile == null)

        {

            CancelDrill();

            return;

        }



        player.StopMovement();

        FaceDrillTarget();



        drillProgress += Time.deltaTime;

        float normalized = drillProgress / drillDuration;

        activeTile.SetDrillProgress(normalized);

        countdownUi.SetRemaining(DrillRemainingSeconds);



        if (Time.time >= nextDebrisTime)

        {

            FarmDrillVfx.PlayDebrisBurst(activeTile.transform.position);

            nextDebrisTime = Time.time + debrisInterval;

        }



        if (drillProgress >= drillDuration)

            FinishDrill();

    }



    private void BeginDrill(FarmDrillTile tile)

    {

        pendingTile = null;

        activeTile = tile;

        drillProgress = 0f;

        nextDebrisTime = Time.time;

        isDrilling = true;

        player.StopMovement();

        tile.BeginDrill();

        countdownUi.SetRemaining(drillDuration);

        drillShake.SetShaking(true);

        FarmDrillAudio.PlayLoop(transform);

        FarmDrillVfx.PlayDebrisBurst(tile.transform.position);

    }



    private void FinishDrill()
    {
        if (activeTile != null)
        {
            long reward = activeTile.CompleteDrill();
            FarmGoldGainPopup.Show(transform, reward);
        }

        EndDrillState();
    }



    private void EndDrillState()

    {

        FarmDrillAudio.Stop();

        drillShake.SetShaking(false);

        countdownUi.Hide();

        activeTile?.EndDrill();

        activeTile = null;

        drillProgress = 0f;

        isDrilling = false;

    }



    private void WalkToTile(FarmDrillTile tile)

    {

        if (player.TrySetMoveTarget(tile.transform.position))

            PlayerMoveMarker.Show(tile.transform.position);

        else

            pendingTile = null;

    }



    private void FaceDrillTarget()

    {

        if (activeTile == null)

            return;



        Vector3 toTile = activeTile.transform.position - transform.position;

        toTile.y = 0f;

        if (toTile.sqrMagnitude < 0.0001f)

            return;



        var targetRotation = Quaternion.LookRotation(toTile.normalized);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);

    }



    private void CancelPending()

    {

        pendingTile = null;

    }



    private void OnDestroy()

    {

        if (isDrilling)

            FarmDrillAudio.Stop();

    }



    private static bool CanDrillNow()
    {
        if (DefenseStageTimerManager.Instance != null)
            return DefenseStageTimerManager.Instance.CanPlayerGather();

        return true;
    }

    public static bool IsGatherAllowed()
    {
        return CanDrillNow();
    }
}


