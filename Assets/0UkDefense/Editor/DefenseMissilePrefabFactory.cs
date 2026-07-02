#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DefenseMissilePrefabFactory
{
    public const string OutputDir = "Assets/Game/2Game/Prefab/Defense/Combat/Missiles";

    private static readonly (string fileName, string sourcePath)[] MissileSources =
    {
        ("MissilePhysicalOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Grenade/GrenadePinkOBJ.prefab"),
        ("MissileWaterOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Liquid/LiquidWaterOBJ.prefab"),
        ("MissilePoisonOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Soul/SoulPurpleOBJ.prefab"),
        ("MissileFireOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Rocket/RocketFireOBJ.prefab"),
        ("MissileIceOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Frost/FrostMissileOBJ.prefab"),
        ("MissileLightningOBJ.prefab", "Assets/Epic Toon FX/Demo/Missile Prefabs/Lightning/LightningYellowOBJ.prefab"),
    };

    public static void EnsureMissilePrefabs()
    {
        EnsureFolder(OutputDir);

        foreach (var (fileName, sourcePath) in MissileSources)
        {
            var destPath = $"{OutputDir}/{fileName}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
                continue;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            {
                Debug.LogWarning($"[DefenseMissilePrefabFactory] Source missing: {sourcePath}");
                continue;
            }

            AssetDatabase.CopyAsset(sourcePath, destPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string GetMissilePath(DefenseMissileId id)
    {
        return id switch
        {
            DefenseMissileId.Physical => $"{OutputDir}/MissilePhysicalOBJ.prefab",
            DefenseMissileId.Water => $"{OutputDir}/MissileWaterOBJ.prefab",
            DefenseMissileId.Poison => $"{OutputDir}/MissilePoisonOBJ.prefab",
            DefenseMissileId.Fire => $"{OutputDir}/MissileFireOBJ.prefab",
            DefenseMissileId.Ice => $"{OutputDir}/MissileIceOBJ.prefab",
            DefenseMissileId.Lightning => $"{OutputDir}/MissileLightningOBJ.prefab",
            _ => $"{OutputDir}/MissilePhysicalOBJ.prefab"
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
