using UnityEngine;

/// <summary>홍명보 보스 스킬 클라이언트 연출.</summary>
public static class CwslBossSkillVfx
{
    public static void ShowWarningZone(Vector3 position, float radius, float duration)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "BossWarningMarker";
        marker.transform.position = position + Vector3.up * 0.02f;
        marker.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
        Object.Destroy(marker.GetComponent<Collider>());
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.95f, 0.1f, 0.08f, 0.55f);
            renderer.material.SetFloat("_Surface", 1f);
        }

        Object.Destroy(marker, duration + 0.1f);
    }

    public static void ShowExplosion(Vector3 position, float radius)
    {
        CwslSimpleVfx.SpawnBurst(position, new Color(0.95f, 0.15f, 0.08f), radius * 0.6f, 0.4f);
        CwslArenaAudioFeedback.PlayBossTeleportArrive(position);
    }

    public static void ShowSilenceEye(Transform playerTransform, float duration)
    {
        if (playerTransform == null)
            return;

        var eyeRoot = new GameObject("SilenceEye");
        eyeRoot.transform.SetParent(playerTransform, false);
        eyeRoot.transform.localPosition = Vector3.up * 2f;

        var left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        left.transform.SetParent(eyeRoot.transform, false);
        left.transform.localPosition = new Vector3(-0.18f, 0f, 0f);
        left.transform.localScale = Vector3.one * 0.22f;
        Object.Destroy(left.GetComponent<Collider>());

        var right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        right.transform.SetParent(eyeRoot.transform, false);
        right.transform.localPosition = new Vector3(0.18f, 0f, 0f);
        right.transform.localScale = Vector3.one * 0.22f;
        Object.Destroy(right.GetComponent<Collider>());

        var red = new Color(0.95f, 0.08f, 0.08f);
        foreach (var renderer in eyeRoot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = red;
        }

        Object.Destroy(eyeRoot, duration);
    }

    public static void AttachSafeZoneVisual(Transform parent, float radius)
    {
        if (parent == null)
            return;

        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = "SafeZoneVisual";
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        Object.Destroy(cylinder.GetComponent<Collider>());
        var renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.1f, 0.85f, 0.2f, 0.45f);
            renderer.material.SetFloat("_Surface", 1f);
        }
    }
}
