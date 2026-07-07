using UnityEngine;

/// <summary>탄환 교체 짧은 피드백 (스태프/총 색상 플래시).</summary>
public class CwslMissileTankAmmoHudVisual : MonoBehaviour
{
    private Coroutine flashRoutine;

    public void ShowAmmoSwitch(CwslMissileTankAmmoKind kind)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(kind));
    }

    private System.Collections.IEnumerator FlashRoutine(CwslMissileTankAmmoKind kind)
    {
        var guns = transform;
        if (guns == null)
            yield break;

        var color = kind switch
        {
            CwslMissileTankAmmoKind.Fire => new Color(1f, 0.45f, 0.12f),
            CwslMissileTankAmmoKind.Poison => new Color(0.35f, 0.95f, 0.25f),
            CwslMissileTankAmmoKind.Lightning => new Color(1f, 0.92f, 0.2f),
            _ => new Color(0.55f, 0.78f, 1f),
        };

        CwslThreatLight.Ensure(guns, color, 4f, 2.4f, Vector3.up * 0.4f);
        yield return new WaitForSeconds(0.35f);
        flashRoutine = null;
    }
}
