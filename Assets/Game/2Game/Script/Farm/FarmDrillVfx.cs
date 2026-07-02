using UnityEngine;

/// <summary>
/// 드릴 완료 골드·흙 파편 이펙트를 재생합니다.
/// </summary>
public static class FarmDrillVfx
{
    private static GameObject goldBurstPrefab;
    private static GameObject debrisBurstPrefab;

    public static void Initialize(GameObject goldPrefab, GameObject debrisPrefab)
    {
        goldBurstPrefab = goldPrefab;
        debrisBurstPrefab = debrisPrefab;
    }

    public static void PlayGoldBurst(Vector3 worldPosition)
    {
        if (goldBurstPrefab == null)
            return;

        var instance = Object.Instantiate(goldBurstPrefab, worldPosition + Vector3.up * 0.35f, Quaternion.identity);
        Object.Destroy(instance, 4f);
    }

    public static void PlayDebrisBurst(Vector3 worldPosition)
    {
        if (debrisBurstPrefab == null)
            return;

        Vector3 offset = new Vector3(
            UnityEngine.Random.Range(-0.18f, 0.18f),
            0.12f,
            UnityEngine.Random.Range(-0.18f, 0.18f));

        var instance = Object.Instantiate(debrisBurstPrefab, worldPosition + offset, Quaternion.identity);
        Object.Destroy(instance, 2.5f);
    }
}
