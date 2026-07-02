using UnityEngine;

public class CoopCameraFollow : MonoBehaviour
{
    [SerializeField] private float orthographicSize = 38f;

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
    }

    private void EnsureCamera()
    {
        if (isoCamera != null)
            return;

        var camera = CoopMapBootstrap.EnsureMainCamera();
        isoCamera = camera.GetComponent<DefenseIsometricCamera>();
        if (isoCamera == null)
            isoCamera = camera.gameObject.AddComponent<DefenseIsometricCamera>();

        if (camera.GetComponent<DefenseCameraControlManager>() == null)
            camera.gameObject.AddComponent<DefenseCameraControlManager>();

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
