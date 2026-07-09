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
        if (rect == null)
            return;

        if (slots[0] == null)
            BuildUi(rect);

        ApplySlotStyles();
    }

    private void BuildUi(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);

        const float boxSize = 72f;
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

            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(slotRoot.transform, false);
            var ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(2f, 2f);
            ringRect.offsetMax = new Vector2(-2f, -2f);
            ring.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.3f, 0.55f);

            var fillGo = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(slotRoot.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            var fillImage = fillGo.GetComponent<Image>();
            CwslUiSpriteUtil.ConfigureRadial360Fill(fillImage, new Color(0.02f, 0.04f, 0.08f, 0.82f));

            var keyGo = new GameObject("Key", typeof(RectTransform), typeof(TextMeshProUGUI));
            keyGo.transform.SetParent(slotRoot.transform, false);
            var keyRect = keyGo.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(7f, -6f);
            keyRect.sizeDelta = new Vector2(24f, 18f);
            var keyLabel = keyGo.GetComponent<TextMeshProUGUI>();

            var timeGo = new GameObject("Time", typeof(RectTransform), typeof(TextMeshProUGUI));
            timeGo.transform.SetParent(slotRoot.transform, false);
            var timeRect = timeGo.GetComponent<RectTransform>();
            timeRect.anchorMin = Vector2.zero;
            timeRect.anchorMax = Vector2.one;
            timeRect.offsetMin = Vector2.zero;
            timeRect.offsetMax = Vector2.zero;
            var timeLabel = timeGo.GetComponent<TextMeshProUGUI>();

            slots[i] = new SlotUi
            {
                cooldownFill = fillImage,
                keyLabel = keyLabel,
                timeLabel = timeLabel,
            };
        }

        ApplySlotStyles();
    }

    private void ApplySlotStyles()
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            if (slot.keyLabel != null)
            {
                CwslTmpFontUtil.ApplyFont(slot.keyLabel);
                slot.keyLabel.fontSize = 13f;
                slot.keyLabel.fontStyle = FontStyles.Bold;
                slot.keyLabel.alignment = TextAlignmentOptions.TopLeft;
                slot.keyLabel.color = new Color(0.78f, 0.86f, 0.96f, 0.88f);
                slot.keyLabel.raycastTarget = false;
            }

            if (slot.timeLabel != null)
            {
                CwslTmpFontUtil.ApplyFont(slot.timeLabel);
                slot.timeLabel.fontSize = 28f;
                slot.timeLabel.fontStyle = FontStyles.Bold;
                slot.timeLabel.alignment = TextAlignmentOptions.Center;
                slot.timeLabel.color = new Color(1f, 0.96f, 0.86f, 0.98f);
                slot.timeLabel.raycastTarget = false;
            }

            if (slot.cooldownFill != null && slot.cooldownFill.sprite == null)
                CwslUiSpriteUtil.ConfigureRadial360Fill(slot.cooldownFill, new Color(0.02f, 0.04f, 0.08f, 0.82f));
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
                    CwslUiSpriteUtil.ConfigureRadial360Fill(slot.cooldownFill, new Color(0.02f, 0.04f, 0.08f, 0.82f));

                var onCooldown = remaining > 0.01f;
                slot.cooldownFill.enabled = onCooldown;
                slot.cooldownFill.fillAmount = onCooldown ? cooldowns.GetFillAmount(i) : 0f;
            }

            if (slot.timeLabel != null)
            {
                if (remaining > 0.01f)
                    slot.timeLabel.text = remaining >= 10f
                        ? Mathf.CeilToInt(remaining).ToString()
                        : remaining.ToString("0.0");
                else
                    slot.timeLabel.text = string.Empty;
            }
        }
    }

    public void Bind(CwslPlayerSkillCooldowns skillCooldowns, CwslPlayerCharacter character)
    {
        cooldowns = skillCooldowns;
        playerCharacter = character;
        ApplySlotStyles();
    }
}
