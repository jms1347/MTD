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
            "임시 밸런스: HP/공격/방어/시야 동일 · Q/E/R/F 스킬 4개 · 스테미너 소모",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q/E/R/F 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.MissileTank,
            "총잡이",
            "임시 밸런스: HP/공격/방어/시야 동일 · Q/E/R/F 스킬 4개 · 스테미너 소모",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q/E/R/F 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.RedMage,
            "빨간 마법사",
            "임시 밸런스: HP/공격/방어/시야 동일 · Q/E/R/F 스킬 4개 · 스테미너 소모",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 메테오(지면) · E/R/F 준비중 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.MomentumRammer,
            "질주자",
            "임시 밸런스: HP/공격/방어/시야 동일 · Q/E/R/W 스킬 4개 · 스테미너 소모",
            "우클릭 홀드 조향 | 좌클릭 적선택 | A+클릭 어택 | Q/E/R/W 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.CrowdGatherer,
            "끌모",
            "임시 밸런스: HP/공격/방어/시야 동일 · Q/E/R/W 스킬 4개 · 스테미너 소모",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q/E/R/W 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.Barricade,
            "바리케이드",
            "벽돌 인간 · 벽/발판/수리/폭파 · Q/E/R/W 스킬",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 드래그로 벽 · E/R/W 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.Healer,
            "힐러",
            "요정 프리스트 · 힐/독/버프 · Q/E/R/W 스킬",
            "우클릭 이동 | 좌클릭 적선택 | A+클릭 어택 | Q 힐장판 · E/R/W 스킬 | G 골드 선물",
            CwslGameConstants.PlayerVisionRadius)
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

    public static float GetVisionRadius(CwslCharacterId id) => CwslGameConstants.PlayerVisionRadius;
}
