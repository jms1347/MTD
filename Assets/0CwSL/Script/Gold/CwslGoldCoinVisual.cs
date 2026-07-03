using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslGoldCoinVisual
{
    private const string CoinAttractionPrefabPath =
        "Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab";
    private const string FlyParticleResourcePath = "CwslGold/CoinFlyParticle";

    private static GameObject flyParticleTemplate;

    public static GameObject FlyParticleTemplate => flyParticleTemplate ??= LoadFlyParticleTemplate();

    private static GameObject LoadFlyParticleTemplate()
    {
        var fromResources = Resources.Load<GameObject>(FlyParticleResourcePath);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        var coinAttraction = AssetDatabase.LoadAssetAtPath<GameObject>(CoinAttractionPrefabPath);
        var particle = coinAttraction != null
            ? coinAttraction.transform.Find("Particle Image")?.gameObject
            : null;
        if (particle != null)
            return particle;
#endif

        Debug.LogWarning("[CwSL] CoinFlyParticle 프리팹이 없습니다. Tools → CwSL → Setup Game Scene을 실행하세요.");
        return null;
    }
}
