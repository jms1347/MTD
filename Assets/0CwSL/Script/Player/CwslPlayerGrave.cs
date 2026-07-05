using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerGrave : NetworkBehaviour
{
    private readonly NetworkVariable<bool> tombstoneActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> reviveGoldRequired = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> reviveGoldPaid = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private GameObject graveInstance;
    private TextMeshPro reviveLabel;
    private CwslPlayerHealth playerHealth;

    public bool IsTombstoneActive => tombstoneActive.Value;
    public int ReviveGoldRequired => reviveGoldRequired.Value;
    public int ReviveGoldPaid => reviveGoldPaid.Value;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        tombstoneActive.OnValueChanged += HandleTombstoneActiveChanged;
        reviveGoldRequired.OnValueChanged += HandleReviveGoldChanged;
        reviveGoldPaid.OnValueChanged += HandleReviveGoldChanged;
        RefreshTombstoneVisual();
        RefreshLabel();
    }

    public override void OnNetworkDespawn()
    {
        tombstoneActive.OnValueChanged -= HandleTombstoneActiveChanged;
        reviveGoldRequired.OnValueChanged -= HandleReviveGoldChanged;
        reviveGoldPaid.OnValueChanged -= HandleReviveGoldChanged;
        DestroyGraveVisual();
    }

    public void BeginTombstoneServer(int goldAtDeath)
    {
        if (!IsServer)
            return;

        reviveGoldRequired.Value = Mathf.Max(0, goldAtDeath);
        reviveGoldPaid.Value = 0;

        if (reviveGoldRequired.Value <= 0)
        {
            playerHealth?.ReviveServer(0);
            return;
        }

        tombstoneActive.Value = true;
    }

    public bool TryReceiveRevivePaymentServer(int amount)
    {
        if (!IsServer || amount <= 0 || !tombstoneActive.Value || playerHealth == null || !playerHealth.IsDead)
            return false;

        reviveGoldPaid.Value += amount;
        if (reviveGoldPaid.Value < reviveGoldRequired.Value)
            return true;

        var restoredGold = reviveGoldPaid.Value;
        playerHealth.ReviveServer(restoredGold);
        EndTombstoneServer();
        return true;
    }

    public void ForceEndTombstoneServer()
    {
        EndTombstoneServer();
    }

    private void EndTombstoneServer()
    {
        if (!IsServer)
            return;

        tombstoneActive.Value = false;
        reviveGoldRequired.Value = 0;
        reviveGoldPaid.Value = 0;
    }

    private void HandleTombstoneActiveChanged(bool previous, bool current)
    {
        RefreshTombstoneVisual();
        RefreshLabel();
    }

    private void HandleReviveGoldChanged(int previous, int current)
    {
        RefreshLabel();
    }

    private void RefreshTombstoneVisual()
    {
        if (!tombstoneActive.Value)
        {
            DestroyGraveVisual();
            return;
        }

        if (graveInstance != null)
            return;

        var prefab = CwslGameSession.Instance?.Assets?.graveVisualPrefab;
        if (prefab != null)
            graveInstance = Instantiate(prefab, transform);
        else
        {
            graveInstance = new GameObject("Grave");
            graveInstance.transform.SetParent(transform, false);
            CwslGraveVisualBuilder.Build(graveInstance.transform);
        }

        graveInstance.transform.localPosition = Vector3.zero;
        graveInstance.transform.localRotation = Quaternion.identity;
        CwslGraveMaterialFix.Refresh(graveInstance.transform);
        EnsureLabel();
    }

    private void EnsureLabel()
    {
        if (graveInstance == null || reviveLabel != null)
            return;

        var anchor = graveInstance.transform.Find("LabelAnchor");
        var labelRoot = new GameObject("ReviveGoldLabel");
        labelRoot.transform.SetParent(anchor != null ? anchor : graveInstance.transform, false);
        labelRoot.transform.localPosition = Vector3.zero;

        reviveLabel = labelRoot.AddComponent<TextMeshPro>();
        CwslTmpFontUtil.ApplyFont(reviveLabel);
        reviveLabel.alignment = TextAlignmentOptions.Center;
        reviveLabel.fontSize = 4.5f;
        reviveLabel.color = new Color(1f, 0.92f, 0.35f);
        reviveLabel.rectTransform.sizeDelta = new Vector2(5f, 1.2f);

        if (reviveLabel.font != null)
        {
            reviveLabel.outlineWidth = 0.2f;
            reviveLabel.outlineColor = new Color32(20, 20, 20, 255);
        }

        labelRoot.AddComponent<CwslBillboardToCamera>();
    }

    private void RefreshLabel()
    {
        if (!tombstoneActive.Value)
            return;

        EnsureLabel();
        if (reviveLabel == null)
            return;

        reviveLabel.text = $"{reviveGoldPaid.Value} / {reviveGoldRequired.Value}";
    }

    private void DestroyGraveVisual()
    {
        if (graveInstance != null)
            Destroy(graveInstance);
        graveInstance = null;
        reviveLabel = null;
    }
}
