using Unity.Netcode;

using UnityEngine;



public class CwslMeleeMonster : CwslMonsterBase

{

    private const float MeleeInterval = 2.1f;

    private const float WindupDuration = 0.13f;

    private const float ChaseMoveMultiplier = 1.28f;

    private const float ChargeMoveMultiplier = 1.45f;

    private const float LungeDistance = 0.62f;



    private float meleeTimer;

    private float windupTimer;

    private bool isWindingUp;



    public override void Initialize(CwslMonsterType type)

    {

        base.Initialize(type);

    }



    protected override void TickServerAI()

    {

        var standDistance = GetCombatStandDistance();

        var distance = GetFlatDistanceToCombatPosition();



        if (isWindingUp)

        {

            windupTimer -= Time.deltaTime;

            FaceTarget(18f);

            if (distance > standDistance * 0.55f)

                MoveToward(GetTargetMovePosition(), ChargeMoveMultiplier * 0.35f);



            if (windupTimer <= 0f)

            {

                isWindingUp = false;

                DealMeleeHit();

            }



            return;

        }



        if (distance > standDistance)

            MoveToward(GetTargetMovePosition(), ChaseMoveMultiplier);

        else

            FaceTarget(16f);



        meleeTimer -= Time.deltaTime;

        if (meleeTimer > 0f || distance > standDistance + 0.25f)

            return;



        meleeTimer = MeleeInterval;

        isWindingUp = true;

        windupTimer = WindupDuration;

        PlayMeleeWindupClientRpc();

    }



    private void DealMeleeHit()

    {

        if (!IsValidTarget(currentTarget))

            return;



        var standDistance = GetCombatStandDistance();

        if (GetFlatDistanceToCombatPosition() > standDistance + 0.65f)

            return;



        transform.position += transform.forward * LungeDistance;

        transform.position = CwslArenaUtility.ClampToPlayArea(transform.position, GetMovementClampRadius());



        if (TryDamageCurrentTargetMelee(

                GetScaledDamage(CwslMonsterStatCatalog.GetMeleeAttackPower(MonsterType)),

                standDistance + 0.85f,

                Vector3.up * 1.05f))

            PlayMeleeHitClientRpc(GetTargetFacePosition());

    }



    private void FaceTarget(float turnSpeed)

    {

        var dir = GetTargetFacePosition() - transform.position;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)

            return;



        transform.rotation = Quaternion.Slerp(

            transform.rotation,

            Quaternion.LookRotation(dir.normalized),

            Time.deltaTime * turnSpeed);

    }



    [ClientRpc]

    private void PlayMeleeWindupClientRpc()

    {

        transform.Find("Visual")?.GetComponent<CwslMeleeLungeVisual>()?.PlayWindup();

    }



    [ClientRpc]

    private void PlayMeleeHitClientRpc(Vector3 targetPosition)

    {

        transform.Find("Visual")?.GetComponent<CwslMeleeLungeVisual>()?.PlayHit();



        var hitPoint = transform.position + transform.forward * 1.15f + Vector3.up * 1.05f;

        CwslVfxSpawner.SpawnMeleeHit(hitPoint, transform.rotation);

    }

}


