using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>게임 시작 전 넥서스 주변 준비 구역을 막는 임시 벽.</summary>
public static class CwslDefensePrepBarrier
{
    private static GameObject root;

    public static void SetActive(bool active)
    {
        if (active)
            Show();
        else
            Hide();
    }

    public static void Show()
    {
        if (root != null)
            return;

        root = new GameObject("CwslDefensePrepBarrier");
        Object.DontDestroyOnLoad(root);

        var radius = CwslGameConstants.DefensePrepBarrierRadius;
        var height = CwslGameConstants.DefensePrepBarrierHeight;
        var thickness = CwslGameConstants.DefensePrepBarrierThickness;
        const int segments = 32;
        var arcLength = 2f * Mathf.PI * radius / segments * 1.22f;
        var addNavObstacle = NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
        var wallColor = new Color(0.38f, 0.4f, 0.46f, 0.82f);

        for (var i = 0; i < segments; i++)
        {
            var angle = i / (float)segments * Mathf.PI * 2f;
            var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"PrepWall_{i:00}";
            wall.transform.SetParent(root.transform, false);
            wall.transform.position = outward * radius + Vector3.up * (height * 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
            wall.transform.localScale = new Vector3(arcLength, height, thickness);

            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                CwslGroundRingVisual.ApplyTransparent(renderer, wallColor);

            ConfigureWallCollider(wall, addNavObstacle);
        }
    }

    private static void ConfigureWallCollider(GameObject wall, bool addNavObstacle)
    {
        var collider = wall.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = false;
            collider.enabled = true;
        }

        if (!addNavObstacle)
            return;

        var obstacle = wall.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;
        obstacle.carveOnlyStationary = false;
    }

    public static void Hide()
    {
        if (root == null)
            return;

        Object.Destroy(root);
        root = null;
    }
}
