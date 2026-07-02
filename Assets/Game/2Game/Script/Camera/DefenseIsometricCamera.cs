using UnityEngine;

/// <summary>
/// 넥서스(맵 중앙)를 기준으로 고정된 쿼터뷰(Isometric) 시점을 유지합니다.
/// DefenseCameraControlManager가 팬·줌을 조절합니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DefenseIsometricCamera : MonoBehaviour
{
    [Header("추적 대상")]
    [Tooltip("카메라가 바라볼 중심 (보통 넥서스 Transform)")]
    [SerializeField] private Transform followTarget;

    [Tooltip("타겟 기준 추가 오프셋 (눈높이 보정 등)")]
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("쿼터뷰 각도")]
    [Tooltip("X축 피치: 위에서 비스듬히 내려다보는 각도 (40~55 권장)")]
    [SerializeField] private float pitchAngle = 45f;

    [Tooltip("Y축 요: 45°면 정통 쿼터뷰(대각선). 0°면 동서남북 정렬 탑다운")]
    [SerializeField] private float yawAngle = 45f;

    [Header("거리·줌")]
    [Tooltip("넥서스에서 카메라까지의 거리 (쿼터뷰 오프셋)")]
    [SerializeField] private float cameraHeight = 32f;

    [Tooltip("Orthographic 줌. 맵이 클수록 값을 키우세요")]
    [SerializeField] private float baseOrthographicSize = 26f;

    [Tooltip("마우스 휠 줌 최소·최대")]
    [SerializeField] private float minOrthographicSize = 10f;
    [SerializeField] private float maxOrthographicSize = 50f;

    [Tooltip("해상도 비율에 따라 줌을 자동 보정할지 여부")]
    [SerializeField] private bool adjustForAspectRatio = true;

    [Tooltip("기준 화면 비율 (16:9 = 1.777)")]
    [SerializeField] private float referenceAspect = 16f / 9f;

    [Header("카메라 모드")]
    [SerializeField] private bool useOrthographic = true;

    private Camera cachedCamera;
    private Vector3 panOffset;
    private float zoomOrthographicSize;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float shakeEndTime;
    private float shakeStrength;

    public float CurrentOrthographicSize => cachedCamera != null ? cachedCamera.orthographicSize : baseOrthographicSize;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        cachedCamera.orthographic = useOrthographic;
        cachedCamera.clearFlags = CameraClearFlags.SolidColor;
        cachedCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        zoomOrthographicSize = baseOrthographicSize;
    }

    public void SetFollowTarget(Transform target, float orthographicSize = -1f)
    {
        followTarget = target;

        if (orthographicSize > 0f)
        {
            baseOrthographicSize = orthographicSize;
            zoomOrthographicSize = orthographicSize;
        }

        ApplyCameraTransform();
        ApplyOrthographicSize();
    }

    public void AdjustZoom(float delta)
    {
        zoomOrthographicSize = Mathf.Clamp(zoomOrthographicSize - delta, minOrthographicSize, maxOrthographicSize);
        ApplyOrthographicSize();
    }

    public void AddPanOffset(Vector3 worldDelta)
    {
        panOffset += worldDelta;
        panOffset.y = 0f;
        ApplyCameraTransform();
    }

    public void ResetPan()
    {
        panOffset = Vector3.zero;
        ApplyCameraTransform();
    }

    public void Shake(float strength, float duration)
    {
        if (duration <= 0f)
            return;

        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeEndTime = Mathf.Max(shakeEndTime, Time.time + duration);
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            ApplyOrthographicSize();

        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        Vector3 focusPoint = GetFocusPoint();
        Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        transform.rotation = rotation;
        transform.position = focusPoint - rotation * Vector3.forward * cameraHeight + GetShakeOffset();
    }

    private Vector3 GetShakeOffset()
    {
        if (Time.time >= shakeEndTime)
        {
            shakeStrength = 0f;
            return Vector3.zero;
        }

        float falloff = Mathf.Clamp01((shakeEndTime - Time.time) / 0.35f);
        Vector3 offset = Random.insideUnitSphere * (shakeStrength * falloff);
        offset.y *= 0.35f;
        return offset;
    }

    private Vector3 GetFocusPoint()
    {
        Vector3 center = followTarget != null
            ? followTarget.position + lookAtOffset
            : lookAtOffset;

        return center + panOffset;
    }

    private void ApplyOrthographicSize()
    {
        if (!cachedCamera.orthographic)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float size = zoomOrthographicSize;

        if (adjustForAspectRatio && lastScreenHeight > 0)
        {
            float currentAspect = (float)lastScreenWidth / lastScreenHeight;

            if (currentAspect < referenceAspect)
                size = zoomOrthographicSize * (referenceAspect / currentAspect);
        }

        cachedCamera.orthographicSize = size;
    }
}
