using UnityEngine;

public static class CwslUiSpriteUtil
{
    private static Sprite whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite != null)
                return whiteSprite;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "CwslUiWhite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(1f, 1f, 1f, 1f));
            whiteSprite.name = "CwslUiWhiteSprite";
            whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSprite;
        }
    }
}
