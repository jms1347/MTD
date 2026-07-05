using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslGoldCoinVisual
{
    private const string CoinSpritePath = "Assets/AssetKits/ParticleImage/Demo/Sprites/Coin.png";
    private const string FlyCoinResourcePath = "CwslGold/CwslGoldFlyCoin";

    private static GameObject flyCoinTemplate;
    private static Sprite coinSprite;

    public static GameObject FlyCoinTemplate => flyCoinTemplate ??= LoadFlyCoinTemplate();

    public static Sprite GetCoinSprite() => coinSprite ??= LoadCoinSprite();

    private static GameObject LoadFlyCoinTemplate()
    {
        var fromResources = Resources.Load<GameObject>(FlyCoinResourcePath);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/0CwSL/Resources/{FlyCoinResourcePath}.prefab");
        if (editorPrefab != null)
            return editorPrefab;
#endif

        Debug.LogWarning("[CwSL] CwslGoldFlyCoin 프리팹이 없습니다. 런타임 템플릿을 사용합니다. Tools → CwSL → Setup Game Scene 권장.");
        return CreateRuntimeTemplate();
    }

    private static GameObject CreateRuntimeTemplate()
    {
        var coinObject = BuildCoinObject("CwslGoldFlyCoinTemplate");
        coinObject.SetActive(false);
        Object.DontDestroyOnLoad(coinObject);
        return coinObject;
    }

    private static GameObject BuildCoinObject(string objectName)
    {
        var coinObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CwslGoldFlyCoin));

        var rect = coinObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20f, 20f);

        var image = coinObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.white;
        image.sprite = LoadCoinSprite();

        var trailObject = new GameObject("Trail", typeof(RectTransform), typeof(CwslGoldFlyCoinTrail));
        trailObject.transform.SetParent(coinObject.transform, false);
        return coinObject;
    }

    private static Sprite LoadCoinSprite()
    {
        if (coinSprite != null)
            return coinSprite;

#if UNITY_EDITOR
        coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);
        if (coinSprite != null)
            return coinSprite;
#endif

        coinSprite = CreateFallbackSprite();
        return coinSprite;
    }

    private static Sprite CreateFallbackSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;

        var center = (size - 1) * 0.5f;
        var radius = size * 0.38f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 0.84f, 0.15f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
