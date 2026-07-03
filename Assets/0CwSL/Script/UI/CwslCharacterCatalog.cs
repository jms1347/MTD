using System.Collections.Generic;

public static class CwslCharacterCatalog
{
    public readonly struct Entry
    {
        public readonly CwslCharacterId Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string ControlHint;
        public readonly float VisionRadius;

        public Entry(
            CwslCharacterId id,
            string displayName,
            string description,
            string controlHint,
            float visionRadius)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ControlHint = controlHint;
            VisionRadius = visionRadius;
        }
    }

    private static readonly Entry[] Entries =
    {
        new(
            CwslCharacterId.Tank,
            "방패 탱커",
            "Q/Space 홀드 + 골드 1 이상 — 쉴드 켜지면 무적, 피격 시 골드 1 소모 / 시야 좁음",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택땅/어택유닛 | Q/Space 방패(홀드) | G 골드 선물/부활",
            14f),
        new(
            CwslCharacterId.MissileTank,
            "미사일 탱크",
            "A+클릭 어택땅/유닛(골드1) | Q 멀티샷(최대12, 발수만큼 골드) / 시야 넓음",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 멀티샷 | G 골드 선물/부활",
            22f),
        new(
            CwslCharacterId.RedMage,
            "빨간 마법사",
            "Q 후 지면 클릭 — 메테오 낙하 광역 피해 (골드 1) / 시야 본인만",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 메테오(지면) | G 골드 선물/부활",
            0f)
    };

    public static IReadOnlyList<Entry> All => Entries;

    public static Entry Get(CwslCharacterId id)
    {
        foreach (var entry in Entries)
        {
            if (entry.Id == id)
                return entry;
        }

        return Entries[0];
    }

    public static float GetVisionRadius(CwslCharacterId id) => Get(id).VisionRadius;
}
