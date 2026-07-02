using UnityEngine;

public class CoopMonsterSpawner : MonoBehaviour
{
    private CoopGameSession session;
    private CoopMapGimmicks gimmicks;
    private readonly System.Random random = new();

    public void Initialize(CoopGameSession gameSession)
    {
        session = gameSession;
        gimmicks = CoopMapGimmicks.Instance;
    }

    public bool TrySpawnForWave(int wave, bool bossWave)
    {
        if (session == null)
            return false;

        var pattern = CoopMapGimmicks.RollPattern(wave, random);
        var forceBoss = bossWave && random.NextDouble() < 0.4d;
        var position = ResolveSpawnPosition(pattern);
        session.SpawnFallbackEnemyAt(position, forceBoss, wave);
        return true;
    }

    public void TrySpawnAmbushBurst(Vector3 nearWorld, int count, int wave)
    {
        if (session == null || count <= 0)
            return;

        for (var i = 0; i < count; i++)
        {
            var origin = gimmicks != null
                ? gimmicks.PickAmbushBurstOrigin(nearWorld, random)
                : nearWorld + new Vector3(random.Next(-3, 4), 0f, random.Next(-3, 4));
            session.SpawnFallbackEnemyAt(origin, false, wave);
        }
    }

    private Vector3 ResolveSpawnPosition(CoopSpawnPattern pattern)
    {
        var players = CollectPlayerPositions();
        if (gimmicks != null)
            return gimmicks.PickSpawnPosition(pattern, random, players);

        if (pattern == CoopSpawnPattern.AmbushNearPlayer && players.Count > 0)
        {
            var player = players[random.Next(players.Count)];
            var angle = (float)(random.NextDouble() * System.Math.PI * 2d);
            return player + new Vector3(Mathf.Cos(angle) * 9f, 0f, Mathf.Sin(angle) * 9f);
        }

        var fallbackAngle = (float)(random.NextDouble() * System.Math.PI * 2d);
        return new Vector3(Mathf.Cos(fallbackAngle) * 16f, 0f, Mathf.Sin(fallbackAngle) * 16f);
    }

    private System.Collections.Generic.List<Vector3> CollectPlayerPositions()
    {
        var positions = new System.Collections.Generic.List<Vector3>();
        if (session == null)
            return positions;

        foreach (var player in session.EnumeratePlayerStates())
            positions.Add(new Vector3(player.towerX, 0f, player.towerZ));

        return positions;
    }
}
