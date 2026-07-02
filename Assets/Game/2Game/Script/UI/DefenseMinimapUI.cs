using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 캔버스 좌표 매핑 방식의 실시간 미니맵.
/// 프리팹 제작 시 Icon Container에 아이콘이 붙을 RectTransform을 직접 연결하세요.
/// </summary>
public class DefenseMinimapUI : MonoBehaviour
{
    [Header("프리팹 연결")]
    [Tooltip("미니맵 위에 아이콘이 생성될 영역 (프리팹에서 직접 지정)")]
    [SerializeField] private RectTransform iconContainer;

    [Header("맵 범위 (월드 XZ)")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    [SerializeField] private float mapHalfExtent = 28f;

    [Header("아이콘 색상·크기")]
    [SerializeField] private Color nexusColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color towerColor = new Color(0.25f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float nexusIconSize = 14f;
    [SerializeField] private float towerIconSize = 10f;
    [SerializeField] private float enemyIconSize = 6f;

    [Header("갱신 주기")]
    [SerializeField] private float enemyUpdateInterval = 0.05f;

    private Image nexusIcon;
    private readonly List<Image> towerIcons = new();
    private readonly List<Image> enemyIconPool = new();
    private float nextEnemyUpdateTime;
    private bool staticIconsPlaced;

    /// <summary>
    /// DefenseHUDSetup / DefenseSceneSetup에서 맵 범위를 주입합니다.
    /// </summary>
    public void Configure(Vector3 center, float halfExtent)
    {
        mapCenter = center;
        mapHalfExtent = halfExtent;
    }

    private void Start()
    {
        if (iconContainer == null)
        {
            Debug.LogError("[DefenseMinimapUI] Icon Container가 할당되지 않았습니다. 프리팹에서 연결해 주세요.");
            return;
        }

        PlaceStaticIcons();
    }

    private void Update()
    {
        if (iconContainer == null || !staticIconsPlaced)
            return;

        if (Time.time < nextEnemyUpdateTime && enemyUpdateInterval > 0f)
            return;

        nextEnemyUpdateTime = Time.time + enemyUpdateInterval;
        UpdateEnemyIcons();
    }

    private void PlaceStaticIcons()
    {
        if (staticIconsPlaced)
            return;

        if (NexusManager.Instance?.NexusTransform != null)
        {
            nexusIcon = CreateDotIcon("NexusIcon", nexusColor, nexusIconSize);
            SetIconWorldPosition(nexusIcon.rectTransform, NexusManager.Instance.NexusTransform.position);
        }

        var towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (var tower in towers)
        {
            var icon = CreateDotIcon(tower.name + "_Minimap", towerColor, towerIconSize);
            SetIconWorldPosition(icon.rectTransform, tower.transform.position);
            towerIcons.Add(icon);
        }

        staticIconsPlaced = true;
    }

    private void UpdateEnemyIcons()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int activeCount = 0;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            Image icon = GetOrCreateEnemyIcon(activeCount);
            icon.gameObject.SetActive(true);
            SetIconWorldPosition(icon.rectTransform, enemy.transform.position);
            activeCount++;
        }

        for (int i = activeCount; i < enemyIconPool.Count; i++)
            enemyIconPool[i].gameObject.SetActive(false);
    }

    private Image GetOrCreateEnemyIcon(int index)
    {
        while (enemyIconPool.Count <= index)
        {
            var icon = CreateDotIcon($"EnemyIcon_{enemyIconPool.Count}", enemyColor, enemyIconSize);
            enemyIconPool.Add(icon);
        }

        return enemyIconPool[index];
    }

    private void SetIconWorldPosition(RectTransform iconRect, Vector3 worldPosition)
    {
        float normalizedX = (worldPosition.x - mapCenter.x) / mapHalfExtent * 0.5f + 0.5f;
        float normalizedZ = (worldPosition.z - mapCenter.z) / mapHalfExtent * 0.5f + 0.5f;

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        iconRect.anchorMin = new Vector2(normalizedX, normalizedZ);
        iconRect.anchorMax = new Vector2(normalizedX, normalizedZ);
        iconRect.anchoredPosition = Vector2.zero;
    }

    private Image CreateDotIcon(string name, Color color, float size)
    {
        var iconObject = new GameObject(name, typeof(RectTransform));
        iconObject.transform.SetParent(iconContainer, false);

        var rect = iconObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = iconObject.AddComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
