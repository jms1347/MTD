using UnityEngine;

/// <summary>
/// Q 방패(골드 있을 때) 캐릭터를 감싸는 트리거 콜라이더.
/// 근접/미사일/자폭 공격은 들어오되, 데미지는 방패가 막습니다.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CwslPlayerShieldBubble : MonoBehaviour
{
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
        if (active == wasActive && bubbleCollider != null)
            return;

        wasActive = active;
        ApplyActive(active);
    }

    public bool TryBlockProjectileServer(Vector3 hitPosition, float damage)
    {
        if (!IsBubbleActive || playerHealth == null)
            return false;

        return playerHealth.TryBlockHitServer(hitPosition, damage, isProjectile: true);
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
