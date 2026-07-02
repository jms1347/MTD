using UnityEngine;

public class CoopSpeedPad : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 1.55f;
    [SerializeField] private float boostDuration = 2.5f;

    private void Awake()
    {
        var trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.8f, 1.2f, 1.8f);
        trigger.center = new Vector3(0f, 0.6f, 0f);
    }

    private void OnTriggerStay(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null)
            return;

        CoopGimmickBuffs.SetMoveBoost(unit.PlayerId, speedMultiplier, boostDuration);
    }
}
