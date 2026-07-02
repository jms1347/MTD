using System.Globalization;

public static class SheetParseUtility
{
    public static bool ParseYn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();
        return normalized.Equals("Y", System.StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("YES", System.StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("1", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseFloat(string value, out float result)
    {
        result = 0f;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseInt(string value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>"1.2 (20% 증가)" 같이 설명이 붙은 셀에서 앞쪽 숫자만 파싱합니다.</summary>
    public static bool TryParseLeadingFloat(string value, out float result)
    {
        result = 0f;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var end = 0;
        while (end < trimmed.Length &&
               (char.IsDigit(trimmed[end]) || trimmed[end] == '.' || trimmed[end] == '-' || trimmed[end] == '+'))
            end++;

        if (end == 0)
            return false;

        return float.TryParse(trimmed.Substring(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
