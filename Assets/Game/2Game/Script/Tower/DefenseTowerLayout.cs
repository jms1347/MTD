using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 배치 데이터. DefenseSceneSetup이 런타임에 VFX 프리팹을 채웁니다.
/// </summary>
[CreateAssetMenu(fileName = "DefenseTowerLayout", menuName = "UkDefense/Tower Layout")]
public class DefenseTowerLayout : ScriptableObject
{
    [Tooltip("넥서스(맵 중심) 월드 좌표")]
    public Vector3 arenaCenter = Vector3.zero;

    [Tooltip("타워 배치 기준 Y 오프셋")]
    public float towerHeight = 0.6f;

    public List<TowerSpawnData> towers = new();

    public Vector3 TowerOrigin => arenaCenter + new Vector3(0f, towerHeight, 0f);

    public TowerSpawnData[] ToSpawnArray()
    {
        return towers != null ? towers.ToArray() : System.Array.Empty<TowerSpawnData>();
    }

    public void SetDefaultLayout()
    {
        arenaCenter = Vector3.zero;
        towerHeight = 0.6f;
        towers = DefenseTowerLayoutDefaults.CreateDefaultTowers();
    }

    public void SetDefaultNearFarmLayout() => SetDefaultLayout();
}
