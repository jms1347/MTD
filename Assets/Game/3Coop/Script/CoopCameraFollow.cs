using UnityEngine;

public class CoopCameraFollow : MonoBehaviour
{
    [SerializeField] private float orthographicSize = 14f;
    [SerializeField] private float cameraDistance = 14f;

    private DefenseIsometricCamera isoCamera;

    private void LateUpdate()
    {
        var session = CoopGameSession.Instance;
        if (session == null)
            return;

        EnsureCamera();
        var target = FindLocalTower();
        if (target == null)
            return;

        isoCamera.SetFollowTarget(target, orthographicSize);
        isoCamera.SetCameraDistance(cameraDistance);
    }

    private void EnsureCamera()
    {
        if (isoCamera != null)
            return;

        var camera = CoopMapBootstrap.EnsureMainCamera();
        isoCamera = camera.GetComponent<DefenseIsometricCamera>();
        if (isoCamera == null)
            isoCamera = camera.gameObject.AddComponent<DefenseIsometricCamera>();

        var legacyControl = camera.GetComponent<DefenseCameraControlManager>();
        if (legacyControl != null)
            Destroy(legacyControl);

        if (camera.GetComponent<CoopCameraControlManager>() == null)
            camera.gameObject.AddComponent<CoopCameraControlManager>();

        camera.enabled = true;
    }

    private static Transform FindLocalTower()
    {
        var session = CoopGameSession.Instance;
        if (session == null || string.IsNullOrEmpty(session.LocalPlayerId))
            return null;

        foreach (var unit in FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (unit != null && unit.PlayerId == session.LocalPlayerId)
                return unit.transform;
        }

        return null;
    }
}
