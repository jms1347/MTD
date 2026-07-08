using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군 플레이어·넥서스 시야를 합집합으로 판정.
/// </summary>
public static class CwslTeamVision
{
    public const int MaxSources = CwslGameConstants.MaxPlayers + 1;

    public struct CwslTeamVisionSource
    {
        public Vector3 Origin;
        public float Radius;
        public bool BlindVision;
        public CwslPlayerVision PlayerVision;
    }

    private static readonly List<CwslTeamVisionSource> SourceBuffer = new(MaxSources);
    private static float cachedAt = -1f;

    public static IReadOnlyList<CwslTeamVisionSource> CollectSources()
    {
        if (Time.time - cachedAt < CwslGameConstants.TeamVisionSourceCacheSeconds && SourceBuffer.Count > 0)
            return SourceBuffer;

        RefreshSources();
        cachedAt = Time.time;
        return SourceBuffer;
    }

    public static void InvalidateSourceCache()
    {
        cachedAt = -1f;
    }

    private static void RefreshSources()
    {
        SourceBuffer.Clear();

        var nexus = CwslNexus.Instance;
        if (nexus != null && nexus.IsAlive)
        {
            SourceBuffer.Add(new CwslTeamVisionSource
            {
                Origin = nexus.transform.position,
                Radius = CwslGameConstants.NexusTeamVisionRadius,
                BlindVision = false,
                PlayerVision = null
            });
        }

        var players = CwslCombatRegistry.AlivePlayers;
        for (var i = 0; i < players.Count; i++)
        {
            var health = players[i];
            if (health == null || !health.IsAlive)
                continue;

            var vision = health.GetComponent<CwslPlayerVision>();
            if (vision == null || !vision.IsSpawned)
                continue;

            SourceBuffer.Add(new CwslTeamVisionSource
            {
                Origin = vision.VisionOrigin,
                Radius = vision.EffectiveVisionRadius,
                BlindVision = vision.IsBlindVision,
                PlayerVision = vision
            });

            if (SourceBuffer.Count >= MaxSources)
                return;
        }
    }

    public static float EvaluateTeamVisibility(
        Vector3 worldPosition,
        bool isProjectile,
        IReadOnlyList<CwslTeamVisionSource> sources = null)
    {
        if (CwslPlayerVision.Local == null)
            return 1f;

        sources ??= CollectSources();

        var maxVisibility = 0f;
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            if (source.PlayerVision != null && source.PlayerVision.IsAbsoluteBlindVision)
            {
                maxVisibility = Mathf.Max(
                    maxVisibility,
                    source.PlayerVision.TryGetScryVisibility(worldPosition, isProjectile));
                continue;
            }

            var visibility = CwslLocalVisionSystem.EvaluateVisibility(
                source.Origin,
                worldPosition,
                source.Radius,
                source.BlindVision,
                isProjectile);

            if (source.PlayerVision != null && source.PlayerVision.HasActiveScry)
            {
                visibility = Mathf.Max(
                    visibility,
                    source.PlayerVision.TryGetScryVisibility(worldPosition, isProjectile));
            }

            maxVisibility = Mathf.Max(maxVisibility, visibility);
        }

        return maxVisibility;
    }

    public static bool IsInTeamVision(Vector3 worldPosition)
    {
        return EvaluateTeamVisibility(worldPosition, isProjectile: false) > 0.01f;
    }
}
