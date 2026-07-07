public static class CwslPrefabPaths
{
    public const string Root = "Assets/0CwSL/Prefabs";

    public static class Characters
    {
        public const string Folder = Root + "/Characters/Player";
        public const string Player = Folder + "/CwslPlayer.prefab";
    }

    public static class Boss
    {
        public const string Folder = Root + "/Boss";
        public const string Hongmyeongbo = Folder + "/CwslBoss_Hongmyeongbo.prefab";
    }

    public static class Monsters
    {
        public const string MeleeFolder = Root + "/Monsters/Melee";
        public const string Melee = MeleeFolder + "/CwslMonster_Melee.prefab";
        public const string NexusMelee = MeleeFolder + "/CwslMonster_NexusMelee.prefab";
        public const string KoreaUniversitySoldier = MeleeFolder + "/CwslMonster_KoreaUniversitySoldier.prefab";

        public const string RangedFolder = Root + "/Monsters/Ranged";
        public const string Ranged = RangedFolder + "/CwslMonster_Ranged.prefab";
        public const string InkSniper = RangedFolder + "/CwslMonster_InkSniper.prefab";

        public const string SuicideFolder = Root + "/Monsters/Suicide";
        public const string SuicideRush = SuicideFolder + "/CwslMonster_Suicide.prefab";
        public const string SuicideSticky = SuicideFolder + "/CwslMonster_StickySuicide.prefab";

        public const string EliteFolder = Root + "/Monsters/Elite";
        public const string MidBoss = EliteFolder + "/CwslMonster_MidBoss.prefab";
        public const string DefenseBoss = EliteFolder + "/CwslMonster_DefenseBoss.prefab";
        public const string SeniorCoach = EliteFolder + "/CwslMonster_SeniorCoach.prefab";
    }

    public static class Projectiles
    {
        public const string Folder = Root + "/Projectiles";
        public const string Monster = Folder + "/CwslProjectile.prefab";
        public const string BossSkill = Folder + "/CwslBossSkillProjectile.prefab";
        public const string PlayerMissile = Folder + "/CwslPlayerMissile.prefab";
        public const string FrozenOrb = Folder + "/CwslFrozenOrb.prefab";
    }

    public static class Pickups
    {
        public const string Folder = Root + "/Pickups";
        public const string Gold = Folder + "/CwslGoldPickup.prefab";
        public const string Pill = Folder + "/CwslPillPickup.prefab";
    }

    public static class Defense
    {
        public const string Folder = Root + "/Defense";
        public const string Nexus = Folder + "/CwslNexus.prefab";
        public const string EnemyBase = Folder + "/CwslEnemyBase.prefab";
    }

    public static class Visuals
    {
        public const string Folder = Root + "/Visuals";
        public const string Grave = Folder + "/CwslGraveVisual.prefab";
    }

    public static string GetMonsterPrefabPath(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Melee => Monsters.Melee,
            CwslMonsterType.NexusMelee => Monsters.NexusMelee,
            CwslMonsterType.Ranged or CwslMonsterType.NexusRanged => Monsters.Ranged,
            CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper => Monsters.InkSniper,
            CwslMonsterType.Suicide or CwslMonsterType.NexusSuicide => Monsters.SuicideRush,
            CwslMonsterType.StickySuicide => Monsters.SuicideSticky,
            CwslMonsterType.KoreaUniversitySoldier => Monsters.KoreaUniversitySoldier,
            CwslMonsterType.MidBoss => Monsters.MidBoss,
            CwslMonsterType.DefenseBoss => Monsters.DefenseBoss,
            CwslMonsterType.SeniorCoach => Monsters.SeniorCoach,
            _ => Root + "/Monsters/_Generated/CwslMonster_" + type + ".prefab"
        };
    }

#if UNITY_EDITOR
    public static void EnsureFoldersExist()
    {
        EnsureFolder(Characters.Folder);
        EnsureFolder(Boss.Folder);
        EnsureFolder(Monsters.MeleeFolder);
        EnsureFolder(Monsters.RangedFolder);
        EnsureFolder(Monsters.SuicideFolder);
        EnsureFolder(Monsters.EliteFolder);
        EnsureFolder(Projectiles.Folder);
        EnsureFolder(Pickups.Folder);
        EnsureFolder(Defense.Folder);
        EnsureFolder(Visuals.Folder);
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (UnityEditor.AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        var parent = assetFolderPath.Substring(0, assetFolderPath.LastIndexOf('/'));
        var name = assetFolderPath.Substring(assetFolderPath.LastIndexOf('/') + 1);
        if (!UnityEditor.AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        UnityEditor.AssetDatabase.CreateFolder(parent, name);
    }
#endif
}
