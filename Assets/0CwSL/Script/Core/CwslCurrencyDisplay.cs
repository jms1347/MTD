public static class CwslCurrencyDisplay
{
    public const long WonPerGold = 1_000_000L;

    public static string FormatGold(int gold) => FormatWon(gold * WonPerGold);

    public static string FormatKarma(long karma) => FormatWon(karma * WonPerGold);

    public static string FormatWon(long won)
    {
        if (won >= 100_000_000L)
        {
            var eok = won / 100_000_000d;
            return eok >= 10d ? $"{eok:0}억" : $"{eok:0.#}억";
        }

        if (won >= 10_000L)
            return $"{won / 10_000d:0.#}만";

        return $"{won:N0}원";
    }
}
