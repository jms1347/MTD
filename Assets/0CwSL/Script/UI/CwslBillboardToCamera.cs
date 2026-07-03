using UnityEngine;

public class CwslBillboardToCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        transform.rotation = camera.transform.rotation;
    }
}
