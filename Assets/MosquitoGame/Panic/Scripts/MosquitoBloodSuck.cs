using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 모기 흡혈: Player 레이어 바디 Collider에 빨대를 꽂고 부착(Attach) 후 틱 데미지.
/// </summary>
public class MosquitoBloodSuck : NetworkBehaviour
{
    private MosquitoController mosquito;
    private Rigidbody body;

    private HumanController attachedHuman;
    private DecoyHumanTrap currentDecoy;
    private Vector3 attachLocalOffset;
    private float suckTimer;
    private bool isAttached;

    private void Awake()
    {
        mosquito = GetComponent<MosquitoController>();
        body = GetComponent<Rigidbody>();
    }

    public bool IsAttached => isAttached;

    public void TryStartSuck()
    {
        if (!IsOwner || !mosquito.IsAlive)
            return;

        if (isAttached)
        {
            Detach();
            return;
        }

        if (!TryFindTarget(out var human, out var decoy, out var hitPoint))
            return;

        if (decoy != null)
        {
            decoy.TriggerAlarm(transform.position);
            return;
        }

        AttachToHuman(human, hitPoint);
    }

    private void Update()
    {
        if (!IsOwner || !isAttached)
            return;

        if (attachedHuman == null || !attachedHuman.IsAlive)
        {
            Detach();
            return;
        }

        // 떨어져 나가면 해제
        if (Vector3.Distance(transform.position, attachedHuman.GetAimPoint()) > PanicGameConstants.BloodSuckRange * 3f)
        {
            Detach();
            return;
        }

        suckTimer -= Time.deltaTime;
        if (suckTimer > 0f)
            return;

        attachedHuman.ReceiveBloodTick(OwnerClientId);
        ScoreManager.Instance?.RegisterBloodTick(OwnerClientId);
        suckTimer = PanicGameConstants.BloodSuckIntervalSeconds;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isAttached || attachedHuman == null)
            return;

        var worldAttach = attachedHuman.transform.TransformPoint(attachLocalOffset);
        body.MovePosition(worldAttach);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        var look = attachedHuman.GetAimPoint() - transform.position;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

    private void AttachToHuman(HumanController human, Vector3 worldHitPoint)
    {
        attachedHuman = human;
        currentDecoy = null;
        isAttached = true;
        suckTimer = PanicGameConstants.BloodSuckIntervalSeconds;
        attachLocalOffset = human.transform.InverseTransformPoint(worldHitPoint);

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        transform.position = worldHitPoint;
    }

    public void Detach()
    {
        isAttached = false;
        attachedHuman = null;
        currentDecoy = null;
        suckTimer = 0f;

        if (body != null)
            body.isKinematic = false;
    }

    private bool TryFindTarget(out HumanController human, out DecoyHumanTrap decoy, out Vector3 hitPoint)
    {
        human = null;
        decoy = null;
        hitPoint = transform.position;

        var mask = PanicVisionLayers.PlayerMask;
        if (mask == 0)
            mask = ~0;

        var hits = Physics.OverlapSphere(
            transform.position,
            PanicGameConstants.BloodSuckRange,
            mask,
            QueryTriggerInteraction.Ignore);

        var bestDistance = float.MaxValue;
        foreach (var hit in hits)
        {
            var foundHuman = hit.GetComponentInParent<HumanController>();
            if (foundHuman != null && foundHuman.IsAlive)
            {
                var attachPoint = foundHuman.GetNearestAttachPoint(transform.position);
                var distance = Vector3.Distance(transform.position, attachPoint);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                human = foundHuman;
                decoy = null;
                hitPoint = attachPoint;
                continue;
            }

            var foundDecoy = hit.GetComponentInParent<DecoyHumanTrap>();
            if (foundDecoy == null)
                continue;

            var d = Vector3.Distance(transform.position, foundDecoy.transform.position);
            if (d >= bestDistance)
                continue;

            bestDistance = d;
            decoy = foundDecoy;
            human = null;
            hitPoint = foundDecoy.transform.position;
        }

        // 가짜 미끼는 Default 레이어일 수 있어 한 번 더 검색
        if (human == null && decoy == null)
        {
            var allHits = Physics.OverlapSphere(transform.position, PanicGameConstants.BloodSuckRange);
            foreach (var hit in allHits)
            {
                var foundDecoy = hit.GetComponentInParent<DecoyHumanTrap>();
                if (foundDecoy == null)
                    continue;

                decoy = foundDecoy;
                hitPoint = foundDecoy.transform.position;
                break;
            }
        }

        return human != null || decoy != null;
    }
}
