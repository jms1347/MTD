using UnityEngine;

public static class CoopEnemyExplosionVfx
{
    public static void Spawn(Vector3 position, float radius, bool heavy = false)
    {
        CoopCombatVfxCache.EnsureInitialized();
        CoopCombatVfxCache.PlayExplosion(position + Vector3.up * 0.2f, heavy);
    }
}
