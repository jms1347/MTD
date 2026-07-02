using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AssetKits ParticleImage 코인 스프라이트/프리팹 로드.
/// </summary>
public static class DefenseGoldCoinVisual
{
    private const string CoinSpritePath = "Assets/AssetKits/ParticleImage/Demo/Sprites/Coin.png";
    private const string CoinRotationSpritePath = "Assets/AssetKits/ParticleImage/Demo/Sprites/Coin_Rotation.png";
    private const string CoinAttractionPrefabPath = "Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab";
    private const string FlyParticleResourcePath = "DefenseGold/CoinFlyParticle";
    private const string HudCoinResourcePath = "DefenseGold/Coin";

    private static Sprite hudCoinSprite;
    private static Sprite rotationCoinSprite;
    private static GameObject flyParticleTemplate;

    public static Sprite HudCoinSprite => hudCoinSprite ??= LoadSprite(CoinSpritePath);

    public static Sprite RotationCoinSprite => rotationCoinSprite ??= LoadSprite(CoinRotationSpritePath);

    public static GameObject FlyParticleTemplate => flyParticleTemplate ??= LoadFlyParticleTemplate();

    public static void ApplyHudIcon(Image image)
    {
        if (image == null)
            return;

        var sprite = HudCoinSprite;
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        var fromResources = Resources.Load<Sprite>(HudCoinResourcePath);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
        return null;
#endif
    }

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

        Debug.LogWarning(
            "[DefenseGoldCoinVisual] CoinFlyParticle 프리팹을 찾을 수 없습니다. " +
            "UkDefense → Setup → Create Defense Gold Coin Fly Prefab 실행");
        return null;
    }
}
