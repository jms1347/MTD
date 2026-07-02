#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 디펜스 게임에 필요한 매니저·HUD 프리팹을 Unity API로 생성합니다.
/// .meta GUID는 Unity가 자동 발급하므로 수동 YAML/meta 작성이 필요 없습니다.
/// </summary>
public static class DefensePrefabFactory
{
    public const string PrefabDir = "Assets/Game/2Game/Prefab/Defense";
    public const string StageManagerPath = "Assets/0UkDefense/3Stage/Prefab/StageManager.prefab";
    public const string GameManagerPath = "Assets/0UkDefense/5Manager/Prefab/GameManager.prefab";

    public const string MissilePoolManagerPath = PrefabDir + "/MissilePoolManager.prefab";
    public const string NexusManagerPath = PrefabDir + "/NexusManager.prefab";
    public const string TowerStatsManagerPath = PrefabDir + "/TowerStatsManager.prefab";
    public const string TowerManagerPath = PrefabDir + "/TowerManager.prefab";
    public const string DefenseHudPath = PrefabDir + "/DefenseHUD.prefab";

    private static readonly Regex GuidRegex = new(@"^guid:\s*([0-9a-fA-F]+)\s*$", RegexOptions.Multiline);

    [MenuItem("Tools/UkDefense/1. Create All Defense Prefabs", false, 0)]
    public static void CreateAllPrefabsMenu()
    {
        CreateAllPrefabs(false);
    }

    [MenuItem("Tools/UkDefense/2. Rebuild All Defense Prefabs (Force)", false, 1)]
    public static void RebuildAllPrefabsMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "프리팹 재생성",
                "Defense 폴더의 매니저·HUD 프리팹을 삭제 후 다시 만듭니다.\n씬 참조는 'Setup TestScene' 실행 시 경로 기준으로 다시 연결됩니다.",
                "재생성", "취소"))
            return;

        CreateAllPrefabs(true);
    }

    /// <summary>
    /// 4개 매니저 + HUD 프리팹을 생성하고 결과를 반환합니다.
    /// </summary>
    public static DefensePrefabSet CreateAllPrefabs(bool forceRebuild)
    {
        EnsureDirectory();

        if (forceRebuild)
            DeleteAllDefensePrefabs();

        var set = new DefensePrefabSet
        {
            MissilePoolManager = CreateOrLoadManagerPrefab<MissilePoolManager>(MissilePoolManagerPath, "MissilePoolManager", forceRebuild),
            NexusManager = CreateOrLoadManagerPrefab<NexusManager>(NexusManagerPath, "NexusManager", forceRebuild),
            TowerStatsManager = CreateOrLoadManagerPrefab<TowerStatsManager>(TowerStatsManagerPath, "TowerStatsManager", forceRebuild),
            TowerManager = CreateOrLoadManagerPrefab<TowerManager>(TowerManagerPath, "TowerManager", forceRebuild),
            StageManager = LoadPrefab(StageManagerPath),
            DefenseHud = CreateOrLoadHudPrefab(forceRebuild)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DefensePrefabFactory] 프리팹 생성 완료:\n" +
                  $"- {MissilePoolManagerPath}\n" +
                  $"- {NexusManagerPath}\n" +
                  $"- {TowerStatsManagerPath}\n" +
                  $"- {TowerManagerPath}\n" +
                  $"- {StageManagerPath}\n" +
                  $"- {DefenseHudPath}");

        return set;
    }

    public static GameObject LoadPrefab(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    private static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Game/2Game/Prefab"))
            AssetDatabase.CreateFolder("Assets/Game/2Game", "Prefab");

        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Game/2Game/Prefab", "Defense");
    }

    private static void DeleteAllDefensePrefabs()
    {
        DeleteAssetIfExists(MissilePoolManagerPath);
        DeleteAssetIfExists(NexusManagerPath);
        DeleteAssetIfExists(TowerStatsManagerPath);
        DeleteAssetIfExists(TowerManagerPath);
        DeleteAssetIfExists(DefenseHudPath);
        AssetDatabase.Refresh();
    }

    private static GameObject CreateOrLoadManagerPrefab<T>(string path, string objectName, bool forceRebuild)
        where T : Component
    {
        if (!forceRebuild)
        {
            var existing = LoadPrefab(path);
            if (existing != null && HasComponent<T>(existing))
                return existing;
        }

        ClearInvalidPrefabSlot(path);

        var temp = new GameObject(objectName);
        temp.AddComponent<T>();
        var prefab = SavePrefabAsset(temp, path);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject CreateOrLoadHudPrefab(bool forceRebuild)
    {
        if (!forceRebuild)
        {
            var existing = LoadPrefab(DefenseHudPath);
            if (existing != null && existing.GetComponent<DefenseHUDSetup>() != null)
                return existing;
        }

        ClearInvalidPrefabSlot(DefenseHudPath);

        var temp = BuildHudHierarchy();
        var prefab = SavePrefabAsset(temp, DefenseHudPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject BuildHudHierarchy()
    {
        var root = new GameObject("DefenseHUD");
        var hudSetup = root.AddComponent<DefenseHUDSetup>();

        var canvasGo = new GameObject("DefenseHUDCanvas");
        canvasGo.transform.SetParent(root.transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var spawnWarning = canvasGo.AddComponent<SpawnDirectionWarningUI>();
        var minimapUI = canvasGo.AddComponent<DefenseMinimapUI>();
        var goldUI = canvasGo.AddComponent<DefenseGoldUI>();
        var stageTimerUI = canvasGo.AddComponent<DefenseStageTimerUI>();

        var goldPanel = CreateRectObject("GoldPanel", canvasGo.transform);
        var goldRect = goldPanel.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(1f, 1f);
        goldRect.anchorMax = new Vector2(1f, 1f);
        goldRect.pivot = new Vector2(1f, 1f);
        goldRect.sizeDelta = new Vector2(280f, 52f);
        goldRect.anchoredPosition = new Vector2(-16f, -16f);
        var goldPanelImage = goldPanel.AddComponent<Image>();
        goldPanelImage.color = new Color(0.08f, 0.1f, 0.08f, 0.82f);
        goldPanelImage.raycastTarget = false;

        var goldIconGo = CreateRectObject("GoldIcon", goldPanel.transform);
        var goldIconRect = goldIconGo.GetComponent<RectTransform>();
        goldIconRect.anchorMin = new Vector2(0f, 0.5f);
        goldIconRect.anchorMax = new Vector2(0f, 0.5f);
        goldIconRect.pivot = new Vector2(0f, 0.5f);
        goldIconRect.sizeDelta = new Vector2(32f, 32f);
        goldIconRect.anchoredPosition = new Vector2(12f, 0f);
        var goldIconImage = goldIconGo.AddComponent<Image>();
        goldIconImage.raycastTarget = true;
        DefenseGoldCoinVisual.ApplyHudIcon(goldIconImage);
        if (goldIconImage.sprite == null)
        {
            goldIconImage.sprite = DefenseUISprites.White;
            goldIconImage.color = new Color(1f, 0.84f, 0.2f);
        }

        var goldIconButton = goldIconGo.AddComponent<Button>();
        goldIconButton.targetGraphic = goldIconImage;
        goldIconButton.transition = Selectable.Transition.None;

        var goldLabelGo = CreateRectObject("GoldLabel", goldPanel.transform);
        var goldLabelRect = goldLabelGo.GetComponent<RectTransform>();
        goldLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        goldLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        goldLabelRect.pivot = new Vector2(0.5f, 0.5f);
        goldLabelRect.sizeDelta = new Vector2(210f, 40f);
        var goldLabel = goldLabelGo.AddComponent<Text>();
        goldLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        goldLabel.fontSize = 28;
        goldLabel.fontStyle = FontStyle.Bold;
        goldLabel.alignment = TextAnchor.MiddleRight;
        goldLabel.color = new Color(1f, 0.92f, 0.55f);
        goldLabel.raycastTarget = false;
        goldLabel.text = "0";

        var goldSo = new SerializedObject(goldUI);
        goldSo.FindProperty("goldText").objectReferenceValue = goldLabel;
        goldSo.FindProperty("panelImage").objectReferenceValue = goldPanelImage;
        goldSo.ApplyModifiedPropertiesWithoutUndo();

        var stageRoot = CreateRectObject("StageTimerRoot", canvasGo.transform);
        var stageRootRect = stageRoot.GetComponent<RectTransform>();
        stageRootRect.anchorMin = new Vector2(0.5f, 1f);
        stageRootRect.anchorMax = new Vector2(0.5f, 1f);
        stageRootRect.pivot = new Vector2(0.5f, 1f);
        stageRootRect.sizeDelta = new Vector2(520f, 180f);
        stageRootRect.anchoredPosition = new Vector2(0f, -12f);

        var stageTimerGo = CreateRectObject("StageTimerText", stageRoot.transform);
        var stageTimerRect = stageTimerGo.GetComponent<RectTransform>();
        stageTimerRect.anchorMin = new Vector2(0.5f, 1f);
        stageTimerRect.anchorMax = new Vector2(0.5f, 1f);
        stageTimerRect.pivot = new Vector2(0.5f, 1f);
        stageTimerRect.sizeDelta = new Vector2(220f, 72f);
        stageTimerRect.anchoredPosition = Vector2.zero;
        var stageTimerText = stageTimerGo.AddComponent<Text>();
        stageTimerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stageTimerText.fontSize = 54;
        stageTimerText.fontStyle = FontStyle.Bold;
        stageTimerText.alignment = TextAnchor.MiddleCenter;
        stageTimerText.color = Color.white;
        stageTimerText.raycastTarget = false;
        stageTimerText.text = "60";

        var stageMessageGo = CreateRectObject("StageMessageText", stageRoot.transform);
        var stageMessageRect = stageMessageGo.GetComponent<RectTransform>();
        stageMessageRect.anchorMin = new Vector2(0.5f, 1f);
        stageMessageRect.anchorMax = new Vector2(0.5f, 1f);
        stageMessageRect.pivot = new Vector2(0.5f, 1f);
        stageMessageRect.sizeDelta = new Vector2(500f, 90f);
        stageMessageRect.anchoredPosition = new Vector2(0f, -78f);
        var stageMessageText = stageMessageGo.AddComponent<Text>();
        stageMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stageMessageText.fontSize = 30;
        stageMessageText.fontStyle = FontStyle.Bold;
        stageMessageText.alignment = TextAnchor.MiddleCenter;
        stageMessageText.color = new Color(1f, 0.92f, 0.55f);
        stageMessageText.raycastTarget = false;
        stageMessageText.text = "1분 뒤 몬스터가 등장합니다";

        var stageTimerSo = new SerializedObject(stageTimerUI);
        stageTimerSo.FindProperty("timerText").objectReferenceValue = stageTimerText;
        stageTimerSo.FindProperty("messageText").objectReferenceValue = stageMessageText;
        stageTimerSo.ApplyModifiedPropertiesWithoutUndo();

        var panel = CreateRectObject("MinimapPanel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(200f, 200f);
        panelRect.anchoredPosition = new Vector2(-16f, -80f);
        panel.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.75f);

        var iconArea = CreateRectObject("IconArea", panel.transform);
        var iconRect = iconArea.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);
        iconArea.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.18f, 0.9f);

        var minimapSo = new SerializedObject(minimapUI);
        minimapSo.FindProperty("iconContainer").objectReferenceValue = iconRect;
        minimapSo.ApplyModifiedPropertiesWithoutUndo();

        var hudSo = new SerializedObject(hudSetup);
        hudSo.FindProperty("minimap").objectReferenceValue = minimapUI;
        hudSo.FindProperty("spawnWarning").objectReferenceValue = spawnWarning;
        hudSo.FindProperty("goldUi").objectReferenceValue = goldUI;
        hudSo.FindProperty("stageTimerUi").objectReferenceValue = stageTimerUI;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    /// <summary>
    /// 잘못된 .meta GUID가 있으면 프리팹 슬롯을 비워 Unity가 새 GUID를 발급하게 합니다.
    /// </summary>
    private static void ClearInvalidPrefabSlot(string assetPath)
    {
        var metaPath = assetPath + ".meta";

        if (File.Exists(assetPath) || File.Exists(metaPath))
        {
            if (!IsMetaGuidValid(metaPath))
            {
                Debug.LogWarning($"[DefensePrefabFactory] 잘못된 .meta 삭제 후 재생성: {assetPath}");
                DeleteAssetIfExists(assetPath);

                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
        }
    }

    private static bool IsMetaGuidValid(string metaPath)
    {
        if (!File.Exists(metaPath))
            return false;

        var text = File.ReadAllText(metaPath);
        var match = GuidRegex.Match(text);
        if (!match.Success)
            return false;

        var guid = match.Groups[1].Value;
        return guid.Length == 32 && Regex.IsMatch(guid, @"^[0-9a-fA-F]{32}$");
    }

    private static GameObject SavePrefabAsset(GameObject tempRoot, string path)
    {
        EnsureDirectory();

        var prefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, path);
        if (prefab == null)
            throw new InvalidOperationException($"프리팹 저장 실패: {path}");

        return prefab;
    }

    private static void DeleteAssetIfExists(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
        else if (File.Exists(assetPath))
            File.Delete(assetPath);
    }

    private static bool HasComponent<T>(GameObject prefab) where T : Component
    {
        return prefab.GetComponent<T>() != null;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public struct DefensePrefabSet
    {
        public GameObject MissilePoolManager;
        public GameObject NexusManager;
        public GameObject TowerStatsManager;
        public GameObject TowerManager;
        public GameObject StageManager;
        public GameObject DefenseHud;
    }
}
#endif
