using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoguelikeMagicHandUI : MonoBehaviour
{
    private RectTransform panelRoot;
    private readonly List<Button> slotButtons = new();
    private RoguelikeCardManager manager;

    public void Initialize(Transform hudRoot)
    {
        var canvas = DefenseHudCanvasUtility.ResolveCanvas(hudRoot);
        if (canvas == null)
        {
            Debug.LogError("[RoguelikeMagicHandUI] Canvas를 찾을 수 없습니다.");
            return;
        }

        if (panelRoot != null && panelRoot.parent != canvas.transform)
        {
            Destroy(panelRoot.gameObject);
            panelRoot = null;
            slotButtons.Clear();
        }

        if (panelRoot != null)
            return;

        var panel = new GameObject("RoguelikeMagicHandUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        panel.transform.SetParent(canvas.transform, false);
        panelRoot = panel.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0f, 0f);
        panelRoot.anchorMax = new Vector2(0f, 0f);
        panelRoot.pivot = new Vector2(0f, 0f);
        panelRoot.anchoredPosition = new Vector2(18f, 18f);
        panelRoot.sizeDelta = new Vector2(420f, 72f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        var layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        for (int i = 0; i < 5; i++)
            CreateSlotButton(panel.transform, i);
    }

    public void Bind(RoguelikeCardManager cardManager)
    {
        manager = cardManager;
        if (manager != null)
            manager.OnRunStateChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnRunStateChanged -= Refresh;
    }

    public void Refresh()
    {
        if (panelRoot == null)
            return;

        var hand = manager != null ? manager.RunState.MagicHand : null;
        bool hasAny = hand != null && hand.Count > 0;
        panelRoot.gameObject.SetActive(hasAny);

        for (int i = 0; i < slotButtons.Count; i++)
        {
            var button = slotButtons[i];
            if (button == null)
                continue;

            bool active = hand != null && i < hand.Count && hand[i]?.card != null;
            button.gameObject.SetActive(active);
            if (!active)
                continue;

            var owned = hand[i];
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string suffix = owned.card.IsGroundTargetMagic ? "\n(조준)" : string.Empty;
                label.text = owned.card.cardName + suffix;
            }

            var catalog = manager.VisualCatalog;
            var image = button.GetComponent<Image>();
            if (image != null && catalog != null && owned.card != null)
                image.sprite = catalog.Resolve(owned.card.cardColor);

            button.interactable = true;
        }
    }

    private void CreateSlotButton(Transform parent, int index)
    {
        var slot = new GameObject($"MagicSlot_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        slot.transform.SetParent(parent, false);
        var rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(72f, 56f);

        var image = slot.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;

        var button = slot.GetComponent<Button>();
        int captured = index;
        button.onClick.AddListener(() =>
        {
            if (manager != null)
                manager.TryUseMagicCard(captured);
        });

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(slot.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(2f, 2f);
        textRect.offsetMax = new Vector2(-2f, -2f);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 11f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.enableWordWrapping = true;

        slot.SetActive(false);
        slotButtons.Add(button);
    }
}
