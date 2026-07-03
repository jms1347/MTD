using Unity.Netcode;
using UnityEngine;

public class CwslPlayerController : NetworkBehaviour
{
    private Transform visualRoot;
    private float attackPulseTimer;

    public override void OnNetworkSpawn()
    {
        visualRoot = transform.Find("Visual");

        if (IsOwner)
        {
            EnsureLocalCamera();
            if (GetComponent<CwslPlayerVision>() == null)
                gameObject.AddComponent<CwslPlayerVision>();
        }
    }

    public void PlayAttackPulse()
    {
        attackPulseTimer = 0.22f;
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        TickAttackPulse();
    }

    private void TickAttackPulse()
    {
        if (visualRoot == null || attackPulseTimer <= 0f)
            return;

        attackPulseTimer -= Time.deltaTime;
        var baseScale = GetComponent<CwslPlayerVisualScale>()?.ScaleMultiplier ?? 1f;
        var pulse = 1f + Mathf.Sin((0.22f - attackPulseTimer) / 0.22f * Mathf.PI) * 0.12f;
        visualRoot.localScale = Vector3.one * baseScale * pulse;
        if (attackPulseTimer <= 0f)
            visualRoot.localScale = Vector3.one * baseScale;
    }

    private void EnsureLocalCamera()
    {
        if (Camera.main != null && Camera.main.GetComponent<CwslPlayerCamera>() != null)
            return;

        var cameraObject = new GameObject("CwslPlayerCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();

        var follow = cameraObject.AddComponent<CwslPlayerCamera>();
        follow.Initialize(transform, camera);
    }
}
