using UnityEngine;

/// <summary>
/// 드릴 중 플레이어 비주얼(몸·머리)을 흔듭니다.
/// </summary>
public class PlayerDrillShake : MonoBehaviour
{
    [SerializeField] private float horizontalAmount = 0.055f;
    [SerializeField] private float verticalAmount = 0.04f;
    [SerializeField] private float shakeSpeed = 28f;

    private Transform body;
    private Transform head;
    private Vector3 bodyBaseLocal;
    private Vector3 headBaseLocal;
    private bool isShaking;
    private float phase;

    private void Awake()
    {
        body = transform.Find("Body");
        head = transform.Find("Head");

        if (body != null)
            bodyBaseLocal = body.localPosition;

        if (head != null)
            headBaseLocal = head.localPosition;
    }

    public void SetShaking(bool active)
    {
        isShaking = active;
        if (!active)
            ResetPose();
    }

    private void LateUpdate()
    {
        if (!isShaking)
            return;

        phase += Time.deltaTime * shakeSpeed;
        Vector3 offset = new Vector3(
            Mathf.Sin(phase) * horizontalAmount,
            Mathf.Abs(Mathf.Sin(phase * 2.1f)) * verticalAmount,
            Mathf.Cos(phase * 1.25f) * horizontalAmount);

        if (body != null)
            body.localPosition = bodyBaseLocal + offset;

        if (head != null)
            head.localPosition = headBaseLocal + offset * 1.15f;
    }

    private void ResetPose()
    {
        phase = 0f;

        if (body != null)
            body.localPosition = bodyBaseLocal;

        if (head != null)
            head.localPosition = headBaseLocal;
    }
}
