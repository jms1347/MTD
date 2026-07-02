/// <summary>
/// 구글 시트 문자열 코드(N-0001, M-F-0001)와 레거시 숫자 ID를 통합 해석합니다.
/// </summary>
public static class SheetCodeUtility
{
    public static bool TryResolveSheetId(string value, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (SheetParseUtility.TryParseInt(trimmed, out id) && id > 0)
            return true;

        id = ToStablePositiveId(trimmed);
        return id > 0;
    }

    public static string NormalizeCode(string value, int resolvedId)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        return resolvedId > 0 ? resolvedId.ToString() : string.Empty;
    }

    /// <summary>동일 문자열 코드는 항상 같은 양의 정수 ID를 반환합니다.</summary>
    public static int ToStablePositiveId(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return 0;

        uint hash = 2166136261;
        var text = code.Trim();
        for (int i = 0; i < text.Length; i++)
            hash = (hash ^ text[i]) * 16777619;

        var result = (int)(hash & 0x7FFFFFFF);
        return result == 0 ? 1 : result;
    }
}
