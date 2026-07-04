using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerGunShootVisual : MonoBehaviour
{
    private const float ArmRaiseBlendSpeed = 10f;
    private static readonly Vector3 CombatReadyOffsetR = new(-82f, -8f, 6f);
    private static readonly Vector3 CombatReadyOffsetL = new(-82f, 8f, -6f);

    private Transform armRPivot;
    private Transform armLPivot;
    private Transform aimRPivot;
    private Transform aimLPivot;
    private Transform cannonRPivot;
    private Transform cannonLPivot;
    private Transform muzzleR;
    private Transform muzzleL;
    private Quaternion armRRestRotation;
    private Quaternion armLRestRotation;
    private Quaternion cannonRRestRotation;
    private Quaternion cannonLRestRotation;
    private Quaternion armRDisplayRotation;
    private Quaternion armLDisplayRotation;
    private CwslPlayerMovement movement;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float walkPhase;
    private float combatBlend;
    private bool isShooting;
    private CwslGunCombatPoseMode poseMode;
    private Vector3 combatAimPoint;
    private Coroutine routine;

    private void Awake()
    {
        CacheParts();
        armRDisplayRotation = armRRestRotation;
        armLDisplayRotation = armLRestRotation;
    }

    private void CacheParts()
    {
        armRPivot = transform.Find("ArmRPivot");
        armLPivot = transform.Find("ArmLPivot");

        aimRPivot = transform.Find("ArmRPivot/BowAimPivot");
        if (aimRPivot == null)
            aimRPivot = transform.Find("ArmRPivot/CannonPivot");

        aimLPivot = transform.Find("ArmLPivot/BowAimPivotL");
        if (aimLPivot == null)
            aimLPivot = transform.Find("ArmLPivot/CannonPivotL");

        muzzleR = aimRPivot != null ? aimRPivot.Find("CannonPivot/Muzzle") : null;
        if (muzzleR == null && aimRPivot != null)
            muzzleR = aimRPivot.Find("Muzzle");

        muzzleL = aimLPivot != null ? aimLPivot.Find("CannonPivotL/Muzzle") : null;
        if (muzzleL == null && aimLPivot != null)
            muzzleL = aimLPivot.Find("Muzzle");

        cannonRPivot = aimRPivot != null ? aimRPivot.Find("CannonPivot") : null;
        cannonLPivot = aimLPivot != null ? aimLPivot.Find("CannonPivotL") : null;

        if (armRPivot != null)
        {
            armRRestRotation = armRPivot.localRotation;
            armRDisplayRotation = armRRestRotation;
        }

        if (armLPivot != null)
        {
            armLRestRotation = armLPivot.localRotation;
            armLDisplayRotation = armLRestRotation;
        }

        if (cannonRPivot != null)
            cannonRRestRotation = cannonRPivot.localRotation;

        if (cannonLPivot != null)
            cannonLRestRotation = cannonLPivot.localRotation;

        movement = GetComponentInParent<CwslPlayerMovement>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    public void SetCombatPose(Vector3 worldAimPoint, CwslGunCombatPoseMode mode)
    {
        poseMode = mode;
        if (mode != CwslGunCombatPoseMode.Off)
            combatAimPoint = worldAimPoint;
    }

    private void LateUpdate()
    {
        if (armRPivot == null)
            CacheParts();
        if (armRPivot == null || isShooting)
            return;

        var speed = EstimateMoveSpeed();
        var targetBlend = ResolveCombatBlend(speed);
        combatBlend = Mathf.MoveTowards(combatBlend, targetBlend, Time.deltaTime * ArmRaiseBlendSpeed);

        if (combatBlend > 0.001f)
        {
            ApplyRaisedArms(speed);
            return;
        }

        armRDisplayRotation = armRRestRotation;
        armLDisplayRotation = armLRestRotation;
        ApplyMoveSway(speed);
    }

    private float ResolveCombatBlend(float speed)
    {
        return poseMode switch
        {
            CwslGunCombatPoseMode.AttackMove => 1f,
            CwslGunCombatPoseMode.Hold when speed < 0.12f => 1f,
            _ => 0f
        };
    }

    private void ApplyRaisedArms(float speed)
    {
        var raisedR = ComputeCombatReadyRotation(armRRestRotation, isLeft: false);
        var raisedL = armLPivot != null
            ? ComputeCombatReadyRotation(armLRestRotation, isLeft: true)
            : armLRestRotation;

        armRDisplayRotation = raisedR;
        armLDisplayRotation = raisedL;

        var movingBob = 0f;
        if (poseMode == CwslGunCombatPoseMode.AttackMove && speed >= 0.12f)
        {
            walkPhase += Time.deltaTime * speed * 1.1f;
            movingBob = Mathf.Sin(walkPhase * 2f) * 0.35f * Mathf.Clamp01(speed / 7f);
        }

        armRPivot.localRotation = Quaternion.Slerp(armRRestRotation, armRDisplayRotation, combatBlend)
            * Quaternion.Euler(movingBob * 3f, 0f, 0f);

        if (armLPivot != null)
        {
            armLPivot.localRotation = Quaternion.Slerp(armLRestRotation, armLDisplayRotation, combatBlend)
                * Quaternion.Euler(movingBob * 2.5f, 0f, 0f);
        }
    }

    private void ApplyMoveSway(float speed)
    {
        if (speed < 0.12f)
        {
            armRPivot.localRotation = armRRestRotation;
            if (armLPivot != null)
                armLPivot.localRotation = armLRestRotation;
            return;
        }

        walkPhase += Time.deltaTime * speed * 1.35f;
        var sway = Mathf.Sin(walkPhase) * Mathf.Clamp01(speed / 7f);
        var bob = Mathf.Sin(walkPhase * 2f) * 0.5f * Mathf.Clamp01(speed / 7f);

        armRPivot.localRotation = armRRestRotation * Quaternion.Euler(
            bob * 4f,
            sway * 5f,
            -sway * 3f);

        if (armLPivot != null)
        {
            armLPivot.localRotation = armLRestRotation * Quaternion.Euler(
                bob * 3f,
                -sway * 4f,
                sway * 3f);
        }
    }

    private static Quaternion ComputeCombatReadyRotation(Quaternion restRotation, bool isLeft)
    {
        var offset = isLeft ? CombatReadyOffsetL : CombatReadyOffsetR;
        return restRotation * Quaternion.Euler(offset);
    }

    private void ApplyGunRecoil(Transform cannonPivot, Quaternion restRotation, float kick)
    {
        if (cannonPivot == null)
            return;

        cannonPivot.localRotation = restRotation * Quaternion.Euler(-12f * kick, 0f, 0f);
    }

    private void RestoreGunPivots()
    {
        if (cannonRPivot != null)
            cannonRPivot.localRotation = cannonRRestRotation;
        if (cannonLPivot != null)
            cannonLPivot.localRotation = cannonLRestRotation;
    }

    private float EstimateMoveSpeed()
    {
        var root = transform.root;
        var flatDelta = root.position - lastRootPosition;
        flatDelta.y = 0f;
        lastRootPosition = root.position;
        var estimatedSpeed = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;

        var speed = estimatedSpeed;
        if (movement != null && movement.CurrentMoveSpeed > speed)
            speed = movement.CurrentMoveSpeed;
        else if (agent != null && agent.enabled && agent.velocity.magnitude > speed)
            speed = agent.velocity.magnitude;

        return speed;
    }

    public void PlayShoot(Vector3 worldAimPoint, bool useLeftGun)
    {
        if (armRPivot == null)
            CacheParts();
        if (armRPivot == null)
            return;

        combatAimPoint = worldAimPoint;
        if (poseMode == CwslGunCombatPoseMode.Off)
            poseMode = CwslGunCombatPoseMode.Hold;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShootRoutine(worldAimPoint, useLeftGun));
    }

    public void PlayDualShoot(Vector3 worldAimPoint)
    {
        if (armRPivot == null)
            CacheParts();
        if (armRPivot == null)
            return;

        combatAimPoint = worldAimPoint;
        if (poseMode == CwslGunCombatPoseMode.Off)
            poseMode = CwslGunCombatPoseMode.Hold;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(DualShootRoutine(worldAimPoint));
    }

    private IEnumerator ShootRoutine(Vector3 worldAimPoint, bool useLeftGun)
    {
        isShooting = true;

        var raisedR = ComputeCombatReadyRotation(armRRestRotation, isLeft: false);
        var raisedL = armLPivot != null
            ? ComputeCombatReadyRotation(armLRestRotation, isLeft: true)
            : armLRestRotation;

        armRDisplayRotation = raisedR;
        armLDisplayRotation = raisedL;
        armRPivot.localRotation = raisedR;
        if (armLPivot != null)
            armLPivot.localRotation = raisedL;

        var firingCannonPivot = useLeftGun ? cannonLPivot : cannonRPivot;
        var firingCannonRest = useLeftGun ? cannonLRestRotation : cannonRRestRotation;

        yield return null;

        var firingMuzzle = useLeftGun ? muzzleL : muzzleR;
        SpawnMuzzleFlash(firingMuzzle);

        const float recoilDuration = 0.12f;
        var timer = 0f;
        while (timer < recoilDuration)
        {
            timer += Time.deltaTime;
            var kick = Mathf.Sin((timer / recoilDuration) * Mathf.PI);
            var fireKick = kick * kick;

            ApplyGunRecoil(firingCannonPivot, firingCannonRest, fireKick);

            if (useLeftGun)
            {
                if (armLPivot != null)
                    armLPivot.localRotation = raisedL * Quaternion.Euler(-5f * fireKick, 0f, 2f * fireKick);
                armRPivot.localRotation = raisedR;
            }
            else
            {
                armRPivot.localRotation = raisedR * Quaternion.Euler(-5f * fireKick, 0f, -2f * fireKick);
                if (armLPivot != null)
                    armLPivot.localRotation = raisedL;
            }

            yield return null;
        }

        RestoreGunPivots();
        isShooting = false;
        routine = null;
    }

    private IEnumerator DualShootRoutine(Vector3 worldAimPoint)
    {
        isShooting = true;

        var raisedR = ComputeCombatReadyRotation(armRRestRotation, isLeft: false);
        var raisedL = armLPivot != null
            ? ComputeCombatReadyRotation(armLRestRotation, isLeft: true)
            : armLRestRotation;

        armRDisplayRotation = raisedR;
        armLDisplayRotation = raisedL;
        armRPivot.localRotation = raisedR;
        if (armLPivot != null)
            armLPivot.localRotation = raisedL;

        yield return null;

        SpawnMuzzleFlash(muzzleR);
        SpawnMuzzleFlash(muzzleL);

        const float recoilDuration = 0.14f;
        var timer = 0f;
        while (timer < recoilDuration)
        {
            timer += Time.deltaTime;
            var kick = Mathf.Sin((timer / recoilDuration) * Mathf.PI);
            var fireKick = kick * kick;

            ApplyGunRecoil(cannonRPivot, cannonRRestRotation, fireKick);
            ApplyGunRecoil(cannonLPivot, cannonLRestRotation, fireKick);
            armRPivot.localRotation = raisedR * Quaternion.Euler(-4f * fireKick, 0f, -2f * fireKick);
            if (armLPivot != null)
                armLPivot.localRotation = raisedL * Quaternion.Euler(-4f * fireKick, 0f, 2f * fireKick);

            yield return null;
        }

        RestoreGunPivots();
        isShooting = false;
        routine = null;
    }

    private static void SpawnMuzzleFlash(Transform muzzle)
    {
        if (muzzle == null)
            return;

        CwslVfxSpawner.SpawnGunMuzzleFlash(muzzle.position, muzzle.rotation);
    }
}
