using System.Collections.Generic;
using UnityEngine;

/// <summary>몬스터 월드 HP 바 — 피격 시 표시, 보스는 항상 표시.</summary>
public class CwslMonsterHealthBar : MonoBehaviour
{
    private const float NormalBarWidth = 1.1f;
    private const float EliteBarWidth = 1.65f;
    private const float BossBarWidth = 2.8f;
    private const float BarHeight = 0.1f;
    private const float ShowDuration = 4.5f;

    private CwslMonsterHealth monsterHealth;
    private Transform barRoot;
    private Transform fillTransform;
    private Renderer fillRenderer;
    private readonly List<Transform> segmentDividers = new();
    private float lastSegmentMaxHealth = -1f;
    private float showUntil;
    private float barWidth = NormalBarWidth;

    private void Awake()
    {
        monsterHealth = GetComponent<CwslMonsterHealth>();
    }

    private void OnEnable()
    {
        if (monsterHealth != null)
            monsterHealth.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (monsterHealth != null)
            monsterHealth.OnDamaged -= HandleDamaged;
    }

    private void LateUpdate()
    {
        if (monsterHealth == null || barRoot == null)
            return;

        var alwaysShow = monsterHealth.IsBoss
                         || monsterHealth.MonsterType is CwslMonsterType.MidBoss
                             or CwslMonsterType.SeniorCoach
                             or CwslMonsterType.DefenseBoss;
        var showBar = monsterHealth.IsAlive && (alwaysShow || Time.time < showUntil);
        if (barRoot.gameObject.activeSelf != showBar)
            barRoot.gameObject.SetActive(showBar);

        if (!showBar)
            return;

        barRoot.position = monsterHealth.GetDamagePopupAnchor() + Vector3.up * 0.18f;
        RefreshBar();
    }

    private void HandleDamaged(CwslMonsterHealth health, float damage, ulong attackerClientId)
    {
        if (damage <= 0f)
            return;

        showUntil = Time.time + ShowDuration;
        EnsureBarVisual();
        RefreshBar();
    }

    private void RefreshBar()
    {
        if (fillTransform == null || monsterHealth == null)
            return;

        var maxHealth = Mathf.Max(1f, monsterHealth.MaxHealth);
        var ratio = Mathf.Clamp01(monsterHealth.CurrentHealth / maxHealth);
        EnsureSegments(maxHealth);
        fillTransform.localScale = new Vector3(barWidth * ratio, BarHeight, 0.1f);
        fillTransform.localPosition = new Vector3(-barWidth * 0.5f + (barWidth * ratio * 0.5f), 0f, -0.01f);

        if (fillRenderer != null)
        {
            var color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.25f, 0.9f, 0.35f), ratio);
            CwslMaterialUtil.ApplyColor(fillRenderer, color);
        }
    }

    private void EnsureBarVisual()
    {
        if (barRoot != null)
            return;

        barWidth = ResolveBarWidth();

        var rootObject = new GameObject("MonsterHealthBar");
        rootObject.transform.SetParent(transform, false);
        barRoot = rootObject.transform;
        barRoot.gameObject.AddComponent<CwslBillboardToCamera>();

        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "HealthBar_Back";
        back.transform.SetParent(barRoot, false);
        back.transform.localScale = new Vector3(barWidth, BarHeight, 0.08f);
        Destroy(back.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(back.GetComponent<Renderer>(), new Color(0.12f, 0.12f, 0.14f, 0.9f));

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "HealthBar_Fill";
        fill.transform.SetParent(barRoot, false);
        fill.transform.localScale = new Vector3(barWidth, BarHeight, 0.1f);
        Destroy(fill.GetComponent<Collider>());
        fillRenderer = fill.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(fillRenderer, new Color(0.95f, 0.25f, 0.2f));
        fillTransform = fill.transform;

        barRoot.gameObject.SetActive(false);
        RefreshBar();
    }

    private float ResolveBarWidth()
    {
        if (monsterHealth == null)
            return NormalBarWidth;

        if (monsterHealth.IsBoss)
            return BossBarWidth;

        return monsterHealth.MonsterType is CwslMonsterType.MidBoss
            or CwslMonsterType.SeniorCoach
            or CwslMonsterType.DefenseBoss
            ? EliteBarWidth
            : NormalBarWidth;
    }

    private void EnsureSegments(float maxHealth)
    {
        if (barRoot == null || Mathf.Approximately(lastSegmentMaxHealth, maxHealth))
            return;

        CwslHealthBarSegments.BuildWorldDividers(
            barRoot,
            barWidth,
            BarHeight,
            maxHealth,
            segmentDividers);
        lastSegmentMaxHealth = maxHealth;
    }
}
