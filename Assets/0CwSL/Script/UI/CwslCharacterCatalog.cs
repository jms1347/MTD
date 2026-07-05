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
            "총잡이",
            "어택/추적 중 1초 쿨 자동 사격(골드1) | Q 양손 동시 2발(골드2, 쿨무시) / 시야=사거리",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 양손 동시 | G 골드 선물/부활",
            CwslGameConstants.MissileTankRange),
        new(
            CwslCharacterId.RedMage,
            "빨간 마법사",
            "Q 후 지면 클릭 — 메테오 낙하 광역 피해 (골드 1) / 시야 본인만 · 착탄 지점 2.8초 개방",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 메테오(지면) | G 골드 선물/부활",
            0f),
        new(
            CwslCharacterId.MomentumRammer,
            "질주자",
            "이동 중 점점 가속 · 고속 충돌 시 피해 / 멈추면 일반 이동 · Q 긴급 제동(골드 1)",
            "우클릭 이동(관성) | 좌클릭 적선택 | A+클릭 어택 | Q 긴급 제동 | G 골드 선물/부활",
            16f)
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
