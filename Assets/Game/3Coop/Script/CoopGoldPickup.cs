using UnityEngine;

public class CoopGoldPickup : MonoBehaviour
{
    [SerializeField] private int goldAmount = 45;
    private bool collected;

    private void Awake()
    {
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.85f;
        trigger.center = new Vector3(0f, 0.5f, 0f);
    }

    private void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null)
            return;

        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority || !session.TryGetPlayer(unit.PlayerId, out var player))
            return;

        collected = true;
        player.gold += goldAmount;
        session.SetAnnouncement($"{player.playerName} 보물 상자 +{goldAmount}G!");
        Destroy(gameObject);
    }
}
