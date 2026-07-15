using UnityEngine;

/// <summary>
/// LobbyScene 진입 시 네트워크 매니저와 UI를 보장합니다.
/// </summary>
public class LobbySceneBootstrap : MonoBehaviour
{
    [SerializeField] private bool useStllEaScene;

    private void Awake()
    {
        if (LobbyNetworkManager.Instance == null)
        {
            var managerObject = new GameObject("LobbyNetworkManager");
            managerObject.AddComponent<LobbyNetworkManager>();
        }

        LobbyNetworkManager.Instance.GameSceneName = useStllEaScene
            ? StllGameConstants.GameSceneName
            : CwslGameConstants.GameSceneName;

        if (FindFirstObjectByType<LobbyUIController>() == null)
        {
            var uiObject = new GameObject("LobbyUI");
            uiObject.AddComponent<LobbyUIController>();
        }
    }
}
