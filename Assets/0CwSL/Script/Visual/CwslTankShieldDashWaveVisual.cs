using System.Collections;
using UnityEngine;

/// <summary>탱커 W 돌진 — 방패 앞 SwordWaveBlue 이펙트.</summary>
public class CwslTankShieldDashWaveVisual : MonoBehaviour
{
    private Transform shield;
    private GameObject waveInstance;
    private Coroutine routine;

    public void PlayDashWave(Vector3 direction, bool empowered, float duration)
    {
        shield = transform.Find("Shield");
        if (shield == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(DashWaveRoutine(direction, empowered, duration));
    }

    private IEnumerator DashWaveRoutine(Vector3 direction, bool empowered, float duration)
    {
        ClearWave();

        var root = transform.root;
        var flat = direction;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            root.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        var scale = CwslTankShieldVfxUtil.GetShieldEffectScale(root, empowered);
        waveInstance = CwslVfxSpawner.AttachShieldDashWave(shield, scale);
        if (waveInstance == null)
        {
            routine = null;
            yield break;
        }

        yield return new WaitForSeconds(duration);
        ClearWave();
        routine = null;
    }

    private void ClearWave()
    {
        if (waveInstance == null)
            return;

        Destroy(waveInstance);
        waveInstance = null;
    }

    private void OnDisable()
    {
        ClearWave();
    }
}
