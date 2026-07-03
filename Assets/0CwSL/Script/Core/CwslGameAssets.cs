using UnityEngine;

[CreateAssetMenu(fileName = "CwslGameAssets", menuName = "CwSL/Game Assets")]
public class CwslGameAssets : ScriptableObject
{
    public GameObject darkMissileVfx;
    public GameObject suicideExplosionVfx;
    public GameObject meleeHitVfx;
    public GameObject enemyDeathVfx;
    public GameObject bossDeathVfx;
    public GameObject playerDeathVfx;
    public GameObject goldBurstVfx;
    public GameObject goldMagnetTrailVfx;
    public GameObject playerMissileVfx;
    public GameObject fortifyAuraVfx;
    public GameObject fortifyBlockVfx;
    public AudioClip goldPickupSound;

    public GameObject playerPrefab;
    public GameObject rangedMonsterPrefab;
    public GameObject suicideMonsterPrefab;
    public GameObject meleeMonsterPrefab;
    public GameObject projectilePrefab;
    public GameObject playerMissilePrefab;
    public GameObject bossPrefab;
    public GameObject goldPickupPrefab;
    public GameObject graveVisualPrefab;
}
