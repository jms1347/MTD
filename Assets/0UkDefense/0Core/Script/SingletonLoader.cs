using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// UkDefense 확정 매니저 부트스트랩. SplashScene에 배치합니다.
/// </summary>
public class SingletonLoader : MonoBehaviour
{
    [Header("Data")]
    public GameObject dataManagerPrefab;
    public GameObject googlesheetManagerPrefab;

    [Header("Core")]
    public GameObject gameManagerPrefab;
    [FormerlySerializedAs("enemyManagerPrefab")]
    public GameObject stageManagerPrefab;

    [Header("Scene Transition")]
    public string nextSceneName = "TitleScene";

    [SerializeField] private float dataLoadTimeoutSeconds = 20f;

    private void Awake()
    {
        StartCoroutine(BootstrapRoutine());
    }

    private IEnumerator BootstrapRoutine()
    {
        DataManager.Load(dataManagerPrefab);
        GoogleSheetManager.Load(googlesheetManagerPrefab);

        yield return WaitForGoogleSheetData();

        GameManager.Load(gameManagerPrefab);
        UkDefenseStageLoader.LoadStageManager(stageManagerPrefab);

        LoadNextScene();
    }

    private IEnumerator WaitForGoogleSheetData()
    {
        var elapsed = 0f;

        while (elapsed < dataLoadTimeoutSeconds)
        {
            if (GoogleSheetManager.Instance != null && GoogleSheetManager.Instance.IsLoaded)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[SingletonLoader] Google Sheet 로드 대기 시간 초과. SO 폴백 데이터로 진행합니다.");
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("[SingletonLoader] nextSceneName이 비어 있습니다.");
    }
}
