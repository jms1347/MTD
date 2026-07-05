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
            "Q/Space 홀드 — 쉴드(피격당 골드 1·100만 원). 방패 없이 맞으면 HP 감소 / 시야 좁음",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택땅/어택유닛 | Q/Space 방패(홀드) | G 골드 선물/부활",
            14f),
        new(
            CwslCharacterId.MissileTank,
            "총잡이",
            "평타 무료(데미지 1) · 1초 쿨 자동 사격 | Q 양손 동시 3골드(300만 원, 쿨무시) / 시야=사거리",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 양손 동시 | G 골드 선물/부활",
            CwslGameConstants.MissileTankRange),
        new(
            CwslCharacterId.RedMage,
            "빨간 마법사",
            "Q 후 지면 클릭 — 메테오 5골드(500만 원) · 착탄 지점 2.8초 시야 개방 / 본인만 시야",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 메테오(지면) | G 골드 선물/부활",
            0f),
        new(
            CwslCharacterId.MomentumRammer,
            "질주자",
            "관성 이동 · 고속 충돌 피해 · Q 날개 3골드+0.5초마다 1골드 · 벽/아군 충돌 스턴",
            "우클릭 이동(관성) | 좌클릭 적선택 | A+클릭 어택 | Q/Space 홀드(날개) | G 골드 선물/부활",
            16f),
        new(
            CwslCharacterId.CrowdGatherer,
            "끌모",
            "Q 홀드 — 5골드 선불 · 원 확장 중 적·총알 슬로우(대상당 0.5초마다 1골드) · 뗄 때 전부 당김",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q/Space 홀드(당김) | G 골드 선물/부활",
            15f)
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
