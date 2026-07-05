using UnityEngine;

/// <summary>
/// 안개 소용돌이 구역 진입 시 ETFX Fog로 시야를 가림 (반경 축소 대신 파티클).
/// </summary>
public class CwslLocalFogZoneEffect : MonoBehaviour
{
    private GameObject localFog;
    private bool inFogZone;

    private void Update()
    {
        if (CwslGameConstants.UseDefenseMode)
        {
            if (localFog != null)
            {
                Destroy(localFog);
                localFog = null;
            }

            inFogZone = false;
            return;
        }

        var inZone = CwslArenaGimmickSystem.IsFogVortexAt(transform.position);
        if (inZone == inFogZone)
            return;

        inFogZone = inZone;
        if (inZone)
        {
            if (localFog == null)
                localFog = CwslVfxSpawner.AttachLocalFogOverlay(transform);
        }
        else if (localFog != null)
        {
            Destroy(localFog);
            localFog = null;
        }
    }

    private void OnDestroy()
    {
        if (localFog != null)
            Destroy(localFog);
    }
}
