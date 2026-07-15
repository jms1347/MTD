using UnityEngine;

[CreateAssetMenu(fileName = "StllGameAssets", menuName = "STLL/Game Assets")]
public class StllGameAssets : ScriptableObject
{
    public GameObject playerPrefab;
    public GameObject minionPrefab;
    public GameObject enemyGruntPrefab;
    public GameObject supplyDepotPrefab;
    public GameObject miniBossPrefab;
    public GameObject bossLuBuPrefab;
    public AudioClip horseGallopSound;
}
