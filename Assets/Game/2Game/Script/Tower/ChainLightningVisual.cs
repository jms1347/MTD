using UnityEngine;

/// <summary>
/// 두 지점 사이에 번개 이펙트를 잠깐 표시합니다.
/// </summary>
public static class ChainLightningVisual
{
    public static void PlayBolt(Vector3 from, Vector3 to, GameObject boltPrefab, float lifetime = 0.28f)
    {
        if (boltPrefab == null)
            return;

        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.05f)
            return;

        Vector3 midpoint = (from + to) * 0.5f;
        var bolt = Object.Instantiate(boltPrefab, midpoint, Quaternion.LookRotation(direction.normalized, Vector3.up));

        float lengthScale = Mathf.Max(distance * 0.22f, 0.6f);
        bolt.transform.localScale = new Vector3(lengthScale, lengthScale, lengthScale);
        Object.Destroy(bolt, lifetime);
    }
}
