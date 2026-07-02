using System.Collections.Generic;
using AssetKits.ParticleImage;
using UnityEngine;

/// <summary>
/// Time.timeScale=0 일시정지 시 ParticleImage가 깨지는 문제를 막기 위한 보조 클래스.
/// (Quaternion assertion / Invalid AABB)
/// </summary>
public static class DefenseUIParticlePause
{
    private static readonly List<ParticleImage> Suspended = new();

    public static void Suspend()
    {
        Resume();

        var particles = Object.FindObjectsByType<ParticleImage>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < particles.Length; i++)
        {
            var particle = particles[i];
            if (particle == null)
                continue;

            if (particle.gameObject.name.Contains("GoldCoinFly"))
            {
                Object.Destroy(particle.gameObject);
                continue;
            }

            particle.Stop(true);
            particle.enabled = false;
            Suspended.Add(particle);
        }
    }

    public static void Resume()
    {
        for (int i = 0; i < Suspended.Count; i++)
        {
            var particle = Suspended[i];
            if (particle == null)
                continue;

            particle.enabled = true;
        }

        Suspended.Clear();
    }
}
