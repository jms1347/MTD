using Unity.Netcode;
using UnityEngine;

/// <summary>사수관 미니보스 — 화웅.</summary>
public class StllMiniBossHuangYing : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float nextSkillTime;

    public bool IsAlive => health.Value > 0f;

    public void ConfigureServer(float maxHp)
    {
        if (!IsServer)
            return;

        health.Value = maxHp;
        BuildVisual();
    }

    public void DamageServer(float amount, ulong attackerId, Vector3 knockback)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        if (health.Value <= 0f)
        {
            StllGoldDropper.ServerAwardKillGold(attackerId, gameObject);
            NetworkObject.Despawn(true);
        }
    }

    private void Update()
    {
        if (!IsServer || !IsAlive)
            return;

        if (Time.time < nextSkillTime)
            return;

        nextSkillTime = Time.time + 3f;
        var forward = transform.forward;
        var origin = transform.position;
        var enemies = FindObjectsByType<StllSupplyDepot>(FindObjectsSortMode.None);
        if (enemies.Length > 0)
            forward = (enemies[0].transform.position - origin).normalized;

        var players = FindObjectsByType<StllPlayerHealth>(FindObjectsSortMode.None);
        for (var i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player == null || !player.IsAlive)
                continue;

            var to = player.transform.position - origin;
            to.y = 0f;
            if (to.magnitude > 6f || Vector3.Angle(forward, to) > 50f)
                continue;

            player.DamageServer(35f);
        }
    }

    private void BuildVisual()
    {
        StllVisualUtil.CreatePrimitive(PrimitiveType.Capsule, transform, new Vector3(0f, 1.2f, 0f),
            new Vector3(0.9f, 1.2f, 0.9f), new Color(0.85f, 0.25f, 0.1f));
        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.height = 2.4f;
        collider.radius = 0.7f;
        collider.center = new Vector3(0f, 1.2f, 0f);
    }
}
