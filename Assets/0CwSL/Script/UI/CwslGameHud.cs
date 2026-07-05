using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CwslGameHud : MonoBehaviour
{
    public static void Ensure(CwslKarmaSystem karmaSystem)
    {
        if (FindFirstObjectByType<CwslGameHud>() != null)
            return;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        var canvasObject = new GameObject("CwslGameHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var karmaLabel = CreateLabel(canvasObject.transform, "KarmaLabel", new Vector2(16f, -96f), new Vector2(640f, 48f), 28f);

        var karmaUi = canvasObject.AddComponent<CwslKarmaUI>();
        karmaUi.Bind(karmaSystem, CwslTeamGoldCollectedSystem.Instance, karmaLabel);
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }
}
