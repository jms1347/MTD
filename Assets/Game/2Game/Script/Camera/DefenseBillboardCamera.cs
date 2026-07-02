using UnityEngine;

/// <summary>
/// 월드 스페이스 UI가 디펜스 쿼터뷰 카메라를 바라보도록 합니다.
/// </summary>
public static class DefenseBillboardCamera
{
    public static Camera Resolve()
    {
        var iso = Object.FindFirstObjectByType<DefenseIsometricCamera>();
        if (iso != null)
        {
            var camera = iso.GetComponent<Camera>();
            if (camera != null)
                return camera;
        }

        return Camera.main;
    }

    /// <summary>Transform.LookAt — 월드 UI가 카메라를 향하도록 회전.</summary>
    public static void Face(Transform target, Camera camera = null)
    {
        if (target == null)
            return;

        camera ??= Resolve();
        if (camera == null)
            return;

        target.LookAt(camera.transform.position, camera.transform.up);
        target.Rotate(0f, 180f, 0f, Space.Self);
    }
}
