using UnityEngine;

/// <summary>
/// 런타임 UI Image에 사용할 1x1 흰색 스프라이트를 제공합니다.
/// 별도 UI 에셋 없이 색상만 입혀 바·아이콘·테두리를 그릴 때 사용합니다.
/// </summary>
public static class DefenseUISprites
{
    private static Sprite whiteSprite;

    public static Sprite White
    {
        get
        {
            if (whiteSprite != null)
                return whiteSprite;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSprite;
        }
    }
}
