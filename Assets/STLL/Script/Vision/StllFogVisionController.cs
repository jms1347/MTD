using UnityEngine;

/// <summary>안개 v1 — 시야 밖 어둡게, 지휘 하이라이트.</summary>
public class StllFogVisionController : MonoBehaviour
{
    private StllBrotherhoodRoleState localRole;
    private Camera mainCamera;

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (localRole == null && Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
            localRole = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StllBrotherhoodRoleState>();
    }

    private void OnGUI()
    {
        if (mainCamera == null || localRole == null)
            return;

        var run = StllRunController.Instance;
        if (run == null || run.Phase != StllRunPhase.StageSashuguan && run.Phase != StllRunPhase.StageHulao)
            return;

        DrawFogOverlay();
        DrawHighlights();
    }

    private void DrawFogOverlay()
    {
        var dim = new Color(0f, 0f, 0f, 0.55f);
        GUI.color = dim;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawHighlights()
    {
        var playerPos = localRole.transform.position;
        var mouseWorld = GetMouseWorld();
        var highlightRadius = StllEaConstants.CommandHighlightRadius;
        if (localRole.Role == StllBrotherhoodRole.LiuBei)
            highlightRadius += StllEaConstants.LiuBeiHighlightBonus;

        HighlightObjects<StllBrotherhoodRoleState>(playerPos, mouseWorld, highlightRadius, new Color(0.2f, 0.6f, 1f, 0.5f));
        HighlightObjects<StllSupplyDepot>(playerPos, mouseWorld, highlightRadius, new Color(1f, 0.8f, 0.2f, 0.5f));
    }

    private void HighlightObjects<T>(Vector3 playerPos, Vector3 mouseWorld, float radius, Color color) where T : Component
    {
        var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        for (var i = 0; i < objects.Length; i++)
        {
            var obj = objects[i];
            if (obj == null)
                continue;

            var pos = obj.transform.position;
            var inVision = Vector3.Distance(playerPos, pos) <= StllEaConstants.PlayerVisionRadius;
            var inHighlight = Vector3.Distance(mouseWorld, pos) <= radius;
            if (!inVision && !inHighlight)
                continue;

            var screen = mainCamera.WorldToScreenPoint(pos + Vector3.up * 2f);
            if (screen.z < 0f)
                continue;

            var rect = new Rect(screen.x - 12f, Screen.height - screen.y - 12f, 24f, 24f);
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    private Vector3 GetMouseWorld()
    {
        if (mainCamera == null)
            return localRole.transform.position;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, localRole.transform.position);
        return plane.Raycast(ray, out var dist) ? ray.GetPoint(dist) : localRole.transform.position;
    }
}
