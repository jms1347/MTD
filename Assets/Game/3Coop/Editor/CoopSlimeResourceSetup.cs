#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CoopSlimeResourceSetup
{
    private const string ResourcesFolder = "Assets/Game/3Coop/Resources";
    private const string AssetPath = ResourcesFolder + "/CoopSlimeRuntimeRefs.asset";

    [MenuItem("Tools/Multiplayer/Setup Coop Slime Resources")]
    public static void CreateOrUpdateRuntimeRefs()
    {
        if (!Directory.Exists(ResourcesFolder))
            Directory.CreateDirectory(ResourcesFolder);

        var refs = AssetDatabase.LoadAssetAtPath<CoopSlimeRuntimeRefs>(AssetPath);
        if (refs == null)
        {
            refs = ScriptableObject.CreateInstance<CoopSlimeRuntimeRefs>();
            AssetDatabase.CreateAsset(refs, AssetPath);
        }

        refs.slimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Kawaii Slimes/Animator/Slime.controller");
        refs.faceAsset = AssetDatabase.LoadAssetAtPath<Face>(
            "Assets/Kawaii Slimes/Scripts/AI/DataFace.asset");

        var fbxAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Kawaii Slimes/Animation/Slime_Anim.fbx");
        refs.slimeAvatar = null;
        for (var i = 0; i < fbxAssets.Length; i++)
        {
            if (fbxAssets[i] is Avatar avatar)
            {
                refs.slimeAvatar = avatar;
                break;
            }
        }

        refs.slimePrefabs = new List<GameObject>();
        foreach (var key in new[]
                 {
                     "SLIME-01", "SLIME-01-KING", "SLIME-01-VIKING", "SLIME-01-METAL",
                     "SLIME-02", "SLIME-03", "SLIME-03-KING", "SLIME-03-LEAF", "SLIME-03-SPROUT"
                 })
        {
            if (!MonsterSlimePrefabPaths.TryGetAssetPath(key, out var path))
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                refs.slimePrefabs.Add(prefab);
        }

        EditorUtility.SetDirty(refs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CoopSlimeResourceSetup] Coop 슬라임 리소스 갱신 완료: {AssetPath}");
    }
}
#endif
