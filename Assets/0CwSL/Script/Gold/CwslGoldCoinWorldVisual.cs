using UnityEngine;

/// <summary>
/// 필드에 놓인 3D 골드 코인 메시 — 대기 회전 / 자석 시 빠른 회전.
/// </summary>
public class CwslGoldCoinWorldVisual : MonoBehaviour
{
    private const float IdleSpinSpeed = 140f;
    private const float MagnetSpinSpeed = 620f;
    private const float BobHeight = 0.06f;
    private const float BobSpeed = 4.5f;

    private CwslGoldPickup pickup;
    private float baseLocalY;
    private float phaseOffset;

    private void Awake()
    {
        pickup = GetComponentInParent<CwslGoldPickup>();
        baseLocalY = transform.localPosition.y;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        EnsureMaterial();
    }

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void EnsureMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
            return;

        CwslMaterialUtil.ApplyColor(renderer, new Color(1f, 0.84f, 0.12f));
    }

    private void Update()
    {
        if (pickup == null)
            return;

        var spin = pickup.IsMagnetized ? MagnetSpinSpeed : IdleSpinSpeed;
        transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
        transform.Rotate(transform.right, spin * 0.28f * Time.deltaTime, Space.Self);

        if (!pickup.IsMagnetized)
        {
            var bob = Mathf.Sin(Time.time * BobSpeed + phaseOffset) * BobHeight;
            var local = transform.localPosition;
            local.y = baseLocalY + bob;
            transform.localPosition = local;
        }
    }
}
