using UnityEngine;

public static class CwslArenaUtility
{
    public static Vector3 GetRandomSpawnPosition()
    {
        var x = Random.Range(-CwslGameConstants.ArenaHalfExtent, CwslGameConstants.ArenaHalfExtent);
        var z = Random.Range(-CwslGameConstants.ArenaHalfExtent, CwslGameConstants.ArenaHalfExtent);
        return new Vector3(x, CwslGameConstants.SpawnHeight, z);
    }

    public static Vector3 ClampToArena(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, -CwslGameConstants.ArenaHalfExtent, CwslGameConstants.ArenaHalfExtent);
        position.z = Mathf.Clamp(position.z, -CwslGameConstants.ArenaHalfExtent, CwslGameConstants.ArenaHalfExtent);
        return position;
    }
}
