using System.Collections.Generic;

/// <summary>
/// 전투 중 자주 순회하는 오브젝트를 등록해 FindObjectsByType 비용을 제거한다.
/// </summary>
public static class CwslCombatRegistry
{
    private static readonly HashSet<CwslMonsterHealth> monsterSet = new();
    private static readonly List<CwslMonsterHealth> monsters = new(256);
    private static readonly HashSet<CwslPlayerHealth> playerSet = new();
    private static readonly List<CwslPlayerHealth> players = new(8);
    private static readonly HashSet<CwslMonsterProjectile> monsterProjectileSet = new();
    private static readonly List<CwslMonsterProjectile> monsterProjectiles = new(128);
    private static readonly HashSet<CwslPlayerProjectile> playerProjectileSet = new();
    private static readonly List<CwslPlayerProjectile> playerProjectiles = new(64);
    private static readonly HashSet<CwslGoldPickup> goldPickupSet = new();
    private static readonly List<CwslGoldPickup> goldPickups = new(256);
    private static readonly HashSet<CwslPillPickup> pillPickupSet = new();
    private static readonly List<CwslPillPickup> pillPickups = new(32);

    public static IReadOnlyList<CwslMonsterHealth> AliveMonsters => monsters;
    public static IReadOnlyList<CwslPlayerHealth> AlivePlayers => players;
    public static IReadOnlyList<CwslMonsterProjectile> ActiveMonsterProjectiles => monsterProjectiles;
    public static IReadOnlyList<CwslPlayerProjectile> ActivePlayerProjectiles => playerProjectiles;
    public static IReadOnlyList<CwslGoldPickup> ActiveGoldPickups => goldPickups;
    public static IReadOnlyList<CwslPillPickup> ActivePillPickups => pillPickups;

    public static void RegisterMonster(CwslMonsterHealth monster)
    {
        if (monster == null || !monsterSet.Add(monster))
            return;

        monsters.Add(monster);
    }

    public static void UnregisterMonster(CwslMonsterHealth monster)
    {
        if (monster == null || !monsterSet.Remove(monster))
            return;

        monsters.Remove(monster);
    }

    public static void RegisterPlayer(CwslPlayerHealth player)
    {
        if (player == null || !playerSet.Add(player))
            return;

        players.Add(player);
    }

    public static void UnregisterPlayer(CwslPlayerHealth player)
    {
        if (player == null || !playerSet.Remove(player))
            return;

        players.Remove(player);
    }

    public static void RegisterMonsterProjectile(CwslMonsterProjectile projectile)
    {
        if (projectile == null || !monsterProjectileSet.Add(projectile))
            return;

        monsterProjectiles.Add(projectile);
    }

    public static void UnregisterMonsterProjectile(CwslMonsterProjectile projectile)
    {
        if (projectile == null || !monsterProjectileSet.Remove(projectile))
            return;

        monsterProjectiles.Remove(projectile);
    }

    public static void RegisterPlayerProjectile(CwslPlayerProjectile projectile)
    {
        if (projectile == null || !playerProjectileSet.Add(projectile))
            return;

        playerProjectiles.Add(projectile);
    }

    public static void UnregisterPlayerProjectile(CwslPlayerProjectile projectile)
    {
        if (projectile == null || !playerProjectileSet.Remove(projectile))
            return;

        playerProjectiles.Remove(projectile);
    }

    public static void RegisterGoldPickup(CwslGoldPickup pickup)
    {
        if (pickup == null || !goldPickupSet.Add(pickup))
            return;

        goldPickups.Add(pickup);
    }

    public static void UnregisterGoldPickup(CwslGoldPickup pickup)
    {
        if (pickup == null || !goldPickupSet.Remove(pickup))
            return;

        goldPickups.Remove(pickup);
    }

    public static void RegisterPillPickup(CwslPillPickup pickup)
    {
        if (pickup == null || !pillPickupSet.Add(pickup))
            return;

        pillPickups.Add(pickup);
    }

    public static void UnregisterPillPickup(CwslPillPickup pickup)
    {
        if (pickup == null || !pillPickupSet.Remove(pickup))
            return;

        pillPickups.Remove(pickup);
    }
}
