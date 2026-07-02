using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoguelikeCardSelectUI : MonoBehaviour
{
    private RectTransform panelRoot;
    private RectTransform cardRow;
    private TextMeshProUGUI titleText;
    private readonly List<RoguelikeCardSlotView> slots = new();
    private Action<RoguelikeCardData> onPicked;

    public bool IsReady => panelRoot != null && cardRow != null;

    public void Initialize(Transform hudRoot)
    {
        var canvas = DefenseHudCanvasUtility.ResolveCanvas(hudRoot);
        if (canvas == null)
        {
            Debug.LogError("[RoguelikeCardSelectUI] Canvas를 찾을 수 없습니다. DefenseHUD 프리팹 구조를 확인하세요.");
            return;
        }

        if (panelRoot != null && panelRoot.parent != canvas.transform)
        {
            Destroy(panelRoot.gameObject);
            panelRoot = null;
            cardRow = null;
            titleText = null;
        }

        if (panelRoot != null)
            return;

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var overlay = new GameObject("RoguelikeCardSelectUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        panelRoot = overlay.GetComponent<RectTransform>();
        panelRoot.anchorMin = Vector2.zero;
        panelRoot.anchorMax = Vector2.one;
        panelRoot.offsetMin = Vector2.zero;
        panelRoot.offsetMax = Vector2.zero;

        var dim = overlay.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;

        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panelRoot, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -48f);
        titleRect.sizeDelta = new Vector2(900f, 48f);
        titleText = titleObject.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 30f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "카드 선택";
        titleText.color = Color.white;

        var rowObject = new GameObject("CardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(panelRoot, false);
        cardRow = rowObject.GetComponent<RectTransform>();
        cardRow.anchorMin = new Vector2(0.5f, 0.5f);
        cardRow.anchorMax = new Vector2(0.5f, 0.5f);
        cardRow.pivot = new Vector2(0.5f, 0.5f);
        cardRow.anchoredPosition = Vector2.zero;
        cardRow.sizeDelta = new Vector2(760f, 340f);

        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 28f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        overlay.SetActive(false);
    }

    public bool TryShow(
        RoguelikeCardManager manager,
        IReadOnlyList<RoguelikeCardData> choices,
        int clearedStage,
        Action<RoguelikeCardData> pickedCallback)
    {
        if (panelRoot == null || choices == null || choices.Count == 0)
            return false;

        onPicked = pickedCallback;
        ClearSlots();

        if (titleText != null)
            titleText.text = $"{clearedStage}스테이지 클리어 — 카드 1장을 선택하세요";

        var catalog = manager != null ? manager.VisualCatalog : null;
        for (int i = 0; i < choices.Count; i++)
        {
            var card = choices[i];
            if (card == null)
                continue;

            var slot = RoguelikeCardSlotView.Create(cardRow, catalog, card, () => HandlePick(card));
            slots.Add(slot);
        }

        if (slots.Count == 0)
            return false;

        panelRoot.SetAsLastSibling();
        panelRoot.gameObject.SetActive(true);
        return true;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);

        ClearSlots();
        onPicked = null;
    }

    private void HandlePick(RoguelikeCardData card)
    {
        onPicked?.Invoke(card);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                Destroy(slots[i].gameObject);
        }

        slots.Clear();
    }
}
