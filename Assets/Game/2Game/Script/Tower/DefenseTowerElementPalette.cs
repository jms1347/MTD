using UnityEngine;

public static class DefenseTowerElementPalette
{
    public readonly struct TowerColors
    {
        public readonly Color Base;
        public readonly Color Accent;
        public readonly Color Secondary;

        public TowerColors(Color baseColor, Color accent, Color secondary)
        {
            Base = baseColor;
            Accent = accent;
            Secondary = secondary;
        }
    }

    public static TowerColors Get(DefenseSkillElement element)
    {
        return element switch
        {
            DefenseSkillElement.Fire => new TowerColors(
                new Color(0.28f, 0.14f, 0.12f),
                new Color(0.92f, 0.18f, 0.12f),
                new Color(0.55f, 0.12f, 0.08f)),

            DefenseSkillElement.Water => new TowerColors(
                new Color(0.18f, 0.28f, 0.42f),
                new Color(0.12f, 0.42f, 0.95f),
                new Color(0.22f, 0.55f, 1f)),

            DefenseSkillElement.Ice => new TowerColors(
                new Color(0.45f, 0.58f, 0.68f),
                new Color(0.55f, 0.88f, 1f),
                new Color(0.72f, 0.94f, 1f)),

            DefenseSkillElement.Lightning => new TowerColors(
                new Color(0.22f, 0.22f, 0.26f),
                new Color(1f, 0.9f, 0.15f),
                new Color(0.95f, 0.78f, 0.1f)),

            DefenseSkillElement.Poison => new TowerColors(
                new Color(0.12f, 0.42f, 0.18f),
                new Color(0.58f, 0.18f, 0.78f),
                new Color(0.28f, 0.72f, 0.28f)),

            _ => new TowerColors(
                new Color(0.34f, 0.34f, 0.36f),
                new Color(0.1f, 0.1f, 0.12f),
                new Color(0.22f, 0.22f, 0.24f))
        };
    }
}
