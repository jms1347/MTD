using UnityEngine;

public class CoopAmbushTrigger : MonoBehaviour
{
    [SerializeField] private float cooldownSeconds = 18f;
    [SerializeField] private int burstCount = 2;

    private float nextTriggerTime;

    private void Awake()
    {
        var trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2.2f, 1.4f, 2.2f);
        trigger.center = new Vector3(0f, 0.7f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null)
            return;

        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority || !session.WaveActive)
            return;

        if (Time.time < nextTriggerTime)
            return;

        nextTriggerTime = Time.time + cooldownSeconds;
        session.TriggerAmbushBurst(transform.position, burstCount);
    }
}
