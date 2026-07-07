using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSelection : NetworkBehaviour
{
    private readonly NetworkVariable<NetworkObjectReference> selectedTarget = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private GameObject localIndicator;

    public bool TryGetSelectedTarget(out NetworkObject target)
    {
        target = null;
        if (!selectedTarget.Value.TryGet(out var obj) || obj == null)
            return false;

        target = obj;
        return true;
    }

    public override void OnNetworkSpawn()
    {
        selectedTarget.OnValueChanged += HandleSelectionChanged;
        RefreshIndicator();
    }

    public override void OnNetworkDespawn()
    {
        selectedTarget.OnValueChanged -= HandleSelectionChanged;
        DestroyIndicator();
    }

    public void SetTargetServer(NetworkObject target)
    {
        if (!IsServer)
            return;

        selectedTarget.Value = target != null ? target : default;
    }

    private void HandleSelectionChanged(NetworkObjectReference previous, NetworkObjectReference current)
    {
        RefreshIndicator();
    }

    private void LateUpdate()
    {
        if (!IsOwner || localIndicator == null)
            return;

        // 죽은 몬스터 선택 해제 표시
        if (!TryGetSelectedTarget(out var target) || target == null)
        {
            DestroyIndicator();
            return;
        }

        var monsterHealth = target.GetComponent<CwslMonsterHealth>();
        if (monsterHealth != null && !monsterHealth.IsAlive)
            DestroyIndicator();

        var enemyBase = target.GetComponent<CwslEnemyBase>();
        if (enemyBase != null && !enemyBase.IsAlive)
            DestroyIndicator();
    }

    private void RefreshIndicator()
    {
        if (!IsOwner)
            return;

        DestroyIndicator();
        if (!TryGetSelectedTarget(out var target) || target == null)
            return;

        var isMonster = target.GetComponent<CwslMonsterHealth>() != null;
        var isStructure = target.GetComponent<CwslEnemyBase>() != null;
        var color = isMonster
            ? new Color(1f, 0.2f, 0.12f, 0.95f)
            : isStructure
                ? new Color(1f, 0.45f, 0.1f, 0.92f)
                : new Color(1f, 0.92f, 0.2f, 0.85f);

        localIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        localIndicator.name = "SelectionRing";
        Object.Destroy(localIndicator.GetComponent<Collider>());
        localIndicator.transform.SetParent(target.transform, false);

        var targetGrave = target.GetComponent<CwslPlayerGrave>();
        var yOffset = targetGrave != null && targetGrave.IsTombstoneActive ? 0.15f : 0.08f;
        localIndicator.transform.localPosition = new Vector3(0f, yOffset, 0f);

        var scale = isMonster
            ? ResolveMonsterSelectionRingScale(target)
            : isStructure
                ? 2.4f
                : ResolvePlayerSelectionRingScale(target);
        localIndicator.transform.localScale = new Vector3(scale, 0.03f, scale);

        var renderer = localIndicator.GetComponent<Renderer>();
        var material = CwslMaterialUtil.CreateMatteColored(color);
        // 살짝 투명하게
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        renderer.material = material;
    }

    private void DestroyIndicator()
    {
        if (localIndicator != null)
            Destroy(localIndicator);
        localIndicator = null;
    }

    private static float ResolveMonsterSelectionRingScale(NetworkObject target)
    {
        var monsterHealth = target.GetComponent<CwslMonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsBoss)
            return CwslGameConstants.BossVisualScale * 2.6f;

        return 2.1f;
    }

    private static float ResolvePlayerSelectionRingScale(NetworkObject target)
    {
        var bodyCollider = target.GetComponent<CwslPlayerBodyCollider>();
        var radius = bodyCollider != null
            ? bodyCollider.Radius
            : CwslGameConstants.PlayerBodyColliderRadiusDefault;
        return Mathf.Clamp(radius * 2.15f, 0.72f, 1.05f);
    }
}
