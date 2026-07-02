using UnityEngine;

/// <summary>
/// 화염방사·빙결 분사·레이저 등 지속 연출.
/// 타겟이 있으면 켜 두고 포신 회전만 따르고, 타겟이 없을 때만 끕니다.
/// </summary>
[DisallowMultipleComponent]
public class DefenseTowerSustainedPresentation : MonoBehaviour
{
    private TowerController tower;
    private Transform muzzle;
    private float attackRange;
    private string targetMobility;
    private DefenseSkillPresentationType presentationType;
    private string effectKey;
    private bool hasPresentation;
    private GameObject vfxInstance;
    private bool vfxActive;

    public static void TryAttach(GameObject towerObject, TowerController towerController)
    {
        if (towerObject == null || towerController == null)
            return;

        if (!towerController.TryGetActiveSkill(out var skill))
            return;

        if (!DefenseSkillPresentationCatalog.TryGet(skill, out var type, out var effectKey))
            return;

        if (type == DefenseSkillPresentationType.None || skill.moveType != DefenseMoveType.Straight)
            return;

        var presentation = towerObject.GetComponent<DefenseTowerSustainedPresentation>();
        if (presentation == null)
            presentation = towerObject.AddComponent<DefenseTowerSustainedPresentation>();

        presentation.Configure(towerController, type, effectKey);
    }

    private void Configure(
        TowerController towerController,
        DefenseSkillPresentationType type,
        string key)
    {
        tower = towerController;
        presentationType = type;
        effectKey = key;
        hasPresentation = true;
        muzzle = tower.MuzzlePoint ?? DefenseTowerVisualBuilder.FindFirePoint(tower.transform);
        attackRange = tower.AttackRange;
        targetMobility = tower.TargetMobility;
    }

    private void LateUpdate()
    {
        if (!hasPresentation || muzzle == null)
            return;

        Transform target = FindNearestEnemy();
        if (target == null)
        {
            SetVfxActive(false);
            return;
        }

        EnsureVfxInstance();
        SetVfxActive(true);
    }

    private void EnsureVfxInstance()
    {
        if (vfxInstance != null)
            return;

        if (!DefenseTowerSkillVfx.TryCreatePresentationInstance(
                muzzle,
                presentationType,
                effectKey,
                out vfxInstance))
            return;

        vfxInstance.name = $"TowerSustainedVfx_{effectKey}";
        vfxActive = vfxInstance.activeSelf;
    }

    private void SetVfxActive(bool active)
    {
        if (vfxInstance == null || vfxActive == active)
            return;

        vfxInstance.SetActive(active);
        vfxActive = active;
    }

    private Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = attackRange * attackRange;
        Vector3 origin = transform.position;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: targetMobility))
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = enemy.transform;
        }

        return nearest;
    }

    private void OnDestroy()
    {
        if (vfxInstance != null)
            Destroy(vfxInstance);
    }
}
