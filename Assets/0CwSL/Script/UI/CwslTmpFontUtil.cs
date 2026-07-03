using TMPro;
using UnityEngine;

public static class CwslTmpFontUtil
{
    private static TMP_FontAsset cachedFont;

    public static TMP_FontAsset ResolveFont()
    {
        if (cachedFont != null)
            return cachedFont;

        if (TMP_Settings.defaultFontAsset != null)
            return cachedFont = TMP_Settings.defaultFontAsset;

        cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NEXONLv1GothicBold SDF")
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/esamanru Bold SDF")
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/PretendardVariable SDF");

        return cachedFont;
    }

    public static void ApplyFont(TMP_Text text)
    {
        if (text == null)
            return;

        var font = ResolveFont();
        if (font != null)
            text.font = font;
    }

    public static void ApplyFont(TextMeshPro text) => ApplyFont((TMP_Text)text);

    public static void ApplyFont(TextMeshProUGUI text) => ApplyFont((TMP_Text)text);
}
