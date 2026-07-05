using Unity.Netcode;

using UnityEngine;



public class CwslMeleeMonster : CwslMonsterBase

{

    private const float StickDistance = 1.05f;

    private const float MeleeInterval = 2.1f;

    private const float WindupDuration = 0.13f;

    private const float ChaseMoveMultiplier = 1.62f;

    private const float ChargeMoveMultiplier = 2.05f;

    private const float LungeDistance = 0.62f;

    private const float MeleeDamage = 6f;



    private float meleeTimer;

    private float windupTimer;

    private bool isWindingUp;



    public override void Initialize(CwslMonsterType type)

    {

        base.Initialize(type);

        moveSpeed = 1.525f;

    }



    protected override void TickServerAI()

    {

        var distance = GetFlatDistanceTo(currentTarget);



        if (isWindingUp)

        {

            windupTimer -= Time.deltaTime;

            FaceTarget(18f);

            MoveToward(currentTarget.transform.position, ChargeMoveMultiplier);

            if (windupTimer <= 0f)

            {

                isWindingUp = false;

                DealMeleeHit();

            }



            return;

        }



        if (distance > StickDistance)

            MoveToward(currentTarget.transform.position, ChaseMoveMultiplier);

        else

            FaceTarget(16f);



        meleeTimer -= Time.deltaTime;

        if (meleeTimer > 0f || distance > StickDistance + 0.25f)

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



        var distance = GetFlatDistanceTo(currentTarget);

        if (distance > StickDistance + 0.65f)

            return;



        transform.position += transform.forward * LungeDistance;



        var playerHealth = currentTarget.GetComponent<CwslPlayerHealth>();

        var hitPoint = currentTarget.transform.position + Vector3.up * 1.05f;

        playerHealth?.TryReceiveMeleeHitServer(MeleeDamage, hitPoint);

        PlayMeleeHitClientRpc(currentTarget.transform.position);

    }



    private void FaceTarget(float turnSpeed)

    {

        var dir = CwslTargetQuery.GetFlatDirection(transform.position, currentTarget.transform.position);

        transform.rotation = Quaternion.Slerp(

            transform.rotation,

            Quaternion.LookRotation(dir),

            Time.deltaTime * turnSpeed);

    }



    [ClientRpc]

    private void PlayMeleeWindupClientRpc()

    {

        var visual = transform.Find("Visual");

        if (visual != null)

            visual.GetComponent<CwslMeleeLungeVisual>()?.PlayWindup();

    }



    [ClientRpc]

    private void PlayMeleeHitClientRpc(Vector3 targetPosition)

    {

        var visual = transform.Find("Visual");

        if (visual != null)

            visual.GetComponent<CwslMeleeLungeVisual>()?.PlayHit();

        var hitPoint = transform.position + transform.forward * 1.15f + Vector3.up * 1.05f;

        CwslVfxSpawner.SpawnMeleeHit(hitPoint, transform.rotation);

    }

}


