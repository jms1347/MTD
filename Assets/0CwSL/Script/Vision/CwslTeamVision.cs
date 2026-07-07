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

    public static IReadOnlyList<CwslTeamVisionSource> CollectSources()
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

        var playerVisions = Object.FindObjectsByType<CwslPlayerVision>(FindObjectsSortMode.None);
        foreach (var vision in playerVisions)
        {
            if (vision == null || !vision.IsSpawned)
                continue;

            var health = vision.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            SourceBuffer.Add(new CwslTeamVisionSource
            {
                Origin = vision.VisionOrigin,
                Radius = vision.EffectiveVisionRadius,
                BlindVision = vision.IsBlindVision,
                PlayerVision = vision
            });

            if (SourceBuffer.Count >= MaxSources)
                break;
        }

        return SourceBuffer;
    }

    public static float EvaluateTeamVisibility(Vector3 worldPosition, bool isProjectile)
    {
        if (CwslPlayerVision.Local == null)
            return 1f;

        var maxVisibility = 0f;
        foreach (var source in CollectSources())
        {
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
