using UnityEngine;

[CreateAssetMenu(fileName = "StllGameAssets", menuName = "STLL/Game Assets")]
public class StllGameAssets : ScriptableObject
{
    public GameObject playerPrefab;
    public GameObject minionPrefab;
    public GameObject enemyGruntPrefab;
    public AudioClip horseGallopSound;
}
