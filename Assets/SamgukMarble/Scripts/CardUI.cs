using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SamgukMarble
{
    /// <summary>
    /// 모두의 마블 스타일 카드 팝업 및 뒤집기 연출.
    /// </summary>
    public class CardUI : MonoBehaviour
    {
        public static CardUI Instance { get; private set; }

        Canvas _canvas;
        RectTransform _cardRoot;
        Image _cardFace;
        Image _cardBack;
        Text _titleText;
        Text _bodyText;
        Text _hintText;
        bool _busy;
        Action _onClosed;

        static readonly Color LuckyColor = new Color(0.2f, 0.7f, 1f);
        static readonly Color UnluckyColor = new Color(0.55f, 0.25f, 0.8f);
        static readonly Color ChanceColor = new Color(1f, 0.55f, 0.15f);

        void Awake()
        {
            Instance = this;
            BuildUI();
            HideImmediate();
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("CardCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var dim = CreateUiImage(canvasGo.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(dim.rectTransform);

            _cardRoot = CreateUiImage(canvasGo.transform, "CardRoot", Color.white).rectTransform;
            _cardRoot.sizeDelta = new Vector2(420f, 560f);
            _cardRoot.anchoredPosition = Vector2.zero;

            _cardBack = CreateUiImage(_cardRoot, "CardBack", new Color(0.15f, 0.2f, 0.35f));
            StretchFull(_cardBack.rectTransform);
            var backLabel = CreateUiText(_cardBack.transform, "BackLabel", "三國\nCARD", 48, TextAnchor.MiddleCenter, Color.white);
            StretchFull(backLabel.rectTransform);

            _cardFace = CreateUiImage(_cardRoot, "CardFace", LuckyColor);
            StretchFull(_cardFace.rectTransform);

            _titleText = CreateUiText(_cardFace.transform, "Title", "행운", 40, TextAnchor.UpperCenter, Color.white);
            _titleText.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
            _titleText.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            _titleText.rectTransform.offsetMin = Vector2.zero;
            _titleText.rectTransform.offsetMax = Vector2.zero;
            _titleText.fontStyle = FontStyle.Bold;

            _bodyText = CreateUiText(_cardFace.transform, "Body", "", 28, TextAnchor.MiddleCenter, Color.white);
            _bodyText.rectTransform.anchorMin = new Vector2(0.1f, 0.25f);
            _bodyText.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
            _bodyText.rectTransform.offsetMin = Vector2.zero;
            _bodyText.rectTransform.offsetMax = Vector2.zero;

            _hintText = CreateUiText(_cardFace.transform, "Hint", "클릭하여 닫기", 20, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.85f));
            _hintText.rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            _hintText.rectTransform.anchorMax = new Vector2(0.9f, 0.18f);
            _hintText.rectTransform.offsetMin = Vector2.zero;
            _hintText.rectTransform.offsetMax = Vector2.zero;

            var btn = _cardRoot.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(CloseCard);
        }

        public void ShowCard(CardKind kind, string title, string body, Action onClosed = null)
        {
            if (_busy)
            {
                onClosed?.Invoke();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(kind, title, body, onClosed));
        }

        IEnumerator ShowRoutine(CardKind kind, string title, string body, Action onClosed)
        {
            _busy = true;
            _onClosed = onClosed;
            _canvas.enabled = true;

            Color face = kind == CardKind.Lucky ? LuckyColor
                : kind == CardKind.Unlucky ? UnluckyColor
                : ChanceColor;

            _cardFace.color = face;
            _titleText.text = title;
            _bodyText.text = body;
            _cardFace.gameObject.SetActive(false);
            _cardBack.gameObject.SetActive(true);

            _cardRoot.localScale = Vector3.one * 0.2f;
            _cardRoot.localEulerAngles = new Vector3(0f, 0f, -18f);

            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.35f);
                _cardRoot.localScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one, p);
                _cardRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-18f, 0f, p));
                yield return null;
            }

            // 뒤집기
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.25f);
                float y = Mathf.Lerp(0f, 90f, p);
                _cardRoot.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }

            _cardBack.gameObject.SetActive(false);
            _cardFace.gameObject.SetActive(true);

            t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.25f);
                float y = Mathf.Lerp(-90f, 0f, p);
                _cardRoot.localEulerAngles = new Vector3(0f, y, 0f);
                yield return null;
            }

            _cardRoot.localEulerAngles = Vector3.zero;
            _busy = false;
        }

        public void CloseCard()
        {
            if (_busy) return;
            HideImmediate();
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();
        }

        void HideImmediate()
        {
            if (_canvas != null) _canvas.enabled = false;
            _busy = false;
        }

        static Image CreateUiImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static Text CreateUiText(Transform parent, string name, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
