using System;
using UnityEngine;

[Serializable]
public class TowerSpawnData
{
    public string towerName = "Tower";
    public int towerSheetId;
    public TowerKind kind = TowerKind.Standard;
    public Color color = Color.white;
    public GameObject missilePrefab;
    public DefenseMissileId standardMissileId = DefenseMissileId.Physical;
    public GameObject meteorProjectilePrefab;
    public GameObject meteorExplosionPrefab;
    public GameObject chainBoltPrefab;
    public GameObject chainHitExplosionPrefab;
    public GameObject stunHeadEffectPrefab;
    public GameObject stunBodyEffectPrefab;
    public Vector3 positionOffset;
    public float rotationY;
    public Vector3 scaleMultiplier = Vector3.one;
}
