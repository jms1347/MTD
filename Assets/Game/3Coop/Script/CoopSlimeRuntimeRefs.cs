using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CoopSlimeRuntimeRefs", menuName = "MTD/Coop Slime Runtime Refs")]
public class CoopSlimeRuntimeRefs : ScriptableObject
{
    public RuntimeAnimatorController slimeController;
    public Face faceAsset;
    public Avatar slimeAvatar;
    public List<GameObject> slimePrefabs = new();
}
