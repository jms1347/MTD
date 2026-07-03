using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Q 방패(골드 있을 때) 캐릭터를 감싸는 트리거 콜라이더.
/// 근접/자폭 몬스터는 방패 밖으로 밀려납니다.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CwslPlayerShieldBubble : MonoBehaviour
{
    private const float PushSpeed = 14f;
    private const float PushPadding = 0.2f;

    private SphereCollider bubbleCollider;
    private Transform bubbleRoot;
    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerCharacter playerCharacter;
    private bool wasActive;

    public bool IsBubbleActive => bubbleCollider != null && bubbleCollider.enabled;
    public float Radius => CwslGameConstants.FortifyShieldBlockRadius;
    public CwslPlayerHealth PlayerHealth => playerHealth;

    private void Awake()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        EnsureBubble();
        ApplyActive(false);
    }

    private void LateUpdate()
    {
        var active = ShouldBeActive();
        if (active != wasActive || bubbleCollider == null)
        {
            wasActive = active;
            ApplyActive(active);
        }

        if (active)
            PushMonstersOut();
    }

    public bool TryBlockProjectileServer(Vector3 hitPosition, float damage)
    {
        if (!IsBubbleActive || playerHealth == null)
            return false;

        return playerHealth.TryBlockHitServer(hitPosition, damage);
    }

    private void PushMonstersOut()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var center = transform.position;
        var radius = Radius + PushPadding;
        var radiusSqr = radius * radius;

        var monsters = FindObjectsByType<CwslMonsterBase>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null)
                continue;

            // 근접·자폭만 밀어냄 (원거리는 밖에서 쏘므로 제외)
            if (monster is not CwslMeleeMonster && monster is not CwslSuicideMonster)
                continue;

            var health = monster.GetComponent<CwslMonsterHealth>();
            if (health != null && !health.IsAlive)
                continue;

            var monsterPos = monster.transform.position;
            var flat = monsterPos - center;
            flat.y = 0f;
            var distSqr = flat.sqrMagnitude;
            if (distSqr >= radiusSqr)
                continue;

            Vector3 pushDir;
            if (distSqr < 0.0001f)
            {
                pushDir = monster.transform.forward;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.0001f)
                    pushDir = Vector3.forward;
                pushDir.Normalize();
            }
            else
            {
                pushDir = flat.normalized;
            }

            var targetPos = center + pushDir * radius;
            targetPos.y = monsterPos.y;
            monster.transform.position = Vector3.MoveTowards(
                monsterPos,
                targetPos,
                PushSpeed * Time.deltaTime);
        }
    }

    private bool ShouldBeActive()
    {
        if (fortifySkill == null)
            fortifySkill = GetComponent<CwslTankFortifySkill>();
        if (playerCharacter == null)
            playerCharacter = GetComponent<CwslPlayerCharacter>();

        return fortifySkill != null &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.Tank &&
               fortifySkill.IsShieldActive;
    }

    private void EnsureBubble()
    {
        if (bubbleCollider != null)
            return;

        bubbleRoot = transform.Find("ShieldBubble");
        if (bubbleRoot == null)
        {
            var go = new GameObject("ShieldBubble");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = gameObject.layer;
            bubbleRoot = go.transform;
        }
        else
        {
            bubbleRoot.gameObject.layer = gameObject.layer;
        }

        bubbleCollider = bubbleRoot.GetComponent<SphereCollider>();
        if (bubbleCollider == null)
            bubbleCollider = bubbleRoot.gameObject.AddComponent<SphereCollider>();

        bubbleCollider.isTrigger = true;
        bubbleCollider.radius = CwslGameConstants.FortifyShieldBlockRadius;
        bubbleCollider.center = Vector3.zero;
        bubbleCollider.enabled = true;

        var marker = bubbleRoot.GetComponent<CwslShieldBubbleMarker>();
        if (marker == null)
            marker = bubbleRoot.gameObject.AddComponent<CwslShieldBubbleMarker>();
        marker.Bind(this);
    }

    private void ApplyActive(bool active)
    {
        EnsureBubble();
        if (bubbleCollider != null)
            bubbleCollider.enabled = active;
        if (bubbleRoot != null)
            bubbleRoot.gameObject.SetActive(true);
    }
}

public class CwslShieldBubbleMarker : MonoBehaviour
{
    private CwslPlayerShieldBubble bubble;

    public CwslPlayerShieldBubble Bubble => bubble;

    public void Bind(CwslPlayerShieldBubble owner)
    {
        bubble = owner;
    }
}
