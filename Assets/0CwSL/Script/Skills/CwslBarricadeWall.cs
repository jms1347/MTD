using UnityEngine;
using UnityEngine.AI;

/// <summary>바리케이드 Q 벽 — 서버 권한 구조물(통과 불가 + 체력).</summary>
public class CwslBarricadeWall : MonoBehaviour
{
    private float health;
    private float maxHealth;
    private ulong ownerClientId;
    private Vector3 segmentA;
    private Vector3 segmentB;
    private float halfThickness;
    private CwslWorldUiHealthBar worldBar;
    private bool alive = true;

    public bool IsAlive => alive && health > 0f;
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;
    public ulong OwnerClientId => ownerClientId;
    public Vector3 FlatCenter
    {
        get
        {
            var p = transform.position;
            p.y = 0f;
            return p;
        }
    }

    public static CwslBarricadeWall SpawnServer(
        Vector3 start,
        Vector3 end,
        ulong ownerClientId,
        float maxHp)
    {
        if (Unity.Netcode.NetworkManager.Singleton == null ||
            !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return null;

        start.y = 0f;
        end.y = 0f;
        var delta = end - start;
        delta.y = 0f;
        var length = delta.magnitude;
        if (length < CwslGameConstants.BarricadeWallMinLength)
            return null;

        length = Mathf.Min(length, CwslGameConstants.BarricadeWallMaxLength);
        var direction = delta.normalized;
        end = start + direction * length;
        var center = (start + end) * 0.5f;
        center.y = CwslGameConstants.BarricadeWallHeight * 0.5f;

        var go = new GameObject("BarricadeWall");
        go.transform.position = center;
        go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        go.transform.localScale = new Vector3(
            CwslGameConstants.BarricadeWallThickness,
            CwslGameConstants.BarricadeWallHeight,
            length);

        BuildBrickVisual(go.transform);

        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;
        obstacle.carveOnlyStationary = true;

        var wall = go.AddComponent<CwslBarricadeWall>();
        wall.Initialize(start, end, ownerClientId, maxHp);
        return wall;
    }

    private static void BuildBrickVisual(Transform root)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one;
        Object.Destroy(body.GetComponent<Collider>());
        var renderer = body.GetComponent<Renderer>();
        if (renderer != null)
            CwslMaterialUtil.ApplyColor(renderer, new Color(0.47f, 0.41f, 0.37f));
    }

    private void Initialize(Vector3 start, Vector3 end, ulong owner, float maxHp)
    {
        segmentA = start;
        segmentB = end;
        halfThickness = CwslGameConstants.BarricadeWallThickness * 0.55f;
        ownerClientId = owner;
        maxHealth = Mathf.Max(1f, maxHp);
        health = maxHealth;
        alive = true;
        CwslBarricadeWallRegistry.Register(this);
        EnsureHealthBar();
        RefreshHealthBar();
    }

    private void OnDestroy()
    {
        CwslBarricadeWallRegistry.Unregister(this);
    }

    public Vector3 GetAimPoint() => transform.position + Vector3.up * 0.35f;

    public Vector3 GetMeleeApproachPoint(Vector3 attackerWorldPosition, float attackerRadius)
    {
        var closest = GetClosestPointOnSegment(attackerWorldPosition);
        var flat = attackerWorldPosition - closest;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = -transform.right;
        var stand = closest + flat.normalized * (halfThickness + attackerRadius + 0.4f);
        stand.y = attackerWorldPosition.y;
        return stand;
    }

    public bool TryGetSegmentCrossing(Vector3 from, Vector3 to, out Vector3 cross, out float distanceAlong)
    {
        cross = to;
        distanceAlong = 0f;
        var closest = GetClosestPointOnSegment(from);
        var move = to - from;
        move.y = 0f;
        if (move.sqrMagnitude < 0.0001f)
            return false;

        var alongDir = (segmentB - segmentA);
        alongDir.y = 0f;
        if (alongDir.sqrMagnitude < 0.0001f)
            return false;
        alongDir.Normalize();

        var lateral = Vector3.Cross(Vector3.up, alongDir);
        var fromSide = Vector3.Dot(from - closest, lateral);
        var toSide = Vector3.Dot(to - closest, lateral);
        var thickness = halfThickness + 0.25f;
        var fromOutside = Mathf.Abs(fromSide) > thickness;
        var crosses = Mathf.Abs(toSide) <= thickness || Mathf.Sign(fromSide) != Mathf.Sign(toSide);
        if (!fromOutside || !crosses)
            return false;

        var along = Vector3.Dot(closest - segmentA, alongDir);
        var segLen = Vector3.Distance(segmentA, segmentB);
        if (along < -0.5f || along > segLen + 0.5f)
            return false;

        cross = closest + lateral * Mathf.Sign(fromSide) * thickness;
        cross.y = from.y;
        distanceAlong = move.magnitude;
        return true;
    }

    public void DamageServer(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        var network = Unity.Netcode.NetworkManager.Singleton;
        if (network == null || !network.IsServer)
            return;

        health = Mathf.Max(0f, health - amount);
        CwslDamageFeedback.PlayFromServer(GetAimPoint(), amount, CwslDamagePopupKind.Structure);
        RefreshHealthBar();
        if (health <= 0f)
            DestroyWall();
    }

    public void DetonateServer()
    {
        if (!IsAlive)
            return;

        var network = Unity.Netcode.NetworkManager.Singleton;
        if (network == null || !network.IsServer)
            return;

        var center = FlatCenter;
        var radius = CwslGameConstants.BarricadeDetonateRadius;
        var radiusSq = radius * radius;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            monster.DamageFromPlayer(ownerClientId, CwslGameConstants.BarricadeDetonateBlastDamage);
            CwslMonsterStatusController.Ensure(monster)?.ApplyBurnServer(
                ownerClientId,
                CwslGameConstants.BarricadeDetonateBurnDuration,
                CwslGameConstants.BarricadeDetonateBurnDamage);
        }

        CwslVfxSpawner.SpawnBarricadeDetonate(center);
        DestroyWall(detonated: true);
    }

    private void DestroyWall(bool detonated = false)
    {
        alive = false;
        health = 0f;
        var center = FlatCenter;
        CwslBarricadeWallRegistry.Unregister(this);
        NotifyClientVisualDestroyed(center, detonated);
        Destroy(gameObject);
    }

    private void NotifyClientVisualDestroyed(Vector3 flatCenter, bool detonated)
    {
        var network = Unity.Netcode.NetworkManager.Singleton;
        if (network == null || !network.IsServer || network.SpawnManager == null)
            return;

        foreach (var obj in network.SpawnManager.SpawnedObjectsList)
        {
            if (obj == null || obj.OwnerClientId != ownerClientId)
                continue;

            var skill = obj.GetComponent<CwslBarricadeWallSkill>();
            if (skill == null)
                continue;

            skill.NotifyWallDestroyedClientRpc(flatCenter, detonated);
            return;
        }
    }

    private Vector3 GetClosestPointOnSegment(Vector3 worldPoint)
    {
        var ab = segmentB - segmentA;
        var lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.0001f)
            return segmentA;

        var t = Vector3.Dot(worldPoint - segmentA, ab) / lengthSq;
        t = Mathf.Clamp01(t);
        return segmentA + ab * t;
    }

    private void EnsureHealthBar()
    {
        if (worldBar != null)
            return;

        worldBar = gameObject.AddComponent<CwslWorldUiHealthBar>();
        worldBar.Configure(
            88f,
            10f,
            1.7f,
            new Color(0.85f, 0.55f, 0.25f),
            new Color(0.1f, 0.1f, 0.12f, 0.9f));
    }

    private void RefreshHealthBar()
    {
        if (worldBar == null)
            return;

        var ratio = maxHealth > 0f ? health / maxHealth : 0f;
        worldBar.Refresh(ratio, maxHealth);
        worldBar.SetVisible(IsAlive);
    }
}
