#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEditor;

using UnityEngine;



public static class DefenseCombatCatalogFactory

{

    public const string CatalogPath = "Assets/0UkDefense/1Data/SO/DefenseCombatCatalog.asset";



    private const string MeteorMissilePath = "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Nuke/NukeMissileFire.prefab";

    private const string MeteorExplosionPath = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionFire.prefab";

    private const string ChainBoltPath = "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Sharp/LightningStrikeSharpBlue.prefab";

    private const string ChainHitExplosionPath =

        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/LightningExplosion/LightningExplosionYellow.prefab";

    private const string StunHeadEffectPath = "Assets/Epic Toon FX/Prefabs/Combat/Brawling/Stun/StunnedCirclingStarsSimple.prefab";

    private const string StunBodyEffectPath = "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Soft/LightningOrbBlue.prefab";

    private const string DeathEffectPath = "Assets/Epic Toon FX/Prefabs/Combat/Death/Souls/SoulCuteDeath.prefab";



    public static DefenseCombatCatalog EnsureCatalogAsset()

    {

        DefenseMissilePrefabFactory.EnsureMissilePrefabs();



        EnsureFolder("Assets/0UkDefense/1Data/SO");



        var catalog = AssetDatabase.LoadAssetAtPath<DefenseCombatCatalog>(CatalogPath);

        if (catalog == null)

        {

            catalog = ScriptableObject.CreateInstance<DefenseCombatCatalog>();

            AssetDatabase.CreateAsset(catalog, CatalogPath);

            PopulateDefaults(catalog, force: true);

        }

        else

        {

            PopulateDefaults(catalog, force: false);

        }



        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();

        return catalog;

    }



    /// <summary>에디터 메뉴용 — 사용자 매핑을 모두 지우고 공장 기본값으로 되돌립니다.</summary>

    public static void ResetCatalogToFactoryDefaults()

    {

        var catalog = AssetDatabase.LoadAssetAtPath<DefenseCombatCatalog>(CatalogPath);

        if (catalog == null)

        {

            EnsureCatalogAsset();

            return;

        }



        PopulateDefaults(catalog, force: true);

        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();

        Debug.Log("[DefenseCombatCatalog] 공장 기본값으로 초기화했습니다.");

    }



    private static void PopulateDefaults(DefenseCombatCatalog catalog, bool force)

    {

        if (force)

            DefenseMonsterStatusVfxDefaults.ApplyToCatalog(catalog);

        else if (catalog.monsterStatusVfx == null || catalog.monsterStatusVfx.Count == 0)

            DefenseMonsterStatusVfxDefaults.ApplyToCatalog(catalog);



        if (catalog.meteorMissilePrefab == null)

            catalog.meteorMissilePrefab = LoadPrefab(MeteorMissilePath);

        if (catalog.meteorExplosionPrefab == null)

            catalog.meteorExplosionPrefab = LoadPrefab(MeteorExplosionPath);

        if (catalog.chainBoltPrefab == null)

            catalog.chainBoltPrefab = LoadPrefab(ChainBoltPath);

        if (catalog.chainHitExplosionPrefab == null)

            catalog.chainHitExplosionPrefab = LoadPrefab(ChainHitExplosionPath);

        else

            EnsureLightningStrikeHitPrefab(catalog);

        if (catalog.stunHeadEffectPrefab == null)

            catalog.stunHeadEffectPrefab = LoadPrefab(StunHeadEffectPath);

        if (catalog.stunBodyEffectPrefab == null)

            catalog.stunBodyEffectPrefab = LoadPrefab(StunBodyEffectPath);

        if (catalog.defaultDeathEffectPrefab == null)

            catalog.defaultDeathEffectPrefab = LoadPrefab(DeathEffectPath);



        catalog.RebuildLookups();

    }



    private static void EnsureLightningStrikeHitPrefab(DefenseCombatCatalog catalog)

    {

        var path = AssetDatabase.GetAssetPath(catalog.chainHitExplosionPrefab);

        if (string.IsNullOrWhiteSpace(path))

            return;



        if (path.Contains("CFX3_Hit_Electric", System.StringComparison.OrdinalIgnoreCase))

            catalog.chainHitExplosionPrefab = LoadPrefab(ChainHitExplosionPath);

    }



    private static GameObject LoadPrefab(string path)

    {

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);

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

