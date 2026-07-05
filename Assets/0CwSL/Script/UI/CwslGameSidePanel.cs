using System.Collections.Generic;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



public class CwslGameSidePanel : MonoBehaviour

{

    private const float PanelWidth = 360f;

    private const float CardHeight = 156f;



    private static readonly Color PanelColor = new(0.06f, 0.08f, 0.12f, 0.94f);

    private static readonly Color TabActiveColor = new(0.18f, 0.42f, 0.72f, 1f);

    private static readonly Color TabIdleColor = new(0.12f, 0.14f, 0.18f, 0.9f);

    private static readonly Color TabDisabledColor = new(0.1f, 0.11f, 0.13f, 0.55f);

    private static readonly Color CardIdleColor = new(0.11f, 0.13f, 0.17f, 0.96f);

    private static readonly Color CardSelectedColor = new(0.14f, 0.24f, 0.38f, 0.98f);

    private static readonly Color CardBorderColor = new(0.28f, 0.55f, 0.9f, 0.95f);

    private static readonly Color AccentColor = new(1f, 0.86f, 0.35f, 1f);

    private static readonly Color MutedTextColor = new(0.72f, 0.78f, 0.86f, 1f);
    private CwslPlayerCharacter playerCharacter;

    private TextMeshProUGUI hintLabel;

    private RectTransform characterTabContent;

    private RectTransform playerTabContent;

    private Image characterTabButtonBg;

    private Image playerTabButtonBg;

    private readonly List<CharacterCard> characterCards = new();

    private bool isBuilt;



    private enum SideTab

    {

        Character,

        Player

    }



    private SideTab activeTab = SideTab.Character;



    private sealed class CharacterCard

    {

        public CwslCharacterId Id;

        public Button Button;

        public Image Background;

        public Image AccentBar;

        public Image Border;

        public TextMeshProUGUI StatusLabel;

    }



    public void Bind(CwslPlayerCharacter character, TextMeshProUGUI hint)

    {

        if (playerCharacter != null)

            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;



        playerCharacter = character;

        hintLabel = hint;



        if (!isBuilt)

            BuildPanel();



        if (playerCharacter != null)
        {
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;
            CwslPlayerCharacter.OnAnyCharacterChanged += HandleAnyCharacterChanged;
            RefreshSelection(playerCharacter.CharacterId);
            RefreshHint(playerCharacter.CharacterId);
            RefreshTakenStates();
        }
    }



    private void OnDestroy()

    {

        if (playerCharacter != null)

            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;

        CwslPlayerCharacter.OnAnyCharacterChanged -= HandleAnyCharacterChanged;

    }



    private void BuildPanel()

    {

        isBuilt = true;



        var panelRect = gameObject.GetComponent<RectTransform>();

        if (panelRect == null)

            panelRect = gameObject.AddComponent<RectTransform>();



        panelRect.anchorMin = new Vector2(0f, 0f);

        panelRect.anchorMax = new Vector2(0f, 1f);

        panelRect.pivot = new Vector2(0f, 0.5f);

        panelRect.anchoredPosition = Vector2.zero;

        panelRect.sizeDelta = new Vector2(PanelWidth, 0f);



        var panelImage = gameObject.GetComponent<Image>();

        if (panelImage == null)

            panelImage = gameObject.AddComponent<Image>();

        SetupImage(panelImage, PanelColor);

        panelImage.raycastTarget = true;



        var header = CreateRect("Header", transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

        header.anchoredPosition = new Vector2(0f, -10f);

        header.sizeDelta = new Vector2(-24f, 34f);

        var headerLabel = CreateText(header, "HeaderLabel", "게임 메뉴", 18, FontStyles.Bold, new Color(0.88f, 0.9f, 0.95f));

        Stretch(headerLabel.rectTransform);

        headerLabel.alignment = TextAlignmentOptions.MidlineLeft;



        var tabBar = CreateRect("TabBar", transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

        tabBar.anchoredPosition = new Vector2(0f, -48f);

        tabBar.sizeDelta = new Vector2(-24f, 40f);



        characterTabButtonBg = CreateTabButton(tabBar, "캐릭터", new Vector2(0f, 0f), new Vector2(0.5f, 1f), true, SelectCharacterTab);

        playerTabButtonBg = CreateTabButton(tabBar, "플레이어", new Vector2(0.5f, 0f), new Vector2(1f, 1f), false, SelectPlayerTab);



        var contentRoot = CreateRect("TabContent", transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));

        contentRoot.offsetMin = new Vector2(12f, 12f);

        contentRoot.offsetMax = new Vector2(-12f, -96f);



        characterTabContent = CreateRect("CharacterTab", contentRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));

        BuildCharacterTab(characterTabContent);



        playerTabContent = CreateRect("PlayerTab", contentRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));

        BuildPlayerTab(playerTabContent);

        playerTabContent.gameObject.SetActive(false);

    }



    private void BuildCharacterTab(RectTransform parent)

    {

        var title = CreateText(parent, "Title", "캐릭터 선택", 20, FontStyles.Bold, AccentColor);

        var titleRect = title.rectTransform;

        titleRect.anchorMin = new Vector2(0f, 1f);

        titleRect.anchorMax = new Vector2(1f, 1f);

        titleRect.pivot = new Vector2(0.5f, 1f);

        titleRect.anchoredPosition = Vector2.zero;

        titleRect.sizeDelta = new Vector2(0f, 28f);

        title.alignment = TextAlignmentOptions.MidlineLeft;



        var subtitle = CreateText(parent, "Subtitle", "카드를 눌러 캐릭터를 변경합니다.", 14, FontStyles.Normal, MutedTextColor);

        var subtitleRect = subtitle.rectTransform;

        subtitleRect.anchorMin = new Vector2(0f, 1f);

        subtitleRect.anchorMax = new Vector2(1f, 1f);

        subtitleRect.pivot = new Vector2(0.5f, 1f);

        subtitleRect.anchoredPosition = new Vector2(0f, -30f);

        subtitleRect.sizeDelta = new Vector2(0f, 22f);

        subtitle.alignment = TextAlignmentOptions.MidlineLeft;



        var scrollRoot = CreateRect("Scroll", parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));

        scrollRoot.offsetMin = Vector2.zero;

        scrollRoot.offsetMax = new Vector2(0f, -58f);



        var scrollImage = scrollRoot.gameObject.AddComponent<Image>();

        SetupImage(scrollImage, new Color(0.08f, 0.09f, 0.12f, 0.45f));

        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();

        scroll.horizontal = false;

        scroll.movementType = ScrollRect.MovementType.Clamped;

        scroll.scrollSensitivity = 24f;



        var viewport = CreateRect("Viewport", scrollRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));

        viewport.offsetMin = new Vector2(4f, 4f);

        viewport.offsetMax = new Vector2(-4f, -4f);

        viewport.gameObject.AddComponent<RectMask2D>();



        var content = CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

        content.anchoredPosition = Vector2.zero;

        content.sizeDelta = new Vector2(0f, 0f);



        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.spacing = 12f;

        layout.padding = new RectOffset(4, 4, 4, 8);

        layout.childAlignment = TextAnchor.UpperCenter;

        layout.childControlWidth = true;

        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;

        layout.childForceExpandHeight = false;



        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;



        scroll.viewport = viewport;

        scroll.content = content;



        foreach (var entry in CwslCharacterCatalog.All)

            characterCards.Add(CreateCharacterCard(content, entry));

    }



    private void BuildPlayerTab(RectTransform parent)

    {

        var panel = CreateRect("PlayerPlaceholder", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));

        panel.sizeDelta = new Vector2(-8f, 180f);



        var bg = panel.gameObject.AddComponent<Image>();

        SetupImage(bg, CardIdleColor);



        var label = CreateText(panel, "Label", "플레이어 탭\n(준비 중)", 18, FontStyles.Bold, MutedTextColor);

        Stretch(label.rectTransform);

        label.alignment = TextAlignmentOptions.Center;

        label.enableWordWrapping = true;

    }



    private CharacterCard CreateCharacterCard(RectTransform parent, CwslCharacterCatalog.Entry entry)

    {

        var cardRoot = CreateRect($"Card_{entry.Id}", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));

        cardRoot.sizeDelta = new Vector2(0f, CardHeight);



        var layoutElement = cardRoot.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredHeight = CardHeight;

        layoutElement.minHeight = CardHeight;



        var bg = cardRoot.gameObject.AddComponent<Image>();

        SetupImage(bg, CardIdleColor);



        var border = CreateRect("Border", cardRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));

        border.offsetMin = new Vector2(-2f, -2f);

        border.offsetMax = new Vector2(2f, 2f);

        var borderImage = border.gameObject.AddComponent<Image>();

        SetupImage(borderImage, CardBorderColor);

        borderImage.raycastTarget = false;

        border.gameObject.SetActive(false);



        var accent = CreateRect("Accent", cardRoot, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));

        accent.anchoredPosition = new Vector2(6f, 0f);

        accent.sizeDelta = new Vector2(4f, -16f);

        var accentImage = accent.gameObject.AddComponent<Image>();

        SetupImage(accentImage, CardBorderColor);

        accentImage.raycastTarget = false;

        accent.gameObject.SetActive(false);



        var button = cardRoot.gameObject.AddComponent<Button>();

        button.targetGraphic = bg;

        var colors = button.colors;

        colors.normalColor = Color.white;

        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);

        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        colors.selectedColor = Color.white;

        button.colors = colors;



        var id = entry.Id;

        button.interactable = false;
        button.onClick.AddListener(() => { });



        var name = CreateText(cardRoot, "Name", entry.DisplayName, 19, FontStyles.Bold, Color.white);

        SetStretchTop(name.rectTransform, 14f, 28f);

        name.alignment = TextAlignmentOptions.MidlineLeft;



        var desc = CreateText(cardRoot, "Desc", entry.Description, 14, FontStyles.Normal, MutedTextColor);

        var descRect = desc.rectTransform;

        descRect.anchorMin = new Vector2(0f, 0f);

        descRect.anchorMax = new Vector2(1f, 1f);

        descRect.offsetMin = new Vector2(16f, 36f);

        descRect.offsetMax = new Vector2(-12f, -44f);

        desc.alignment = TextAlignmentOptions.TopLeft;

        desc.enableWordWrapping = true;

        desc.overflowMode = TextOverflowModes.Ellipsis;

        desc.lineSpacing = -2f;



        var status = CreateText(cardRoot, "Status", "선택됨", 14, FontStyles.Bold, AccentColor);

        var statusRect = status.rectTransform;

        statusRect.anchorMin = new Vector2(0f, 0f);

        statusRect.anchorMax = new Vector2(1f, 0f);

        statusRect.pivot = new Vector2(0f, 0f);

        statusRect.anchoredPosition = new Vector2(16f, 12f);

        statusRect.sizeDelta = new Vector2(-28f, 22f);

        status.alignment = TextAlignmentOptions.MidlineLeft;

        status.gameObject.SetActive(false);



        return new CharacterCard

        {

            Id = entry.Id,

            Button = button,

            Background = bg,

            AccentBar = accentImage,

            Border = borderImage,

            StatusLabel = status

        };

    }



    private void SelectCharacterTab()

    {

        activeTab = SideTab.Character;

        if (characterTabContent != null)

            characterTabContent.gameObject.SetActive(true);

        if (playerTabContent != null)

            playerTabContent.gameObject.SetActive(false);

        if (characterTabButtonBg != null)

            characterTabButtonBg.color = TabActiveColor;

        if (playerTabButtonBg != null)

            playerTabButtonBg.color = TabIdleColor;

    }



    private void SelectPlayerTab()

    {

        activeTab = SideTab.Player;

        if (characterTabContent != null)

            characterTabContent.gameObject.SetActive(false);

        if (playerTabContent != null)

            playerTabContent.gameObject.SetActive(true);

        if (characterTabButtonBg != null)

            characterTabButtonBg.color = TabIdleColor;

        if (playerTabButtonBg != null)

            playerTabButtonBg.color = TabActiveColor;

    }



    private Image CreateTabButton(

        RectTransform parent,

        string label,

        Vector2 anchorMin,

        Vector2 anchorMax,

        bool active,

        UnityEngine.Events.UnityAction onClick)

    {

        var rect = CreateRect($"Tab_{label}", parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f));

        rect.offsetMin = new Vector2(2f, 0f);

        rect.offsetMax = new Vector2(-2f, 0f);



        var image = rect.gameObject.AddComponent<Image>();

        SetupImage(image, active ? TabActiveColor : onClick == null ? TabDisabledColor : TabIdleColor);



        var text = CreateText(rect, "Label", label, 16, FontStyles.Bold, Color.white);

        Stretch(text.rectTransform);

        text.alignment = TextAlignmentOptions.Center;



        if (onClick != null)

        {

            var button = rect.gameObject.AddComponent<Button>();

            button.targetGraphic = image;

            button.onClick.AddListener(onClick);

        }



        return image;

    }



    private void HandleCharacterChanged(CwslCharacterId characterId)

    {

        RefreshSelection(characterId);

        RefreshHint(characterId);

        RefreshTakenStates();

    }

    private void HandleAnyCharacterChanged()
    {
        RefreshTakenStates();
    }



    private void RefreshSelection(CwslCharacterId selected)

    {

        foreach (var card in characterCards)

        {

            var isSelected = card.Id == selected;

            card.Background.color = isSelected ? CardSelectedColor : CardIdleColor;

            if (card.AccentBar != null)

                card.AccentBar.gameObject.SetActive(isSelected);

            if (card.Border != null)

                card.Border.gameObject.SetActive(isSelected);

        }

    }

    private void RefreshTakenStates()
    {
        if (playerCharacter == null)
            return;

        var ownerId = playerCharacter.OwnerClientId;
        var selected = playerCharacter.CharacterId;

        foreach (var card in characterCards)
        {
            var takenByOther = CwslCharacterRegistry.IsTakenByOther(card.Id, ownerId);
            var isSelected = card.Id == selected;
            card.Button.interactable = !takenByOther || isSelected;

            if (card.StatusLabel != null)
            {
                if (isSelected)
                    card.StatusLabel.text = "배정됨";
                else if (takenByOther)
                    card.StatusLabel.text = "다른 플레이어";
                else
                    card.StatusLabel.text = string.Empty;

                card.StatusLabel.gameObject.SetActive(isSelected || takenByOther);
            }
        }
    }



    private void RefreshHint(CwslCharacterId characterId)

    {

        if (hintLabel != null)

            hintLabel.text = CwslCharacterCatalog.Get(characterId).ControlHint;

    }



    private static void SetupImage(Image image, Color color)
    {
        image.sprite = CwslUiSpriteUtil.WhiteSprite;
        image.type = Image.Type.Simple;
        image.color = color;
    }

    private static RectTransform CreateRect(

        string name,

        Transform parent,

        Vector2 anchorMin,

        Vector2 anchorMax,

        Vector2 pivot)

    {

        var go = new GameObject(name, typeof(RectTransform));

        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;

        rect.anchorMax = anchorMax;

        rect.pivot = pivot;

        return rect;

    }



    private static TextMeshProUGUI CreateText(

        Transform parent,

        string name,

        string text,

        float fontSize,

        FontStyles style,

        Color color)

    {

        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

        go.transform.SetParent(parent, false);

        var label = go.GetComponent<TextMeshProUGUI>();

        CwslTmpFontUtil.ApplyFont(label);

        label.text = text;

        label.fontSize = fontSize;

        label.fontStyle = style;

        label.color = color;

        label.raycastTarget = false;

        return label;

    }



    private static void Stretch(RectTransform rect)

    {

        rect.anchorMin = Vector2.zero;

        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;

        rect.offsetMax = Vector2.zero;

    }



    private static void SetStretchTop(RectTransform rect, float topPadding, float height)

    {

        rect.anchorMin = new Vector2(0f, 1f);

        rect.anchorMax = new Vector2(1f, 1f);

        rect.pivot = new Vector2(0.5f, 1f);

        rect.anchoredPosition = new Vector2(0f, -topPadding);

        rect.sizeDelta = new Vector2(-32f, height);

    }

}


