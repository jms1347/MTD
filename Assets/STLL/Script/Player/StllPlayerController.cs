using Unity.Netcode;
using UnityEngine;

public class StllPlayerController : NetworkBehaviour
{
    private StllPlayerCamera followCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        EnsureFollowCamera();
    }

    public override void OnNetworkDespawn()
    {
        if (followCamera == null)
            return;

        var cameraObject = followCamera.gameObject;
        if (cameraObject.name == "StllPlayerCamera")
            Destroy(cameraObject);
    }

    private void EnsureFollowCamera()
    {
        if (Camera.main != null && Camera.main.GetComponent<StllPlayerCamera>() != null)
        {
            followCamera = Camera.main.GetComponent<StllPlayerCamera>();
            return;
        }

        var cameraObject = new GameObject("StllPlayerCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();

        followCamera = cameraObject.AddComponent<StllPlayerCamera>();
        followCamera.Initialize(transform, camera);
    }
}
