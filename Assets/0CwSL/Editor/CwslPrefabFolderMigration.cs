#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>프리팹 폴더 구조로 기존 에셋을 이동한다.</summary>
public static class CwslPrefabFolderMigration
{
    private struct MoveRule
    {
        public string FileName;
        public string DestFolder;

        public MoveRule(string fileName, string destFolder)
        {
            FileName = fileName;
            DestFolder = destFolder;
        }
    }

    private static readonly MoveRule[] Rules =
    {
        new("CwslPlayer.prefab", "Characters/Player"),
        new("CwslBoss_Hongmyeongbo.prefab", "Boss"),
        new("CwslMonster_Melee.prefab", "Monsters/Melee"),
        new("CwslMonster_KoreaUniversitySoldier.prefab", "Monsters/Melee"),
        new("CwslMonster_Ranged.prefab", "Monsters/Ranged"),
        new("CwslMonster_Suicide.prefab", "Monsters/Suicide"),
        new("CwslMonster_StickySuicide.prefab", "Monsters/Suicide"),
        new("CwslMonster_MidBoss.prefab", "Monsters/Elite"),
        new("CwslMonster_DefenseBoss.prefab", "Monsters/Elite"),
        new("CwslProjectile.prefab", "Projectiles"),
        new("CwslPlayerMissile.prefab", "Projectiles"),
        new("CwslFrozenOrb.prefab", "Projectiles"),
        new("CwslGoldPickup.prefab", "Pickups"),
        new("CwslPillPickup.prefab", "Pickups"),
        new("CwslNexus.prefab", "Defense"),
        new("CwslEnemyBase.prefab", "Defense"),
        new("CwslGraveVisual.prefab", "Visuals")
    };

    [MenuItem("Tools/CwSL/Reorganize Prefab Folders", false, 21)]
    public static void ReorganizeFromMenu()
    {
        CwslPrefabPaths.EnsureFoldersExist();

        var moved = 0;
        foreach (var rule in Rules)
        {
            var source = CwslPrefabPaths.Root + "/" + rule.FileName;
            var destination = CwslPrefabPaths.Root + "/" + rule.DestFolder + "/" + rule.FileName;
            if (AssetDatabase.LoadAssetAtPath<Object>(source) == null)
                continue;

            if (AssetDatabase.LoadAssetAtPath<Object>(destination) != null)
                continue;

            var error = AssetDatabase.MoveAsset(source, destination);
            if (string.IsNullOrEmpty(error))
                moved++;
            else
                Debug.LogWarning("[CwSL] Prefab move failed: " + source + " -> " + destination + " : " + error);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("프리팹 폴더 정리", moved + "개 프리팹을 타입별 폴더로 이동했습니다.", "확인");
    }
}
#endif
