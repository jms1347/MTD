using UnityEngine;

/// <summary>
/// 지면 폭발 그을림 데칼 — 2~3초에 걸쳐 서서히 사라집니다. 순수 연출이며 몬스터에게 화상 등 상태이상을 주지 않습니다.
/// </summary>
public class DefenseGroundScorchFade : MonoBehaviour
{
    private ParticleSystem[] systems;
    private Color[] baseColors;
    private float elapsed;
    private float lifetime = 2.5f;

    public void Play(float duration)
    {
        lifetime = Mathf.Clamp(duration, 1f, 6f);
        elapsed = 0f;
        CacheSystems();
    }

    private void OnEnable()
    {
        if (systems == null || systems.Length == 0)
            CacheSystems();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / lifetime);
        ApplyAlpha(alpha);

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void CacheSystems()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        baseColors = new Color[systems.Length];

        for (int i = 0; i < systems.Length; i++)
        {
            var main = systems[i].main;
            baseColors[i] = main.startColor.color;
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (systems == null || baseColors == null)
            return;

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null)
                continue;

            var color = baseColors[i];
            color.a *= alpha;
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }
    }
}
