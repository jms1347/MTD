using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 빈 씬에서도 Play 시 자동으로 삼국마블 시스템을 스폰.
    /// (에디터 메뉴로 씬을 만든 뒤에는 불필요하지만 안전망으로 유지)
    /// </summary>
    public class SamgukMarbleBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootIfNeeded()
        {
            if (Object.FindFirstObjectByType<GameManager3D>() != null)
                return;

            // MainGame3DScene 또는 SamgukMarble 태그가 있을 때만 자동 부트
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "MainGame3DScene" && scene.name != "SamgukMarble")
                return;

            var root = new GameObject("SamgukMarbleGame");
            root.AddComponent<BoardBuilder3D>();
            root.AddComponent<BuildingManager>();
            root.AddComponent<TreasuryManager>();
            root.AddComponent<CardUI>();
            var gm = root.AddComponent<GameManager3D>();
            gm.PlayerCount = 2;
        }
    }
}
