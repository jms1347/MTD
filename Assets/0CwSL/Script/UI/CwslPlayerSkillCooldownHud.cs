using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>왼쪽 하단 Q/W/E/R 스킬 쿨타임 (360° 라디얼 Fill + 남은 초).</summary>
public class CwslPlayerSkillCooldownHud : MonoBehaviour
{
    private sealed class SlotUi
    {
        public Image cooldownFill;
        public TextMeshProUGUI keyLabel;
        public TextMeshProUGUI timeLabel;
    }

    private CwslPlayerSkillCooldowns cooldowns;
    private CwslPlayerCharacter playerCharacter;
    private readonly SlotUi[] slots = new SlotUi[CwslCharacterSkillCatalog.SkillCount];

    public static CwslPlayerSkillCooldownHud Ensure(
        Transform canvasTransform,
        CwslPlayerSkillCooldowns skillCooldowns,
        CwslPlayerCharacter character)
    {
        var existing = canvasTransform.Find("CwslSkillCooldownHud");
        CwslPlayerSkillCooldownHud hud;
        if (existing != null)
        {
            hud = existing.GetComponent<CwslPlayerSkillCooldownHud>();
            if (hud == null)
                hud = existing.gameObject.AddComponent<CwslPlayerSkillCooldownHud>();
            hud.EnsureUiBuilt(existing.GetComponent<RectTransform>());
        }
        else
        {
            var root = new GameObject("CwslSkillCooldownHud", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            hud = root.AddComponent<CwslPlayerSkillCooldownHud>();
            hud.BuildUi(root.GetComponent<RectTransform>());
        }

        hud.Bind(skillCooldowns, character);
        return hud;
    }

    private void EnsureUiBuilt(RectTransform rect)
    {
        if (slots[0] != null || rect == null)
            return;

        BuildUi(rect);
    }

    private void BuildUi(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);

        const float boxSize = 68f;
        const float spacing = 10f;
        rect.sizeDelta = new Vector2(
            boxSize * CwslCharacterSkillCatalog.SkillCount + spacing * (CwslCharacterSkillCatalog.SkillCount - 1),
            boxSize);

        for (var i = 0; i < slots.Length; i++)
        {
            var slotRoot = new GameObject($"Slot{i}", typeof(RectTransform));
            slotRoot.transform.SetParent(rect, false);
            var slotRect = slotRoot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 0f);
            slotRect.anchorMax = new Vector2(0f, 0f);
            slotRect.pivot = new Vector2(0f, 0f);
            slotRect.sizeDelta = new Vector2(boxSize, boxSize);
            slotRect.anchoredPosition = new Vector2(i * (boxSize + spacing), 0f);

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(slotRoot.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.92f);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(slotRoot.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(-8f, 2f);
            accentRect.anchoredPosition = new Vector2(0f, -4f);
            accent.GetComponent<Image>().color = new Color(0.42f, 0.72f, 1f, 0.85f);

            var fillGo = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(slotRoot.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fillImage = fillGo.GetComponent<Image>();
            CwslUiSpriteUtil.ConfigureRadial360Fill(fillImage, new Color(0.02f, 0.03f, 0.05f, 0.78f));

            var keyGo = new GameObject("Key", typeof(RectTransform), typeof(TextMeshProUGUI));
            keyGo.transform.SetParent(slotRoot.transform, false);
            var keyRect = keyGo.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(8f, -8f);
            keyRect.sizeDelta = new Vector2(28f, 22f);
            var keyLabel = keyGo.GetComponent<TextMeshProUGUI>();
            CwslTmpFontUtil.ApplyFont(keyLabel);
            keyLabel.fontSize = 16f;
            keyLabel.fontStyle = FontStyles.Bold;
            keyLabel.alignment = TextAlignmentOptions.TopLeft;
            keyLabel.color = new Color(0.88f, 0.94f, 1f, 0.95f);

            var timeGo = new GameObject("Time", typeof(RectTransform), typeof(TextMeshProUGUI));
            timeGo.transform.SetParent(slotRoot.transform, false);
            var timeRect = timeGo.GetComponent<RectTransform>();
            timeRect.anchorMin = Vector2.zero;
            timeRect.anchorMax = Vector2.one;
            timeRect.offsetMin = Vector2.zero;
            timeRect.offsetMax = Vector2.zero;
            var timeLabel = timeGo.GetComponent<TextMeshProUGUI>();
            CwslTmpFontUtil.ApplyFont(timeLabel);
            timeLabel.fontSize = 18f;
            timeLabel.fontStyle = FontStyles.Bold;
            timeLabel.alignment = TextAlignmentOptions.Center;
            timeLabel.color = new Color(1f, 0.95f, 0.82f, 0.98f);

            slots[i] = new SlotUi
            {
                cooldownFill = fillImage,
                keyLabel = keyLabel,
                timeLabel = timeLabel,
            };
        }
    }

    private void Update()
    {
        if (cooldowns == null)
            return;

        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            var definition = CwslCharacterSkillCatalog.Get(characterId, i);
            if (slot.keyLabel != null)
                slot.keyLabel.text = definition.KeyHint;

            var remaining = cooldowns.GetRemaining(i);
            if (slot.cooldownFill != null)
            {
                if (slot.cooldownFill.sprite == null)
                    CwslUiSpriteUtil.ConfigureRadial360Fill(slot.cooldownFill, new Color(0.02f, 0.03f, 0.05f, 0.78f));

                slot.cooldownFill.enabled = remaining > 0.01f;
                slot.cooldownFill.fillAmount = cooldowns.GetFillAmount(i);
            }

            if (slot.timeLabel != null)
            {
                if (remaining > 0.01f)
                    slot.timeLabel.text = remaining.ToString("0.0");
                else
                    slot.timeLabel.text = string.Empty;
            }
        }
    }

    public void Bind(CwslPlayerSkillCooldowns skillCooldowns, CwslPlayerCharacter character)
    {
        cooldowns = skillCooldowns;
        playerCharacter = character;
    }
}
