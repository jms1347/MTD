using UnityEngine;

[DefaultExecutionOrder(-200)]
public class CoopSceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        CoopSoloPlayBootstrap.Ensure();
        CoopMapBootstrap.EnsureMainCamera();
        CoopSlimeAssetCache.TryGetPrefab("SLIME-01", out _);

        if (FindFirstObjectByType<CoopMapBootstrap>() == null)
        {
            var mapObject = new GameObject("CoopMapBootstrap");
            mapObject.AddComponent<CoopMapBootstrap>();
        }

        if (FindFirstObjectByType<CoopGameSession>() == null)
        {
            var sessionObject = new GameObject("CoopGameSession");
            sessionObject.AddComponent<CoopGameSession>();
        }

        if (FindFirstObjectByType<CoopWorldView>() == null)
        {
            var worldObject = new GameObject("CoopWorldView");
            worldObject.AddComponent<CoopWorldView>();
        }

        if (FindFirstObjectByType<CoopGameUI>() == null)
        {
            var uiObject = new GameObject("CoopGameUI");
            uiObject.AddComponent<CoopGameUI>();
        }

        if (FindFirstObjectByType<CoopMinimapUI>() == null)
        {
            var minimapObject = new GameObject("CoopMinimapUI");
            minimapObject.AddComponent<CoopMinimapUI>();
        }

        if (FindFirstObjectByType<CoopCombatVfxBootstrap>() == null)
        {
            var vfxObject = new GameObject("CoopCombatVfxBootstrap");
            vfxObject.AddComponent<CoopCombatVfxBootstrap>();
        }
    }
}
