using UnityEngine;

/// <summary>
/// 풀에서 꺼낸 VFX의 자동 반환 타이머.
/// </summary>
public class CwslPooledVfxHandle : MonoBehaviour
{
    private GameObject sourcePrefab;
    private float releaseAt = -1f;

    public GameObject SourcePrefab => sourcePrefab;

    public void Bind(GameObject prefab)
    {
        sourcePrefab = prefab;
    }

    public void CancelAutoRelease()
    {
        releaseAt = -1f;
    }

    public void ScheduleAutoRelease(float lifetimeSeconds)
    {
        if (lifetimeSeconds <= 0f)
        {
            releaseAt = -1f;
            return;
        }

        releaseAt = Time.time + lifetimeSeconds;
    }

    private void Update()
    {
        if (releaseAt < 0f || Time.time < releaseAt)
            return;

        releaseAt = -1f;
        CwslVfxPool.Release(gameObject);
    }
}
