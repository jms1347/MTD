using UnityEngine;

public readonly struct CwslMonsterPalette
{
    public readonly Color Primary;
    public readonly Color Secondary;
    public readonly Color Accent;
    public readonly Color Metal;

    public CwslMonsterPalette(Color primary, Color secondary, Color accent, Color metal)
    {
        Primary = primary;
        Secondary = secondary;
        Accent = accent;
        Metal = metal;
    }
}

public static class CwslMonsterVisualPalette
{
    private static readonly CwslMonsterPalette NexusYellow = new(
        new Color(1f, 0.9f, 0.22f),
        new Color(0.92f, 0.72f, 0.08f),
        new Color(1f, 0.98f, 0.55f),
        new Color(0.62f, 0.52f, 0.14f));

    private static readonly CwslMonsterPalette MeleeRed = new(
        new Color(0.92f, 0.18f, 0.14f),
        new Color(0.62f, 0.08f, 0.1f),
        new Color(1f, 0.42f, 0.28f),
        new Color(0.42f, 0.42f, 0.46f));

    private static readonly CwslMonsterPalette RangedNavy = new(
        new Color(0.14f, 0.22f, 0.58f),
        new Color(0.08f, 0.12f, 0.38f),
        new Color(0.28f, 0.45f, 0.82f),
        new Color(0.35f, 0.38f, 0.42f));

    private static readonly CwslMonsterPalette SuicideOrange = new(
        new Color(0.95f, 0.32f, 0.08f),
        new Color(0.18f, 0.16f, 0.16f),
        new Color(1f, 0.55f, 0.12f),
        new Color(0.25f, 0.22f, 0.2f));

    private static readonly CwslMonsterPalette BossRobot = new(
        new Color(0.55f, 0.58f, 0.64f),
        new Color(0.32f, 0.34f, 0.38f),
        new Color(0.95f, 0.22f, 0.18f),
        new Color(0.72f, 0.76f, 0.82f));

    public static CwslMonsterPalette GetPalette(CwslMonsterType type)
    {
        if (CwslMonsterTypeUtil.IsNexusPriority(type))
            return NexusYellow;

        return type switch
        {
            CwslMonsterType.Melee or CwslMonsterType.MidBoss => MeleeRed,
            CwslMonsterType.Ranged => RangedNavy,
            CwslMonsterType.Suicide => SuicideOrange,
            CwslMonsterType.DefenseBoss or CwslMonsterType.BossHongmyeongbo => BossRobot,
            _ => MeleeRed
        };
    }

    public static Color GetThreatLightColor(CwslMonsterType type)
    {
        if (CwslMonsterTypeUtil.IsNexusPriority(type))
            return new Color(1f, 0.88f, 0.2f);

        return type switch
        {
            CwslMonsterType.Ranged or CwslMonsterType.NexusRanged => new Color(0.45f, 0.55f, 1f),
            CwslMonsterType.Suicide or CwslMonsterType.NexusSuicide => new Color(1f, 0.35f, 0.1f),
            CwslMonsterType.DefenseBoss or CwslMonsterType.MidBoss or CwslMonsterType.BossHongmyeongbo =>
                new Color(1f, 0.3f, 0.22f),
            _ => new Color(1f, 0.25f, 0.2f)
        };
    }
}
