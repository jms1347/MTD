using UnityEngine;

/// <summary>
/// 상태 이상은 VFX로만 표시합니다. 텍스트 오버레이는 사용하지 않습니다.
/// </summary>
[RequireComponent(typeof(MonsterStatusController))]
public class MonsterStatusOverlayUI : MonoBehaviour
{
    private void Awake()
    {
        DestroyLegacyOverlay();
    }

    public void RefreshForSpawn()
    {
        DestroyLegacyOverlay();
    }

    public void RefreshDisplay()
    {
        DestroyLegacyOverlay();
    }

    public void ClearAndDestroy()
    {
        DestroyLegacyOverlay();
    }

    private void DestroyLegacyOverlay()
    {
        var legacy = transform.Find("StatusOverlay");
        if (legacy != null)
            Destroy(legacy.gameObject);
    }
}
