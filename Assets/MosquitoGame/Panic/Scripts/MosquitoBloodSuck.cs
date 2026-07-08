using Unity.Netcode;
using UnityEngine;

public class MosquitoBloodSuck : NetworkBehaviour
{
    private float suckTimer;
    private HumanController currentTarget;
    private DecoyHumanTrap currentDecoy;

    public void TryStartSuck()
    {
        if (!IsOwner)
            return;

        TryFindTarget(out currentTarget, out currentDecoy);
        if (currentTarget == null && currentDecoy == null)
            return;

        suckTimer = PanicGameConstants.BloodSuckIntervalSeconds;
    }

    private void Update()
    {
        if (!IsOwner || suckTimer <= 0f)
            return;

        suckTimer -= Time.deltaTime;
        if (suckTimer > 0f)
            return;

        if (currentDecoy != null)
        {
            currentDecoy.TriggerAlarm(transform.position);
            suckTimer = 0f;
            return;
        }

        if (currentTarget != null && currentTarget.IsAlive)
        {
            currentTarget.ReceiveBloodTick(OwnerClientId);
            ScoreManager.Instance?.RegisterBloodTick(OwnerClientId);
        }

        suckTimer = PanicGameConstants.BloodSuckIntervalSeconds;
    }

    private void TryFindTarget(out HumanController human, out DecoyHumanTrap decoy)
    {
        human = null;
        decoy = null;

        var hits = Physics.OverlapSphere(transform.position, PanicGameConstants.BloodSuckRange);
        var bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            var foundHuman = hit.GetComponentInParent<HumanController>();
            if (foundHuman != null && foundHuman.IsAlive)
            {
                var distance = Vector3.Distance(transform.position, foundHuman.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    human = foundHuman;
                    decoy = null;
                }
            }

            var foundDecoy = hit.GetComponentInParent<DecoyHumanTrap>();
            if (foundDecoy != null)
            {
                var distance = Vector3.Distance(transform.position, foundDecoy.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    decoy = foundDecoy;
                    human = null;
                }
            }
        }
    }
}
