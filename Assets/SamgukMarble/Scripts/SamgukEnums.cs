using UnityEngine;

namespace SamgukMarble
{
    public enum ColorGroup
    {
        WeiBlue = 0,
        ShuGreen = 1,
        JingzhouYellow = 2,
        WuRed = 3,
        CentralPurple = 4
    }

    public enum TileType
    {
        Start,
        Castle,
        LuckyCard,
        UnluckyCard,
        ChanceCard,
        Crossroad,
        Market,
        Treasury,
        Exile,
        Special,
        Gate,
        Throne
    }

    public enum BuildingType
    {
        None = 0,
        Barracks = 1,
        TaxOffice = 2,
        Watchtower = 3,
        Landmark = 4
    }

    public enum CardKind
    {
        Lucky,
        Unlucky,
        Chance
    }

    public static class SamgukColors
    {
        public static readonly Color Wei = new Color(0.25f, 0.45f, 0.95f);
        public static readonly Color Shu = new Color(0.25f, 0.75f, 0.35f);
        public static readonly Color Jingzhou = new Color(0.95f, 0.82f, 0.2f);
        public static readonly Color Wu = new Color(0.9f, 0.25f, 0.25f);
        public static readonly Color Central = new Color(0.65f, 0.3f, 0.85f);
        public static readonly Color BoardBase = new Color(0.92f, 0.88f, 0.78f);
        public static readonly Color Ground = new Color(0.45f, 0.62f, 0.4f);

        public static Color Get(ColorGroup group)
        {
            switch (group)
            {
                case ColorGroup.WeiBlue: return Wei;
                case ColorGroup.ShuGreen: return Shu;
                case ColorGroup.JingzhouYellow: return Jingzhou;
                case ColorGroup.WuRed: return Wu;
                default: return Central;
            }
        }
    }
}
