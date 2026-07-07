/// <summary>비주얼 테스트 씬에서 CwslGameSession 없이 VFX 에셋을 참조합니다.</summary>
public static class CwslVisualTestAssetsContext
{
    public static CwslGameAssets Assets { get; private set; }

    public static void Set(CwslGameAssets assets) => Assets = assets;
}
