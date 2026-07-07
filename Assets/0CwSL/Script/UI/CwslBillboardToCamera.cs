using UnityEngine;

public class CwslBillboardToCamera : MonoBehaviour
{
    public static Camera ResolveCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        var playerCamera = Object.FindFirstObjectByType<CwslPlayerCamera>();
        if (playerCamera != null)
        {
            var camera = playerCamera.GetComponent<Camera>();
            if (camera != null)
                return camera;
        }

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (var i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
                return cameras[i];
        }

        return null;
    }

    private void LateUpdate()
    {
        var camera = ResolveCamera();
        if (camera == null)
            return;

        transform.rotation = camera.transform.rotation;
    }
}
